import { Component, OnInit, AfterViewInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { CustomerService, UpdateCustomerDto } from '../../services/customer.service';
import { CartService, CartItem } from '../../services/cart.service';
import { CustomizationService } from '../../services/customization.service';
import { OrderService } from '../../services/order.service';
import { ToastService } from '../../shared/toast.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { ToastContainerComponent } from '../../shared/toast-container.component';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';

declare const google: any;

@Component({
  selector: 'app-customer-profile',
  standalone: true,
  providers: [CurrencyPipe],
  imports: [CommonModule, ReactiveFormsModule, ConfirmDialogComponent, ToastContainerComponent],
  templateUrl: './customer-profile.component.html',
  styleUrls: ['./customer-profile.component.scss']
})
export class CustomerProfileComponent implements OnInit, AfterViewInit {
  customerId!: number;

  cartItems: CartItem[] = [];
  customizingItem: CartItem | null = null;
  showCustomizationModal = false;
  isSending = false;

  profileForm!: FormGroup;
  passwordForm!: FormGroup;

  profileError = '';
  passwordError = '';

  confirming = false;
  confirmMessage = '';
  confirmAction: (() => void) | null = null;

  constructor(
    private fb: FormBuilder,
    private customerService: CustomerService,
    private authService: AuthService,
    private cartService: CartService,
    private customizationService: CustomizationService,
    private router: Router,
    private orderService: OrderService,
    private currencyPipe: CurrencyPipe,
    public toast: ToastService
  ) {}

  /** Enable CTA only when user changed something AND the form is valid */
  get canSubmitProfile(): boolean {
    return this.profileForm?.valid && this.profileForm?.dirty;
  }
  get canSubmitPassword(): boolean {
    return this.passwordForm?.valid && this.passwordForm?.dirty;
  }

  ngOnInit(): void {
    const token = this.authService.getToken();
    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    const payload = JSON.parse(atob(token.split('.')[1]));
    const claimKey = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
    this.customerId = +payload[claimKey];

    // Validators aligned to Sign-In/Register
    this.profileForm = this.fb.group({
      username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20), Validators.pattern(/^[A-Za-z0-9_]+$/)]],
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(40), Validators.pattern(/^[A-Za-z]+(?:[ '\-][A-Za-z]+)*$/)]],
      surname: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(40), Validators.pattern(/^[A-Za-z]+(?:[ '\-][A-Za-z]+)*$/)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^0\d{9}$/)]], // starts with 0 + exactly 10 digits
      address: ['', Validators.required]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });

    // Pre-populate and start pristine (button disabled)
    this.customerService.getCustomerById(this.customerId).subscribe({
      next: c => {
        this.profileForm.patchValue(c);
        this.profileForm.markAsPristine();
        this.profileForm.updateValueAndValidity({ emitEvent: false });
      },
      error: () => this.toast.show('Failed to load profile data.')
    });

    this.loadCart();
  }

  ngAfterViewInit(): void {
    const el = document.getElementById('autocomplete') as HTMLInputElement | null;
    if (el && typeof google !== 'undefined' && google.maps?.places) {
      const ac = new google.maps.places.Autocomplete(el, { types: ['geocode'] });
      ac.addListener('place_changed', () => {
        const place = ac.getPlace();
        if (place?.formatted_address) {
          this.profileForm.patchValue({ address: place.formatted_address });
        }
      });
    }
  }

  // --- helpers for validation messages
  pErr(ctrl: string, name: string): boolean {
    const c = this.profileForm.get(ctrl);
    return !!(c && c.touched && c.hasError(name));
  }
  pwErr(ctrl: string, name: string): boolean {
    const c = this.passwordForm.get(ctrl);
    return !!(c && c.touched && c.hasError(name));
  }
  private passwordMatchValidator(form: AbstractControl) {
    const a = form.get('newPassword')?.value;
    const b = form.get('confirmNewPassword')?.value;
    return a === b ? null : { mismatch: true };
  }

  // --- profile flow with confirm dialog + toasts
  updateProfile(): void {
    this.profileError = '';
    if (!this.canSubmitProfile) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.confirmMessage = 'Update your profile details?';
    this.confirmAction = () => {
      this.customerService.updateCustomer(this.customerId, this.profileForm.value as UpdateCustomerDto).subscribe({
        next: () => {
          this.toast.show('Profile updated successfully.');
          this.profileForm.markAsPristine(); // disable again until next change
        },
        error: err => {
          this.profileError = err?.error?.message || err?.error || err?.message || 'Update failed.';
        }
      });
    };
    this.confirming = true;
  }

  // --- password flow with confirm dialog + toasts
  updatePassword(): void {
    this.passwordError = '';
    if (!this.canSubmitPassword) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.confirmMessage = 'Update your password?';
    this.confirmAction = () => {
      this.customerService.updatePassword(this.customerId, this.passwordForm.value).subscribe({
        next: () => {
          this.toast.show('Password updated successfully.');
          this.passwordForm.reset();
          this.passwordForm.markAsPristine();
        },
        error: err => {
          this.passwordError = err?.error?.message || err?.error || err?.message || 'Failed to update password.';
        }
      });
    };
    this.confirming = true;
  }

  // --- confirm dialog handlers
  onConfirmed(): void {
    if (this.confirmAction) this.confirmAction();
    this.confirming = false;
    this.confirmAction = null;
  }
  onCanceled(): void {
    this.confirming = false;
    this.confirmAction = null;
  }

  // --- cart (unchanged)
  loadCart(): void { this.cartItems = this.cartService.getItems(); }
  removeItem(item: CartItem): void { this.cartService.remove(item); this.loadCart(); }
  openCustomizationModal(item: CartItem): void { this.customizingItem = item; this.showCustomizationModal = true; }
  closeCustomizationModal(): void { this.customizingItem = null; this.showCustomizationModal = false; }
  onCustomizationSaved(customization: any): void {
    if (this.customizingItem) {
      this.cartService.updateCustomization(this.customizingItem, customization);
      this.loadCart();
      this.toast.show('Customization saved.');
      this.closeCustomizationModal();
    }
  }
  increaseQuantity(item: CartItem): void { this.cartService.increment(item); this.loadCart(); }
  decreaseQuantity(item: CartItem): void { this.cartService.decrement(item); this.loadCart(); }

  getSubtotal(): number { return this.cartService.getSubtotal(); }
  getVAT(): number { return this.cartService.getVATAmount(); }
  getTotal(): number { return this.cartService.getTotalWithVAT(); }

  formatCurrency(amount: number): string {
    return this.currencyPipe.transform(amount, 'ZAR', 'symbol-narrow', '1.2-2') || 'R0.00';
  }

  downloadInvoice(): void {
    const cart = document.getElementById('invoice-section');
    if (!cart) return;
    html2canvas(cart).then(canvas => {
      const imgData = canvas.toDataURL('image/png');
      const pdf = new jsPDF('p', 'mm', 'a4');
      const imgProps = pdf.getImageProperties(imgData);
      const pdfWidth = pdf.internal.pageSize.getWidth();
      const pdfHeight = (imgProps.height * pdfWidth) / imgProps.width;
      pdf.addImage(imgData, 'PNG', 0, 0, pdfWidth, pdfHeight);
      pdf.save('invoice.pdf');
    });
  }
deleteAccount(): void {
  this.confirmMessage = 'Delete your account?';
  this.confirmAction = () => {
    this.customerService.deleteCustomer(this.customerId).subscribe({
      next: () => {
        this.toast.show('Account deleted.');
        this.authService.logout();
        this.router.navigate(['/']);
      },
      error: () => this.toast.show('Failed to delete account.')
    });
  };
  this.confirming = true;
}

  async checkout(): Promise<void> {
    if (this.cartItems.length === 0) {
      this.toast.show('Your cart is empty.');
      return;
    }
    this.isSending = true;
    try {
      await new Promise(r => setTimeout(r, 1500));
      this.toast.show('Order placed successfully! (Simulated)');
      this.cartService.clearCart();
      this.loadCart();
    } finally {
      this.isSending = false;
    }
  }
}

