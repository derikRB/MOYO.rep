import { Component, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  FormControl
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  AdjustmentService,
  StockAdjustmentDto,
  StockAdjustment,
  StockReason
} from '../../../services/admin/adjustment/adjustment.service';
import { ProductService } from '../../../services/product.service';
import { AdminService, Employee } from '../../../services/admin.service';
import { Product } from '../../../models/product.model';
import { ToastService } from '../../../shared/toast.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-stock-adjustment',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './stock-adjustment.component.html',
  styleUrls: ['./stock-adjustment.component.scss']
})
export class StockAdjustmentComponent implements OnInit {
  form!: FormGroup;

  reasonForm!: FormGroup<{
    name: FormControl<string>;
    sortOrder: FormControl<number>;
    isActive: FormControl<boolean>;
  }>;
  triedReasonSubmit = false;

  products: Product[] = [];
  employees: Employee[] = []; // retained for history display label mapping
  adjustments: StockAdjustment[] = [];
  reasons: StockReason[] = [];

  showInactive = false;
  editingReason: StockReason | null = null;
  recordedAdjustment: StockAdjustment | null = null;
  errorMessages: string[] = [];

  currentStaffId: number | null = null;
  currentStaffDisplay = '';

  constructor(
    private fb: FormBuilder,
    private adjSvc: AdjustmentService,
    private prodSvc: ProductService,
    private adminSvc: AdminService,
    private toast: ToastService,
    private auth: AuthService
  ) {}

  ngOnInit() {
    const ident = this.auth.getStaffIdentity();
    this.currentStaffId = ident.id;
    this.currentStaffDisplay = this.auth.getStaffDisplay();

    this.prodSvc.getProducts().subscribe({
      next: p => (this.products = p),
      error: e => this.toast.show('Failed to load products: ' + e.message)
    });
    this.adminSvc.getEmployees().subscribe({
      next: e => (this.employees = e),
      error: e => this.toast.show('Failed to load employees: ' + e.message)
    });
    this.loadAdjustments();
    this.refreshReasons();

    this.form = this.fb.group({
      productId: [null, [Validators.required]],
      adjustmentQty: [0, [Validators.required]],
      reason: [null, [Validators.required]],
      adjustedBy: [this.currentStaffId, [Validators.required]] // hidden, enforced server-side anyway
    });

    this.reasonForm = this.fb.group({
      name: this.fb.control<string>('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(80)] }),
      sortOrder: this.fb.control<number>(10, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
      isActive: this.fb.control<boolean>(true, { nonNullable: true })
    });
  }

  get rf() { return this.reasonForm.controls; }

  get activeReasons(): StockReason[] {
    return this.reasons
      .filter(r => r.isActive)
      .sort((a,b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
  }

  get tableReasons(): StockReason[] {
    const list = this.showInactive ? this.reasons : this.reasons.filter(r => r.isActive);
    return [...list].sort((a,b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
  }

  private loadAdjustments() {
    this.adjSvc.getAllAdjustments().subscribe({
      next: list => (this.adjustments = list),
      error: e => this.toast.show('Failed to load adjustments: ' + e.message)
    });
  }

  refreshReasons() {
    this.adjSvc.getReasons(this.showInactive).subscribe({
      next: list => (this.reasons = list),
      error: e => this.toast.show('Failed to load reasons: ' + e.message)
    });
  }

  getProductLabel(id: number | string): string {
    const p = this.products.find(x => x.productID === +id);
    return p ? `${p.name} (On-hand: ${p.stockQuantity})` : id.toString();
  }
  getEmployeeLabel(id: number | string): string {
    const emp = this.employees.find(x => x.employeeID === +id);
    return emp ? `${emp.username} (${emp.role})` : id.toString();
  }

  submit() {
    this.errorMessages = [];
    this.recordedAdjustment = null;

    if (!this.currentStaffId) {
      this.toast.show('You must be logged in as staff to adjust stock.');
      return;
    }
    if (this.form.invalid) {
      this.errorMessages.push('Please complete all required fields.');
      return;
    }

    const dto: StockAdjustmentDto = {
      productId: +this.form.value.productId,
      adjustmentQty: +this.form.value.adjustmentQty,
      reason: String(this.form.value.reason),
      adjustedBy: String(this.currentStaffId) // UI value, server enforces from JWT
    };

    this.adjSvc.adjustStock(dto).subscribe({
      next: adj => {
        this.recordedAdjustment = adj;
        this.toast.show(`Adjustment #${adj.stockAdjustmentId} recorded!`);
        this.loadAdjustments();
        this.form.patchValue({ adjustmentQty: 0, reason: null });
      },
      error: err => {
        this.toast.show('Failed to apply adjustment.');
        const validation = err.error?.errors || {};
        Object.keys(validation).forEach(field =>
          validation[field].forEach((msg: string) => this.errorMessages.push(`${field}: ${msg}`))
        );
      }
    });
  }

  // ===== Reason CRUD =====
  startEdit(reason: StockReason) {
    this.editingReason = reason;
    this.triedReasonSubmit = false;
    this.reasonForm.setValue({
      name: reason.name,
      sortOrder: reason.sortOrder,
      isActive: reason.isActive
    });
  }
  cancelEdit() {
    this.editingReason = null;
    this.triedReasonSubmit = false;
    this.reasonForm.reset({ name: '', sortOrder: 10, isActive: true });
  }
  saveReason() {
    this.triedReasonSubmit = true;
    if (this.reasonForm.invalid) return;

    const dto = {
      name: this.reasonForm.value.name!.trim(),
      sortOrder: Number(this.reasonForm.value.sortOrder ?? 10),
      isActive: Boolean(this.reasonForm.value.isActive)
    };

    if (this.editingReason) {
      this.adjSvc.updateReason(this.editingReason.stockReasonId, dto).subscribe({
        next: _ => {
          this.toast.show('Reason updated');
          this.cancelEdit();
          this.refreshReasons();
        },
        error: e => this.toast.show(e.error ?? 'Failed to update reason')
      });
    } else {
      this.adjSvc.createReason(dto).subscribe({
        next: _ => {
          this.toast.show('Reason created');
          this.cancelEdit();
          this.refreshReasons();
        },
        error: e => this.toast.show(e.error ?? 'Failed to create reason')
      });
    }
  }
  deactivateReason(r: StockReason) {
    this.adjSvc.deleteReason(r.stockReasonId).subscribe({
      next: () => {
        this.toast.show('Reason deactivated');
        this.refreshReasons();
      },
      error: e => this.toast.show(e.error ?? 'Failed to deactivate reason')
    });
  }
  activateReason(r: StockReason) {
    const dto = { name: r.name, sortOrder: r.sortOrder, isActive: true };
    this.adjSvc.updateReason(r.stockReasonId, dto).subscribe({
      next: () => {
        this.toast.show('Reason activated');
        this.refreshReasons();
      },
      error: e => this.toast.show(e.error ?? 'Failed to activate reason')
    });
  }
}
