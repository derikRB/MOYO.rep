// src/app/manage-vat/manage-vat.component.ts
import { Component, OnInit }       from '@angular/core';
import { CommonModule }            from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl
}                                  from '@angular/forms';
import { FormsModule }             from '@angular/forms'; // <-- for [(ngModel)]
import { RouterModule }            from '@angular/router';

import { VatService, Vat }         from '../../../services/vat.service';
import { ToastService }            from '../../../shared/toast.service';

type StatusFilter = 'All' | 'Active' | 'Inactive';

@Component({
  selector:    'app-manage-vat',
  standalone:  true,
  imports:     [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './manage-vat.component.html',
  styleUrls:   ['./manage-vat.component.scss']
})
export class ManageVatComponent implements OnInit {
  vats: Vat[] = [];
  form!: FormGroup;
  loading = false;

  // ---- search + filter ----
  searchTerm = '';
  statusFilter: StatusFilter = 'All';

  constructor(
    private vatService: VatService,
    private fb:          FormBuilder,
    private toastSvc:    ToastService
  ) {}

  ngOnInit(): void {
    this.loadAll();

    this.form = this.fb.group({
      vatName:       ['', [ Validators.required ]],
      percentage:    [0,   [ Validators.required, Validators.min(0), Validators.max(100) ]],
      effectiveDate: [
        this.today(),
        [ Validators.required, this.duplicateDateValidator.bind(this) ]
      ]
    });

    const effCtrl = this.form.get('effectiveDate')!;
    effCtrl.valueChanges.subscribe(() => {
      effCtrl.updateValueAndValidity({ onlySelf: true, emitEvent: false });
    });
  }

  // handy control getters for template
  get percentageCtrl(): AbstractControl { return this.form.get('percentage')!; }
  get effectiveDateCtrl(): AbstractControl { return this.form.get('effectiveDate')!; }

  private loadAll(): void {
    this.vatService.getAll().subscribe((list: Vat[]) => {
      this.vats = list;
      this.form.get('effectiveDate')?.updateValueAndValidity();
    });
  }

  /** Derived view with search + status filter */
  get filteredVats(): Vat[] {
    const q = this.searchTerm.trim().toLowerCase();
    return this.vats
      .filter(v => {
        if (this.statusFilter !== 'All') {
          const isActive = v.status === 'Active';
          if (this.statusFilter === 'Active' && !isActive) return false;
          if (this.statusFilter === 'Inactive' && isActive) return false;
        }
        if (!q) return true;

        const name  = (v.vatName || '').toLowerCase();
        const pct   = String(v.percentage ?? '').toLowerCase();
        const stat  = (v.status || '').toLowerCase();
        const eff   = new Date(v.effectiveDate).toLocaleDateString().toLowerCase();
        const idStr = String((v as any).vatId ?? '').toLowerCase();

        return (
          name.includes(q) || pct.includes(q) || stat.includes(q) ||
          eff.includes(q)  || idStr.includes(q)
        );
      });
  }

  onSearchChanged(){ /* pagination-free; nothing else needed */ }
  setStatusFilter(v: StatusFilter){ this.statusFilter = v; }

  /** User clicked “Add VAT” */
  create(): void {
    const dateCtrl = this.form.get('effectiveDate')!;
    const newDate  = dateCtrl.value as string;

    const pct = Number(this.percentageCtrl.value ?? 0);
    if (pct < 0) {
      this.percentageCtrl.setValue(0);
      this.percentageCtrl.markAsTouched();
    }

    if (this.isDuplicateDate(newDate)) {
      dateCtrl.setErrors({ duplicateDate: true });
      dateCtrl.markAsTouched();
      this.toastSvc.show(` A VAT rate for ${newDate} already exists.`);
      return;
    }

    if (this.form.invalid) return;

    this.loading = true;
    const name = this.form.value.vatName;

    this.vatService.create(this.form.value).subscribe({
      next: () => {
        this.toastSvc.show(` VAT added: “${name}”`);
        this.resetForm();
        this.loadAll();
        this.loading = false;
      },
      error: error => {
        if (
          error.status === 400 &&
          typeof error.error === 'string' &&
          error.error.toLowerCase().includes('effective date')
        ) {
          this.toastSvc.show(
            ` A VAT rate for ${newDate} already exists. Please pick another date.`
          );
        } else {
          this.toastSvc.show('Failed to add VAT. Please try again.');
        }
        this.loading = false;
      }
    });
  }

  activate(id: number): void {
    const vat = this.vats.find(v => v.vatId === id);
    this.vatService.activate(id).subscribe(() => {
      this.loadAll();
      if (vat) this.toastSvc.show(` VAT activated: “${vat.vatName}”`);
    });
  }

  /** Validator: flags duplicate if any existing VAT date matches YYYY-MM-DD */
  private duplicateDateValidator(ctrl: AbstractControl) {
    const d = ctrl.value as string;
    return this.isDuplicateDate(d) ? { duplicateDate: true } : null;
  }

  private isDuplicateDate(dateStr: string): boolean {
    return this.vats.some(v => this.normalize(v.effectiveDate) === dateStr);
  }

  private normalize(input: string|Date): string {
    return new Date(input).toISOString().substring(0,10);
  }

  private today(): string {
    return new Date().toISOString().substring(0,10);
  }

  private resetForm() {
    this.form.reset({
      vatName:       '',
      percentage:    0,
      effectiveDate: this.today()
    });
  }
}
