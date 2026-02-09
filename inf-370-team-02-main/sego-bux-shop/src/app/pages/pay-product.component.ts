import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  standalone: true,
  selector: 'app-pay-product',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './pay-product.component.html',
  styleUrls: ['./pay-product.component.scss']
})
export class PayProductComponent implements OnInit {
  product: any = null;
  form!: FormGroup;
  paid = false;

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    const productId = +this.route.snapshot.paramMap.get('id')!;
    this.http.get(`${environment.apiUrl}/products/${productId}`).subscribe({
      next: (res: any) => this.product = res
    });

    this.form = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]]
    });
  }

  pay(): void {
    const { name, email } = this.form.value;
    const handler = (window as any).PaystackPop.setup({
      key: environment.paystackPublicKey,
      email,
      amount: this.product.price * 100,
      currency: 'ZAR',
      ref: 'PAY-' + Math.floor(Math.random() * 1000000000),
      metadata: {
        custom_fields: [{ display_name: name, variable_name: 'customer_name', value: name }]
      },
      callback: () => {
        this.paid = true;
        // Optional: save transaction to backend here
      },
      onClose: () => alert('Payment window closed.')
    });
    handler.openIframe();
  }
}
