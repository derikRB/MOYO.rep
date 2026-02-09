import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators
} from '@angular/forms';
import { FormsModule } from '@angular/forms';

import {
  TemplateService,
  TemplateDto
} from '../../services/template.service';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';

import { PaginatePipe } from '../../shared/pagination/pagination.pipe';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ToastService } from '../../shared/toast.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';

type DisplayTemplate = TemplateDto & { assignedNames: string };

@Component({
  selector: 'app-manage-templates',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,               // <-- needed for [(ngModel)] in search bar
    PaginatePipe,
    PaginationComponent,
    ConfirmDialogComponent
  ],
  templateUrl: './manage-templates.component.html',
  styleUrls: ['./manage-templates.component.scss']
})
export class ManageTemplatesComponent implements OnInit {
  @ViewChild('tplFormTop') tplFormTop!: ElementRef<HTMLElement>;

  templates: DisplayTemplate[] = [];
  products: Product[] = [];
  form!: FormGroup;
  editing?: DisplayTemplate;

  confirmingDelete = false;
  toDelete?: DisplayTemplate;

  // search
  searchTerm = '';

  // pagination
  page = 1;
  pageSize = 7;
  get totalPages() {
    return Math.ceil(this.filteredTemplates.length / this.pageSize) || 1;
  }

  submitted = false;

  constructor(
    private tplSvc: TemplateService,
    private prodSvc: ProductService,
    private fb: FormBuilder,
    private toastSvc: ToastService
  ) {}

  ngOnInit() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      file: [null],
      productIDs: [[], Validators.required]
    });

    this.prodSvc.getProducts().subscribe(ps => {
      this.products = ps;
      this.reloadTemplates();
    });
  }

  private reloadTemplates() {
    this.tplSvc.getAll().subscribe(list => {
      this.templates = list.map(t => ({
        ...t,
        assignedNames: (t.productIDs || [])
          .map(id => this.products.find(p => p.productID === id)?.name || '')
          .filter(n => !!n)
          .join(', ')
      }));
      if (this.editing) {
        this.editing = this.templates.find(x => x.templateID === this.editing!.templateID);
      }
    });
  }

  /* -------- Search helpers -------- */
  get filteredTemplates(): DisplayTemplate[] {
    const q = this.searchTerm.trim().toLowerCase();
    if (!q) return this.templates;
    return this.templates.filter(t =>
      (t.name || '').toLowerCase().includes(q) ||
      (t.assignedNames || '').toLowerCase().includes(q) ||
      String(t.templateID || '').includes(q)
    );
  }
  onSearchChanged() { this.page = 1; }

  startCreate() {
    this.editing = undefined;
    this.form.reset({ name: '', file: null, productIDs: [] });
    this.submitted = false;
    this.scrollToForm();
  }

  startEdit(t: DisplayTemplate) {
    this.editing = t;
    this.form.patchValue({
      name: t.name,
      file: null,
      productIDs: t.productIDs || []
    });
    this.submitted = false;
    this.scrollToForm();
  }

  onFileChange(evt: Event) {
    const file = (evt.target as HTMLInputElement).files?.[0] || null;
    this.form.get('file')!.setValue(file);
  }

  save() {
    this.submitted = true;
    if (this.form.invalid) {
      this.scrollToForm();
      this.focusFirstInvalid();
      return;
    }

    const fd = new FormData();
    fd.append('Name', this.form.value.name);
    if (this.form.value.file) {
      fd.append('File', this.form.value.file);
    }
    const [pid] = this.form.value.productIDs as number[];
    fd.append('ProductID', pid.toString());

    const op$ = this.editing
      ? this.tplSvc.update(this.editing.templateID, fd)
      : this.tplSvc.create(fd);

    op$.subscribe({
      next: () => {
        const verb = this.editing ? 'updated' : 'created';
        this.toastSvc.show(`Template ${verb}: “${this.form.value.name}”`);
        this.reloadTemplates();
      },
      error: e => {
        this.toastSvc.show(`Error: ${e.message}`);
      },
      complete: () => {
        this.startCreate();
      }
    });
  }

  promptDelete(t: DisplayTemplate) {
    this.toDelete = t;
    this.confirmingDelete = true;
  }

  onDeleteConfirmed() {
    if (!this.toDelete) return;
    const id = this.toDelete.templateID;
    const name = this.toDelete.name;
    this.tplSvc.delete(id).subscribe(() => {
      this.toastSvc.show(`Template deleted: “${name}”`);
      this.reloadTemplates();
    });
    this.confirmingDelete = false;
    this.toDelete = undefined;
  }

  onDeleteCanceled() {
    this.confirmingDelete = false;
    this.toDelete = undefined;
  }

  private scrollToForm() {
    setTimeout(() => {
      this.tplFormTop?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 0);
  }

  private focusFirstInvalid() {
    setTimeout(() => {
      const el = document.querySelector('.manage-templates .ng-invalid') as HTMLElement | null;
      el?.focus();
    }, 0);
  }
}
