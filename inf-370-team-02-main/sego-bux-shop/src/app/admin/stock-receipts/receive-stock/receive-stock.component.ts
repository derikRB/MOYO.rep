import { Component, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  FormArray,
  Validators,
  ReactiveFormsModule
} from '@angular/forms';
import { CommonModule } from '@angular/common';

import {
  ReceiptService,
  StockPurchase,
  StockPurchaseLine,
  StockReceiptDto,
  ReceiptLineDto,
  StockReceiptResponseDto
} from '../../../services/admin/receipt/receipt.service';
import { AdminService } from '../../../services/admin.service';
import { Employee }     from '../../../models/employee';
import { ToastService } from '../../../shared/toast.service';
import { AuthService }  from '../../../services/auth.service';

@Component({
  selector: 'app-receive-stock',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './receive-stock.component.html',
  styleUrls: ['./receive-stock.component.scss']
})
export class ReceiveStockComponent implements OnInit {
  form!: FormGroup;

  purchases: StockPurchase[] = [];
  employees: Employee[]      = []; // kept for rendering names in history/success
  receipts:  StockReceiptResponseDto[] = [];

  recordedReceipt: StockReceiptResponseDto | null = null;
  errorMessages: string[] = [];

  // 🔑 current staff from JWT
  staffId: number | null = null;
  staffDisplay = 'Unknown User';

  constructor(
    private fb: FormBuilder,
    private receiptSvc: ReceiptService,
    private adminSvc:   AdminService,
    private toast:      ToastService,
    private auth:       AuthService
  ) {}

  ngOnInit() {
    // identity
    const ident = this.auth.getStaffIdentity();
    this.staffId = ident.id;
    this.staffDisplay = this.auth.getStaffDisplay();

    // selectors
    this.receiptSvc.getAllPurchases().subscribe({
      next: p => (this.purchases = p),
      error: e => this.toast.show('Failed to load purchases: ' + e.message)
    });
    this.adminSvc.getEmployees().subscribe({
      next: e => (this.employees = e),
      error: e => this.toast.show('Failed to load employees: ' + e.message)
    });

    // history
    this.receiptSvc.getAllReceipts().subscribe(r => (this.receipts = r));

    // form
    this.form = this.fb.group({
      stockPurchaseId: [null, Validators.required],
      receivedBy:      [this.staffId, Validators.required], // auto-populated
      lines:           this.fb.array([], Validators.required)
    });

    // when purchase changes -> rebuild lines
    this.form.get('stockPurchaseId')!.valueChanges.subscribe((id: number | null) => {
      this.clearLines();
      this.recordedReceipt = null;
      if (id) this.loadPurchaseLines(id);
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  private clearLines() {
    while (this.lines.length) this.lines.removeAt(0);
  }

  /** create a line with SAME validation as before: min 1 */
  private createLine(productId: number, productName: string) {
    return this.fb.group({
      productId:        [productId],
      productName:      [{ value: productName, disabled: true }],
      quantityReceived: [0, [Validators.required, Validators.min(1)]]
    });
  }

  private loadPurchaseLines(purchaseId: number) {
    this.receiptSvc.getPurchaseById(purchaseId).subscribe({
      next: purchase => {
        (purchase.lines || []).forEach((l: StockPurchaseLine) =>
          this.lines.push(this.createLine(l.productId, l.product.name))
        );
      },
      error: e => this.toast.show(`Could not load purchase #${purchaseId}: ${e.message}`)
    });
  }

  getEmployeeLabel(id: number|string): string {
    const emp = this.employees.find(x => x.employeeID === +id);
    return emp ? `${emp.username} (${emp.role})` : `#${id}`;
  }

  submit() {
    this.errorMessages   = [];
    this.recordedReceipt = null;

    if (!this.staffId) {
      this.toast.show('You must be logged in as staff to receive stock.');
      return;
    }
    if (this.form.invalid) {
      this.errorMessages.push('Please fill in all required fields.');
      return;
    }

    const dto: StockReceiptDto = {
      stockPurchaseId: Number(this.form.value.stockPurchaseId),
      // even though the server overwrites from JWT, we still send our ID
      receivedBy:      String(this.staffId),
      lines: this.lines.controls.map(ctrl => ({
        productId:        Number(ctrl.get('productId')!.value),
        quantityReceived: Number(ctrl.get('quantityReceived')!.value)
      } as ReceiptLineDto))
    };

    this.receiptSvc.receiveStock(dto).subscribe({
      next: receipt => {
        this.recordedReceipt = receipt;
        this.toast.show(`Receipt #${receipt.stockReceiptId} recorded!`);
        this.form.reset({ stockPurchaseId: null, receivedBy: this.staffId });
        this.clearLines();
        this.receiptSvc.getAllReceipts().subscribe(r => (this.receipts = r));
      },
      error: err => {
        const valErrors = err.error?.errors || {};
        Object.keys(valErrors).forEach(field =>
          valErrors[field].forEach((m: string) => this.errorMessages.push(`${field}: ${m}`))
        );
        this.toast.show('Failed to record receipt.');
      }
    });
  }
}
