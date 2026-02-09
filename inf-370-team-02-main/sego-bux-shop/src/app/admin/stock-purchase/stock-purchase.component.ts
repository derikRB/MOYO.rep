import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import {
  StockService,
  StockPurchase,
  CaptureDto,
  PurchaseLine
} from '../../services/admin/stock/stock.service';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';
import { ToastService } from '../../shared/toast.service';   // ← fixed import path

@Component({
  selector: 'app-stock-purchase',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './stock-purchase.component.html',
  styleUrls: ['./stock-purchase.component.scss']
})
export class StockPurchaseComponent implements OnInit {
  form!: FormGroup;
  products: Product[]        = [];
  purchases: StockPurchase[] = [];

  constructor(
    private fb: FormBuilder,
    private stockSvc: StockService,
    private productSvc: ProductService,
    private toast: ToastService              // ← now injects correctly
  ) {}

  ngOnInit() {
    this.loadProducts();
    this.buildForm();
    this.loadPurchases();
  }

  private loadProducts() {
    this.productSvc.getProducts().subscribe({
      next: list => this.products = list,
      error: err => this.toast.show('Could not load products: ' + err.message)
    });
  }

  private buildForm() {
    this.form = this.fb.group({
      supplierName: ['', Validators.required],
      lines: this.fb.array([ this.createLine() ])
    });
  }

  private loadPurchases() {
    this.stockSvc.getAllPurchases().subscribe({
      next: data => this.purchases = data,
      error: err => this.toast.show('Error loading purchases: ' + err.message)
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  createLine(): FormGroup {
    return this.fb.group({
      productId: [null, Validators.required],
      quantity:  [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]]
    });
  }

  addLine() {
    this.lines.push(this.createLine());
  }

  removeLine(i: number) {
    this.lines.removeAt(i);
  }

  submit() {
    if (this.form.invalid) return;

    const payload: CaptureDto = this.form.value;
    this.stockSvc.capturePurchase(payload).subscribe({
      next: () => {
        this.toast.show('Stock purchase recorded.');
        this.form.reset();
        this.lines.clear();
        this.addLine();
        this.loadPurchases();
      },
      error: err => {
        this.toast.show('❌ Error capturing purchase: ' + err.message);
      }
    });
  }
}
