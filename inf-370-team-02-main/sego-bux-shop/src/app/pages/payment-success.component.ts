import { Component } from '@angular/core';

@Component({
  standalone: true,
  selector: 'app-payment-success',
  template: `
    <div class="payment-status">
      <h2>🎉 Payment Successful!</h2>
      <p>Your order was received and payment is confirmed.</p>
      <a routerLink="/account" class="btn">Go to My Account</a>
    </div>
  `,
  styles: [`
    .payment-status { text-align: center; margin-top: 40px; }
    .btn { margin-top: 24px; color: #fff; background: #e91e63; border: none; padding: 10px 28px; border-radius: 20px; font-size: 1.2em;}
  `]
})
export class PaymentSuccessComponent {}
