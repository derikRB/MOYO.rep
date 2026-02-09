import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormsModule,
  FormBuilder,
  FormGroup,
  Validators
} from '@angular/forms';
import { FaqService }           from '../../services/admin/faq/faq.service';
import { FaqItem }              from '../../models/faq-item.model';
import { ToastService }         from '../../shared/toast.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';

@Component({
  selector: 'app-faq-manager',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ConfirmDialogComponent
  ],
  templateUrl: './faq-manager.component.html',
  styleUrls: ['./faq-manager.component.scss']
})
export class FaqManagerComponent implements OnInit {
  faqs: FaqItem[] = [];
  categories: string[] = [];
  selectedCategory = 'All';
  form!: FormGroup;
  editing = false;

  // pagination
  currentPage = 1;
  pageSize = 10;

  // delete confirmation state
  confirmingDelete = false;
  toDeleteId: number | null = null;

  constructor(
    private fb:     FormBuilder,
    private svc:    FaqService,
    private toast:  ToastService
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadFaqs();
  }

  private initForm(): void {
    this.form = this.fb.group({
      faqId:           [0],
      category:        ['', Validators.required],
      questionVariant: ['', Validators.required],
      answer:          ['', Validators.required],
      sortOrder:       [1, [Validators.required, Validators.min(1)]]
    });
  }

  private loadFaqs(): void {
    this.svc.getAll().subscribe(data => {
      this.faqs = data;
      this.categories = ['All', ...new Set(data.map(f => f.category))];
      this.currentPage = 1;
    });
  }

  save(): void {
    if (this.form.invalid) return;
    const faq = this.form.value as FaqItem;
    const op = faq.faqId
      ? this.svc.update(faq)
      : this.svc.create(faq);

    op.subscribe(() => {
      this.toast.show(
        faq.faqId
          ? `FAQ #${faq.faqId} updated!`
          : `New FAQ added!`
      );
      this.cancel();
      this.loadFaqs();
    });
  }

  cancel(): void {
    this.editing = false;
    this.form.reset({
      faqId:           0,
      category:        '',
      questionVariant: '',
      answer:          '',
      sortOrder:       1
    });
  }

  onCategoryFilterChange(): void {
    this.currentPage = 1;
  }

  edit(item: FaqItem): void {
    this.editing = true;
    this.form.patchValue(item);
  }

  promptDelete(id: number): void {
    this.toDeleteId = id;
    this.confirmingDelete = true;
  }

  onDeleteConfirmed(): void {
    if (this.toDeleteId == null) return;
    this.svc.delete(this.toDeleteId).subscribe(() => {
      this.toast.show(`FAQ #${this.toDeleteId} deleted!`);
      this.loadFaqs();
    });
    this.confirmingDelete = false;
    this.toDeleteId = null;
  }

  onDeleteCanceled(): void {
    this.confirmingDelete = false;
    this.toDeleteId = null;
  }

  exportToChatbot(): void {
    this.svc.exportToNlu().subscribe({
      next: () => this.toast.show('Exported to chatbot!'),
      error: e => this.toast.show('Export failed: ' + e.message)
    });
  }

  get filteredFaqs(): FaqItem[] {
    return this.selectedCategory === 'All'
      ? this.faqs
      : this.faqs.filter(f => f.category === this.selectedCategory);
  }

  get pagedFaqs(): FaqItem[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredFaqs.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredFaqs.length / this.pageSize));
  }

  get pages(): any[] {
    return Array(this.totalPages);
  }
}
