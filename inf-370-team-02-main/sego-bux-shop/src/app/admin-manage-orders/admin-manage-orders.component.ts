// src/app/components/admin-manage-orders.component.ts
import { Component, HostListener, OnInit, ViewChild, ElementRef, OnDestroy } from '@angular/core'; // <-- ADD OnDestroy
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import emailjs from 'emailjs-com';
import { OrderService, DeliveryUpdate } from '../services/order.service';
import type { OrderResponseDto } from '../dto/order-response.dto';
import { environment } from '../../environments/environment';
import { PaginatePipe } from '../shared/pagination/pagination.pipe';
import { PaginationComponent } from '../shared/pagination/pagination.component';
import { ToastService } from '../shared/toast.service';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';
import { DeliveryCalendarComponent } from './delivery-calendar.component';
import { OrdersStateService } from '../services/orders-state.service'; // <-- ADD THIS
import { Subscription } from 'rxjs'; // <-- ADD THIS

// ⬇️ Date pipes (C.A.T formatting wherever used in template)
import { CatDatePipe } from '../pipes/cat-date.pipe';
import { CatRelativePipe } from '../pipes/cat-relative.pipe';

/* ===================== EFFECTIVE/EXTERNAL STATUS ==================== */
type ExtStatus = 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Dispatched';

function effectiveStatus(o: { orderStatusName?: string | null; deliveryStatus?: string | null }): ExtStatus {
  const ds = (o.deliveryStatus || '').trim().toLowerCase();
  const os = (o.orderStatusName || 'Pending').trim().toLowerCase();

  // First check delivery status - this takes priority
  if (ds.includes('delivered')) return 'Delivered';
  if (ds.includes('dispatched')) return 'Dispatched';
  if (ds && ds !== 'pending') return ds.charAt(0).toUpperCase() + ds.slice(1) as ExtStatus;

  // Fall back to order status if delivery status is not informative
  if (os.includes('shipped')) return 'Shipped';
  if (os.includes('processing')) return 'Processing';

  return 'Pending';
}

/* =============================== EMAIL =============================== */
function buildOrderEmailHtml(order: OrderResponseDto, type: 'status'|'delivery') {
  // ... (keep the existing email HTML builder function exactly as is)
  const qtySum = order.orderLines.reduce((s, x) => s + x.quantity, 0) || 1;
  const linesHtml = order.orderLines.map((l, idx) => `
  <tr>
    <td style="padding:8px 6px;border-bottom:1px solid #eee;">${idx + 1}</td>
    <td style="padding:8px 6px;border-bottom:1px solid #eee;">${l.productName}</td>
    <td style="padding:8px 6px;border-bottom:1px solid #eee;">${l.quantity}</td>
    <td style="padding:8px 6px;border-bottom:1px solid #eee;">${l.customText || '---'}</td>
    <td style="padding:8px 6px;border-bottom:1px solid #eee;text-align:right;">
      R${((l.quantity / qtySum) * order.totalPrice).toFixed(2)}
    </td>
  </tr>
  `).join('');

  return `
  <div style="max-width:660px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 4px 32px #e91e6340;padding:0 0 24px 0;font-family:'Segoe UI',Arial,sans-serif;color:#2b2b2b;">
    <div style="display:flex;align-items:center;justify-content:space-between;background:#f8e3ef;border-radius:12px 12px 0 0;padding:22px 28px;">
      <img src="https://i.postimg.cc/9Css0H5p/logo.jpg" alt="Logo" style="height:60px;"/>
      <div style="text-align:right;">
        <div style="font-size:24px;font-weight:700;letter-spacing:-1px;color:#e91e63;">By Sego and Bux</div>
        <div style="font-size:15px;color:#333;">Order #${order.orderID} <span style="color:#a47f99;">${type === 'delivery' ? '--- Delivery Update' : '--- Status Update'}</span></div>
      </div>
    </div>
    <div style="padding:28px 32px;">
      <div style="margin-bottom:12px;font-size:17px;">Hi <b>${order.customerName}</b>,</div>
      <div style="margin-bottom:18px;">
        ${type === 'status'
          ? `Your order status is now: <span style="color:#e91e63;font-weight:bold;">${order.orderStatusName}</span>.`
          : `Your order delivery is <span style="color:#e91e63;font-weight:bold;">${order.deliveryStatus}</span>.`}
      </div>
      <table style="width:100%;border-collapse:collapse;font-size:15px;margin:14px 0 24px;">
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
      <div style="font-size:15px;line-height:1.7;margin-bottom:14px;">
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
  selector: 'app-admin-manage-orders',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PaginatePipe,
    PaginationComponent,
    ConfirmDialogComponent,
    DeliveryCalendarComponent,
    CatDatePipe,
    CatRelativePipe
  ],
  templateUrl: './admin-manage-orders.component.html',
  styleUrls: ['./admin-manage-orders.component.scss']
})
export class AdminManageOrdersComponent implements OnInit, OnDestroy { // <-- ADD OnDestroy
  orders: OrderResponseDto[] = [];
  
  /** Order Status path: 1:Pending -> 2:Processing -> 3:Shipped */
  statusOptions = [1,2,3].map(id => ({ id, name: ['Pending','Processing','Shipped'][id-1] }));
  
  /** Delivery Status path: Pending -> Dispatched -> Delivered */
  deliveryOptionsOrdered = ['Pending','Dispatched','Delivered'];
  
  /** Search/filter state */
  query = '';
  statusFilter: '' | 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Dispatched' = '';
  
  modalImageUrl: string|null = null;
  modalPopupImage: string|null = null;
  selectedOrder: OrderResponseDto|null = null;
  confirmingStatus = false;
  confirmingDelivery = false;
  confirmMessage = '';
  private pendingOrder!: OrderResponseDto;
  
  page = 1;
  pageSize = 7;

  constructor(
    private orderService: OrderService,
    private toast: ToastService,
    private ordersStateService: OrdersStateService // <-- ADD THIS
  ) {}

  ngOnInit(): void { 
    this.loadOrders(); 
  }

  ngOnDestroy() {
    // Component cleanup if needed
  }

  private loadOrders(callback?: () => void) {
    this.orderService.getAllOrders().subscribe({
      next: data => { 
        this.orders = [...data]; 
        // <-- ADD THIS: Sync with the state service
        this.ordersStateService.updateFromOrders(this.orders);
        if (callback) callback(); 
      },
      error: e => this.toast.show('Failed to load orders: ' + e.message)
    });
  }

  /* ===================== SEARCH / FILTER use effectiveStatus ==================== */
  onFilterChanged() { this.page = 1; }

  private matchesQuery(o: OrderResponseDto, q: string): boolean {
    if (!q) return true;
    const t = q.toLowerCase().trim();
    const digits = t.replace(/[^0-9]/g, '');
    const orderIdHit = digits.length > 0 && String(o.orderID).includes(digits);
    
    const merged = effectiveStatus(o).toLowerCase();
    const statusHit = 
      merged.includes(t) ||
      (o.orderStatusName || '').toLowerCase().includes(t) ||
      (o.deliveryStatus || '').toLowerCase().includes(t);
    
    const customerHit = 
      (o.customerName || '').toLowerCase().includes(t) ||
      (o.customerEmail || '').toLowerCase().includes(t) ||
      (o.customerPhone || '').toLowerCase().includes(t) ||
      (o.deliveryAddress|| '').toLowerCase().includes(t);
    
    const logisticsHit = 
      (o.deliveryMethod || '').toLowerCase().includes(t) ||
      (o.courierProvider || '').toLowerCase().includes(t) ||
      (o.waybillNumber || '').toLowerCase().includes(t);
    
    return orderIdHit || statusHit || customerHit || logisticsHit;
  }

  get filteredOrders(): OrderResponseDto[] {
    const s = (this.statusFilter || '').toLowerCase();
    return this.orders
      .filter(o => {
        const byStatus = !s || effectiveStatus(o).toLowerCase() === s;
        const byQuery = this.matchesQuery(o, this.query || '');
        return byStatus && byQuery;
      })
      .sort((a, b) => b.orderID - a.orderID); // 🔑 highest ID first
  }

  trackByOrderId(index: number, item: OrderResponseDto) { return item.orderID; }

  /* ===================== Chip text/class driven by effectiveStatus ==================== */
  getDisplayStatus(o: OrderResponseDto): string { return effectiveStatus(o); }
  getDisplayStatusClass(o: OrderResponseDto): string { return 'status-' + effectiveStatus(o).toLowerCase(); }

  /* ===================== Modal + image helpers ==================== */
  openOrderModal(o: OrderResponseDto) { this.selectedOrder = { ...o }; this.waybillInvalid = false; this.attemptedDeliverySave = false; }
  closeOrderModal() { this.selectedOrder = null; this.closeModalImage(); }

  private nonEmpty(v: unknown): boolean { return typeof v === 'string' ? v.trim().length > 0 : !!v; }
  hasCustomText(line: any): boolean { return this.nonEmpty(line?.customText); }
  private hasCustomMedia(line: any): boolean {
    return this.nonEmpty(line?.uploadedImagePath) || this.nonEmpty(line?.snapshotPath);
  }

  private isNonDefault(val: any, defaults: any[]): boolean {
    if (val === null || val === undefined) return false;
    return !defaults.includes((typeof val === 'string' ? val.toLowerCase() : val));
  }

  showStyleMeta(line: any): boolean {
    if (line?.styleCustomized === true || line?.isCustomized === true) return true;
    
    const hasStyle = this.nonEmpty(line?.font) || this.nonEmpty(line?.fontSize) || this.nonEmpty(line?.color);
    if ((this.hasCustomText(line) || this.hasCustomMedia(line)) && hasStyle) return true;
    
    const COMMON_DEFAULT_FONTS = ['arial', 'helvetica', 'sans-serif'];
    const COMMON_DEFAULT_SIZES = [12, 14, 16, 18, 30];
    const COMMON_DEFAULT_COLORS = ['#000', '#000000', 'black', 'rgb(0,0,0)'];
    
    const fontNonDefault = this.isNonDefault((line?.font || '').toString().toLowerCase(), COMMON_DEFAULT_FONTS);
    const sizeNonDefault = this.isNonDefault(line?.fontSize, COMMON_DEFAULT_SIZES);
    const colorNonDefault = this.isNonDefault((line?.color || '').toString().toLowerCase(), COMMON_DEFAULT_COLORS);
    
    return fontNonDefault || sizeNonDefault || colorNonDefault;
  }

  openImageModal(url: string) { this.modalImageUrl = url; document.body.classList.add('modal-open'); }
  closeImageModal() { this.modalImageUrl = null; document.body.classList.remove('modal-open'); }
  openModalImage(url: string) { this.modalPopupImage = url; document.body.classList.add('modal-open'); }
  closeModalImage() { this.modalPopupImage = null; document.body.classList.remove('modal-open'); }

  @HostListener('document:keydown.escape') onEsc() {
    if (this.modalPopupImage) this.closeModalImage();
    else if (this.modalImageUrl) this.closeImageModal();
  }

  async downloadImage(url: string, name: string) {
    try {
      const res = await fetch(url, { mode: 'cors' });
      if (!res.ok) throw new Error(String(res.status));
      const blob = await res.blob();
      const objectUrl = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = objectUrl;
      a.download = `${name}.png`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
      this.toast.show('Image downloaded.');
    } catch {
      const a = document.createElement('a');
      a.href = url;
      a.download = `${name}.png`;
      a.target = '_self';
      a.rel = 'noopener';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      this.toast.show('Download started.');
    }
  }

  getFullImageUrl(path?: string | null): string {
    if (!path) return 'assets/no-image.png';
    if (path.startsWith('http')) return path;
    const trimmedApi = environment.apiUrl.replace(/\/$/, '');
    const normalized = path.startsWith('/') ? path : '/' + path;
    return `${trimmedApi}${normalized}`;
  }

  /* ===================== Persisted-state helpers (lock prev only after Save) ==================== */
  private originalOrderStatus(order: OrderResponseDto | null): number {
    if (!order) return 1;
    const orig = this.orders.find(x => x.orderID === order.orderID);
    return (orig?.orderStatusID ?? order.orderStatusID ?? 1) as number;
  }

  private originalDeliveryStatus(order: OrderResponseDto | null): string {
    if (!order) return 'Pending';
    const orig = this.orders.find(x => x.orderID === order.orderID);
    return (orig?.deliveryStatus ?? order.deliveryStatus ?? 'Pending') as string;
  }

  /** Order Status options: only current (persisted) and next allowed */
  isOrderStatusOptionDisabled(order: OrderResponseDto, optionId: number): boolean {
    const cur = this.originalOrderStatus(order);
    return optionId < cur || optionId > cur + 1;
  }

  /** Delivery select/input enabled only when persisted Order Status is Shipped (id=3) */
  canEditDelivery(order: OrderResponseDto | null): boolean {
    return this.originalOrderStatus(order!) >= 3;
  }

  /** Delivery options: only current (persisted) and next allowed */
  isDeliveryOptionDisabled(order: OrderResponseDto, opt: string): boolean {
    if (!this.canEditDelivery(order)) return true;
    const curIdx = this.deliveryIndex(this.originalDeliveryStatus(order));
    const optIdx = this.deliveryIndex(opt);
    return optIdx < curIdx || optIdx > curIdx + 1;
  }

  private deliveryIndex(v?: string|null): number {
    const s = (v || 'Pending').toString().toLowerCase();
    const i = this.deliveryOptionsOrdered.findIndex(x => x.toLowerCase() === s);
    return i < 0 ? 0 : i;
  }

  /* ===================== Waybill gating ==================== */
  @ViewChild('waybillInput') waybillInputRef!: ElementRef<HTMLInputElement>;
  waybillInvalid = false;
  attemptedDeliverySave = false;

  requiresWaybill(order: OrderResponseDto | null): boolean {
    if (!order) return false;
    const s = (order.deliveryStatus || '').toLowerCase();
    return s === 'dispatched' || s === 'delivered';
  }

  isWaybillEmpty(order: OrderResponseDto | null): boolean {
    if (!order) return true;
    return (order.waybillNumber || '').trim().length === 0;
  }

  isWaybillValid(order: OrderResponseDto | null): boolean {
    if (!order) return true;
    const w = (order.waybillNumber || '').trim();
    return w.length >= 5;
  }

  canSaveDelivery(order: OrderResponseDto | null): boolean {
    if (!order) return false;
    if (!this.canEditDelivery(order)) return false;
    if (this.requiresWaybill(order) && !this.isWaybillValid(order)) return false;
    return true;
  }

  onWaybillChanged() {
    this.waybillInvalid = false;
    this.attemptedDeliverySave = false;
  }

  onDeliveryStatusChange() {
    this.waybillInvalid = false;
    this.attemptedDeliverySave = false;
  }

  /* ===================== Update status / delivery ==================== */
  promptSaveStatus(order: OrderResponseDto) {
    this.waybillInvalid = false;
    this.attemptedDeliverySave = false;
    
    const desired = Number(order.orderStatusID);
    const cur = Number(this.originalOrderStatus(order));
    const nextAllowed = Math.min(cur + 1, 3);
    
    if (desired !== cur && desired !== nextAllowed) {
      this.toast.show('Order Status must progress sequentially. You can only move to the next stage.');
      if (this.selectedOrder) this.selectedOrder.orderStatusID = cur;
      return;
    }
    
    this.pendingOrder = order;
    const newName = this.statusOptions[desired - 1]?.name || 'Unknown';
    this.confirmMessage = `Change order #${order.orderID} status to "${newName}"?`;
    this.confirmingStatus = true;
  }

  promptSaveDelivery(order: OrderResponseDto) {
    if (!this.canEditDelivery(order)) {
      this.toast.show('Set Order Status to "Shipped" before updating delivery.');
      return;
    }
    
    const curIdx = this.deliveryIndex(this.originalDeliveryStatus(order));
    const newIdx = this.deliveryIndex(order.deliveryStatus);
    
    if (newIdx !== curIdx && newIdx !== curIdx + 1) {
      this.toast.show('Delivery must progress sequentially. You can only move to the next stage.');
      if (this.selectedOrder) this.selectedOrder.deliveryStatus = this.originalDeliveryStatus(order);
      return;
    }
    
    this.attemptedDeliverySave = true;
    const requires = this.requiresWaybill(order);
    const valid = this.isWaybillValid(order);
    
    if (requires && !valid) {
      this.waybillInvalid = true;
      this.toast.show('Enter a valid waybill (min 5 characters) before saving delivery.');
      setTimeout(() => this.waybillInputRef?.nativeElement?.focus(), 0);
      return;
    }
    
    this.waybillInvalid = false;
    this.pendingOrder = order;
    this.confirmMessage = `Change order #${order.orderID} delivery status to "${order.deliveryStatus}"?`;
    this.confirmingDelivery = true;
  }

  onStatusConfirmed() { this.confirmingStatus = false; this.applyChange('status'); }
  onStatusCanceled() { this.confirmingStatus = false; }
  onDeliveryConfirmed(){ this.confirmingDelivery = false; this.applyChange('delivery'); }
  onDeliveryCanceled() { this.confirmingDelivery = false; }

  private applyChange(type: 'status'|'delivery') {
    const o = this.pendingOrder!;
    
    if (type === 'status') {
      this.orderService.updateOrderStatus(o.orderID, Number(o.orderStatusID)).subscribe({
        next: () => {
          this.toast.show('Order status updated!');
          this.orderService.getOrderById(o.orderID).subscribe({
            next: (updated) => { 
              this.sendEmail(updated, 'status'); 
              this.loadOrders(() => {
                this.refreshSelected();
                // <-- ADD THIS: Update the state service after status change
                this.ordersStateService.updateFromOrders(this.orders);
              }); 
            },
            error: err => this.toast.show('Could not fetch updated order for email: ' + err.message)
          });
        },
        error: e => this.toast.show('Status update failed: ' + e.message)
      });
    } else {
      const payload: DeliveryUpdate = { deliveryStatus: o.deliveryStatus, waybillNumber: o.waybillNumber || '' };
      this.orderService.updateDelivery(o.orderID, payload).subscribe({
        next: () => {
          this.toast.show('Delivery status updated!');
          this.orderService.getOrderById(o.orderID).subscribe({
            next: (updated) => { 
              this.sendEmail(updated, 'delivery'); 
              this.loadOrders(() => {
                this.refreshSelected();
                // <-- ADD THIS: Update the state service after delivery change
                this.ordersStateService.updateFromOrders(this.orders);
              }); 
            },
            error: err => this.toast.show('Could not fetch updated order for email: ' + err.message)
          });
        },
        error: e => this.toast.show('Delivery update failed: ' + e.message)
      });
    }
  }

  private refreshSelected() {
    if (this.selectedOrder) {
      const found = this.orders.find(ord => ord.orderID === this.selectedOrder!.orderID);
      if (found) this.selectedOrder = { ...found };
      this.attemptedDeliverySave = false;
      this.waybillInvalid = false;
    }
  }

  private sendEmail(order: OrderResponseDto, type: 'status'|'delivery') {
    const html = buildOrderEmailHtml(order, type);
    const params = {
      to_name: order.customerName,
      to_email: order.customerEmail,
      subject: `Order #${order.orderID} ${type==='status'?'Status':'Delivery'} Update`,
      order_id: order.orderID,
      order_lines_html: html,
      deliveryMethod: order.deliveryMethod,
      deliveryStatus: order.deliveryStatus,
      waybillNumber: order.waybillNumber || 'N/A',
      deliveryAddress: order.deliveryAddress
    };
    
    emailjs.send(
      environment.emailJsServiceId,
      environment.emailJsOrderStatusTemplateId,
      params,
      environment.emailJsUserId
    );
  }

  get totalPages() { return Math.ceil(this.filteredOrders.length / this.pageSize) || 1; }

  /** Disable Save Status button once order is shipped */
  isSaveStatusDisabled(order: OrderResponseDto | null): boolean {
    if (!order) return true;
    return this.originalOrderStatus(order) >= 3; // 3 = Shipped
  }
}