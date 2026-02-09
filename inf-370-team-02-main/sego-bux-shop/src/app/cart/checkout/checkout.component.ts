import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators
} from '@angular/forms';
import { Router } from '@angular/router';
import emailjs from '@emailjs/browser';
import { firstValueFrom } from 'rxjs';
import { HttpClient } from '@angular/common/http';

import { CartService, CartItem } from '../../services/cart.service';
import { OrderService } from '../../services/order.service';
import { AuthService } from '../../services/auth.service';
import { VatService } from '../../services/vat.service';
import { CustomerService, Customer } from '../../services/customer.service';
import { environment } from '../../../environments/environment';
import type { OrderDto } from '../../dto/order.dto';
import type { OrderResponseDto } from '../../dto/order-response.dto';
import { ToastService } from '../../shared/toast.service';

type ShippingPolicy = { originAddress: string; thresholdKm: number; flatShippingFee: number; };

/** Map internal status -> customer-facing label */
function displayOrderStatusForCustomer(internal?: string) {
  return (internal || '').toLowerCase() === 'pending' ? 'Paid' : (internal || 'Paid');
}

function buildOrderEmailHtml(order: OrderResponseDto, type: 'status' | 'delivery') {
  const qtySum = order.orderLines.reduce((sum, x) => sum + x.quantity, 0) || 1;
  const linesHtml = order.orderLines.map((l, idx) => `
    <tr>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;">${idx + 1}</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;">${l.productName}</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;">${l.quantity}</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;">${l.customText || '—'}</td>
      <td style="padding:8px 6px;border-bottom:1px solid #eee;text-align:right;">
        R${(l.quantity * order.totalPrice / qtySum).toFixed(2)}
      </td>
    </tr>
  `).join('');

  const statusText = type === 'status'
    ? displayOrderStatusForCustomer(order.orderStatusName)
    : (order.deliveryStatus || 'Pending');

  return `
    <div style="max-width:660px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 4px 32px #e91e6340;padding:0 0 24px 0;font-family:'Segoe UI',Arial,sans-serif;color:#2b2b2b;">
      <div style="display:flex;align-items:center;justify-content:space-between;background:#f8e3ef;border-radius:12px 12px 0 0;padding:22px 28px;">
        <img src="https://i.postimg.cc/9Css0H5p/logo.jpg" alt="By Sego and Bux Logo" style="height:60px;display:block;" />
        <div style="text-align:right;">
          <div style="font-size:24px;font-weight:700;letter-spacing:-1px;color:#e91e63;">By Sego and Bux</div>
          <div style="font-size:15px;color:#333;">Order #${order.orderID} <span style="color:#a47f99;">${type === 'delivery' ? '— Delivery Update' : '— Status Update'}</span></div>
        </div>
      </div>
      <div style="padding:28px 32px 0 32px;">
        <div style="margin-bottom:12px;font-size:17px;">Hi <b>${order.customerName}</b>,</div>
        <div style="margin-bottom:18px;">
          ${type === 'status'
            ? `Your order status is now: <span style="color:#e91e63;font-weight:bold;">${statusText}</span>.`
            : `Your order is <span style="color:#e91e63;font-weight:bold;">${statusText}</span>.`}
        </div>

        <table style="width:100%;border-collapse:collapse;font-size:15px;margin:14px 0 24px 0;">
          <thead>
            <tr style="background:#f7e8f4;">
              <th style="padding:8px 6px;text-align:left;border-bottom:2px solid #e91e63;">#</th>
              <th style="padding:8px 6px;text-align:left;border-bottom:2px solid #e91e63;">Product</th>
              <th style="padding:8px 6px;text-align:left;border-bottom:2px solid #e91e63;">Qty</th>
              <th style="padding:8px 6px;text-align:left;border-bottom:2px solid #e91e63;">Custom</th>
              <th style="padding:8px 6px;text-align:right;border-bottom:2px solid #e91e63;">Line Total</th>
            </tr>
          </thead>
          <tbody>${linesHtml}</tbody>
          <tfoot>
            <tr>
              <td colspan="4" style="padding:10px 6px;text-align:right;border-top:2px solid #e91e63;"><b>Order Total:</b></td>
              <td style="padding:10px 6px;text-align:right;border-top:2px solid #e91e63;color:#e91e63;font-weight:700;">
                R${order.totalPrice.toFixed(2)}
              </td>
            </tr>
          </tfoot>
        </table>

        <div style="font-size:15px;margin:10px 0 18px 0;line-height:1.7;">
          <b>Order Date:</b> ${new Date(order.orderDate).toLocaleDateString()}<br>
          <b>Delivery Method:</b> ${order.deliveryMethod}<br>
          <b>Delivery Status:</b> ${order.deliveryStatus}<br>
          <b>Waybill:</b> ${order.waybillNumber || 'N/A'}<br>
          <b>Delivery Address:</b> ${order.deliveryAddress}
        </div>
      </div>
    </div>
  `;
}

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.scss']
})
export class CheckoutComponent implements OnInit {
  cartItems: CartItem[] = [];

  /** Display variables (aligned to your template) */
  total = 0;   // SubTotal Excl (net)
  vat   = 0;   // VAT amount
  grand = 0;   // Total Due (gross + shipping)
  vatRate = 0; // %

  /** Internal tracking */
  private gross = 0; // Gross (incl VAT, excl shipping)

  deliveryForm!: FormGroup;
  chosenDelivery: string | null = null;
  chosenDistance: number | null = null;
  isSending = false;

  userEmail = '';
  userFirstName = '';
  userLastName = '';

  shippingFee = 0;
  private customerId = 0;

  // NEW: policy for dynamic help text
  policy: ShippingPolicy = { originAddress: '', thresholdKm: 20, flatShippingFee: 100 };

  constructor(
    public  cartService: CartService,
    private orderService: OrderService,
    private auth: AuthService,
    private router: Router,
    private fb: FormBuilder,
    private vatService: VatService,
    private customerService: CustomerService,
    private toastSvc: ToastService,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    const token = this.auth.getToken();
    if (!token) {
      this.router.navigate(['/login']);
      return;
    }
    const payload: any = JSON.parse(atob(token.split('.')[1]));
    this.customerId = +payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

    this.customerService.getCustomerById(this.customerId).subscribe({
      next: (customer: Customer) => {
        this.userEmail     = customer.email;
        this.userFirstName = customer.name;
        this.userLastName  = customer.surname;
      },
      error: () => {
        this.userEmail = '';
        this.userFirstName = '';
        this.userLastName = '';
      }
    });

    this.cartItems = this.cartService.getItems();
    this.vatRate = this.vatService.vatRateValue;

    this.deliveryForm = this.fb.group({
      address:    ['', Validators.required],
      city:       ['', Validators.required],
      suburb:     ['', Validators.required],
      province:   ['', Validators.required],
      postalCode: ['', Validators.required]
    });

    this.recalcTotals();
    this.vatService.vatRate$.subscribe(rate => {
      this.vatRate = rate ?? 0;
      this.recalcTotals();
    });
    this.cartService.cart$.subscribe(items => {
      this.cartItems = items;
      this.recalcTotals();
    });

    // NEW: pull current policy for the help text
    this.http.get<ShippingPolicy & { id:number; whatsAppNumber:string; supportEmail:string }>(
      `${environment.apiUrl}/api/admin/chatbot-config`
    ).subscribe({
      next: p => { this.policy = { originAddress: p.originAddress, thresholdKm: p.thresholdKm, flatShippingFee: p.flatShippingFee }; },
      error: () => {}
    });
  }

  /** Centralized totals calculator (VAT-inclusive pricing) */
  private recalcTotals(): void {
    this.gross = this.round2(this.cartService.getGrossSubtotal());
    this.total = this.round2(this.cartService.getNetFromGross(this.vatRate));
    this.vat   = this.round2(this.cartService.getVatFromGross(this.vatRate));
    this.grand = this.round2(this.gross + (this.shippingFee || 0));
  }

  private round2(n: number): number {
    return Math.round((n + Number.EPSILON) * 100) / 100;
  }

  determineDelivery(): void {
    if (this.deliveryForm.invalid) {
      alert('Please fill out all address fields first.');
      return;
    }
    const addr = Object.values(this.deliveryForm.value).join(', ');
    this.orderService.calculateDelivery(addr).subscribe({
      next: res => {
        this.chosenDelivery = res.deliveryMethod;
        this.chosenDistance = res.distance;
        this.shippingFee    = res.shippingFee ?? 0;
        this.recalcTotals();
      },
      error: () => alert('❌ Delivery calculation failed')
    });
  }

  get shipping() { return this.shippingFee; }

  async placeOrder(): Promise<void> {
    if (this.deliveryForm.invalid || this.cartItems.length === 0 || !this.chosenDelivery) {
      alert('Please complete the form, calculate delivery, and have items in your cart.');
      return;
    }

    this.isSending = true;
    const addr = Object.values(this.deliveryForm.value).join(', ');

    const dto: OrderDto = {
      customerID:      this.customerId,
      orderStatusID:   1, // backend "Pending" (we show "Paid")
      totalPrice:      this.grand, // Total Due (gross + shipping)
      deliveryMethod:  this.chosenDelivery!,
      deliveryAddress: addr,
      courierProvider: this.chosenDelivery === 'Courier' ? 'Courier Guy' : 'Company Delivery',
      orderLines: this.cartItems.map(i => ({
        productID:         i.id,
        quantity:          i.quantity,
        template:          i.customization?.template,
        customText:        i.customization?.customText,
        font:              i.customization?.font,
        fontSize:          i.customization?.fontSize,
        color:             i.customization?.color,
        uploadedImagePath: ''
      }))
    };

    this.launchPaystack(this.grand, this.userEmail, () => {
      this.orderService.placeOrder(dto).subscribe({
        next: async (order: OrderResponseDto) => {
          const itemByProductId = new Map<number, CartItem>();
          this.cartItems.forEach(ci => itemByProductId.set(ci.id, ci));

          const tasks: Promise<any>[] = [];
          for (const line of order.orderLines) {
            if (line.productID == null) continue;
            const item = itemByProductId.get(line.productID);
            if (!item?.customization) continue;

            if (item.customization.uploadedImageFile) {
              tasks.push(firstValueFrom(
                this.orderService.uploadCustomizationImage(order.orderID, line.orderLineID, item.customization.uploadedImageFile)
              ));
            }
            if (item.customization.snapshotFile) {
              tasks.push(firstValueFrom(
                this.orderService.uploadCustomizationSnapshot(order.orderID, line.orderLineID, item.customization.snapshotFile)
              ));
            }
          }
          try { await Promise.all(tasks); } catch {}

          // Confirmation email (shows "Paid")
          this.sendConfirmationEmail(order);

          this.toastSvc.show('✅ Order placed successfully!');
          this.cartService.clearCart();
          this.router.navigate(['/account']);
        },
        error: (err: any) => {
          console.error('Order placement failed', err);
          alert('❌ Failed to place order: ' + (err.error?.message || err.message));
          this.isSending = false;
        }
      });
    });
  }

  private launchPaystack(orderAmount: number, email: string, onSuccess: () => void): void {
    const handler = (window as any).PaystackPop.setup({
      key: environment.paystackPublicKey,
      email,
      amount: orderAmount * 100,
      currency: 'ZAR',
      ref: '' + Math.floor(Math.random() * 1000000000 + 1),
      callback: onSuccess,
      onClose: () => this.toastSvc.show('Payment window closed.')
    });
    handler.openIframe();
  }

  private sendConfirmationEmail(order: OrderResponseDto) {
    const html = buildOrderEmailHtml(order, 'status');
    const params = {
      to_name:          order.customerName,
      to_email:         order.customerEmail,
      subject:          `Order #${order.orderID} Confirmation`,
      order_id:         order.orderID,
      order_lines_html: html,
      deliveryMethod:   order.deliveryMethod,
      deliveryAddress:  order.deliveryAddress
    };

    emailjs.send(
      environment.emailJsServiceId,
      environment.emailJsOrderStatusTemplateId,
      params,
      environment.emailJsUserId
    ).then(
      () => console.log('✅ Confirmation email sent'),
      (err: any) => console.error('❌ EmailJS error:', err)
    );
  }
}
