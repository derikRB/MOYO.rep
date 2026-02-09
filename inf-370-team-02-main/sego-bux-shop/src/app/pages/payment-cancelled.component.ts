import { Component } from '@angular/core';

@Component({
  standalone: true,
  selector: 'app-payment-cancelled',
  template: `
    <div class="payment-status">
      <h2>❌ Payment Cancelled</h2>
      <p>Your payment was cancelled or failed. No money was deducted.</p>
      <a routerLink="/cart" class="btn">Back to Cart</a>
    </div>
  `,
  styles: [`
    .payment-status { text-align: center; margin-top: 40px; }
    .btn { margin-top: 24px; color: #fff; background: #e91e63; border: none; padding: 10px 28px; border-radius: 20px; font-size: 1.2em;}
  `]
})
export class PaymentCancelledComponent {}
