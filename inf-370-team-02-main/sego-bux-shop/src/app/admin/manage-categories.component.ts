import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { AdminService } from '../services/admin.service';
import { PaginatePipe } from '../shared/pagination/pagination.pipe';
import { PaginationComponent } from '../shared/pagination/pagination.component';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';
import { ToastService } from '../shared/toast.service';

@Component({
  selector: 'app-manage-categories',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PaginatePipe,
    PaginationComponent,
    ConfirmDialogComponent
  ],
  templateUrl: './manage-categories.component.html',
  styleUrls: ['./manage-categories.component.scss']
})
export class ManageCategoriesComponent implements OnInit {
  categories: any[] = [];

  // search
  searchTerm = '';

  form = {
    categoryName: '',
    description: ''
  };
  isEditMode = false;
  editingCategoryID: number | null = null;

  // inline confirm dialog
  confirmingDeleteCategory = false;
  toDeleteCategory: any | null = null;

  // pagination
  page = 1;
  pageSize = 7;
  get totalPages() {
    return Math.ceil(this.filteredCategories.length / this.pageSize) || 1;
  }

  // UI helpers
  catSubmitted = false;

  @ViewChild('catFormContainer') catFormContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('categoryNameInput') categoryNameInput!: ElementRef<HTMLInputElement>;

  constructor(
    private adminService: AdminService,
    private toastSvc: ToastService
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  private scrollFormIntoView() {
    if (!this.catFormContainer) return;
    const top =
      this.catFormContainer.nativeElement.getBoundingClientRect().top +
      window.scrollY -
      120;
    window.scrollTo({ top, behavior: 'smooth' });
  }

  private focusName() {
    setTimeout(() => this.categoryNameInput?.nativeElement.focus(), 250);
  }

  public loadCategories() {
    this.adminService.getCategories().subscribe((data) => (this.categories = data));
  }

  /** Search support */
  get filteredCategories(): any[] {
    const q = this.searchTerm.trim().toLowerCase();
    if (!q) return this.categories;
    return this.categories.filter((c) => {
      const name = (c.categoryName || '').toLowerCase();
      const desc = (c.description || '').toLowerCase();
      const id = String(c.categoryID || '');
      return name.includes(q) || desc.includes(q) || id.includes(q);
    });
  }
  onSearchChanged() {
    this.page = 1;
  }

  /** One submit handler to show errors + keep your logic */
  submitCategory(ngf: NgForm) {
    this.catSubmitted = true;
    if (ngf.invalid) {
      this.scrollFormIntoView();
      return;
    }
    if (this.isEditMode) {
      this.updateCategory();
    } else {
      this.addCategory();
    }
  }

  public addCategory() {
    this.adminService.addCategory(this.form).subscribe((newCat: any) => {
      this.toastSvc.show(`Category added: “${newCat.categoryName}”`);
      this.resetForm();
      this.loadCategories();
    });
  }

  public editCategory(c: any) {
    this.isEditMode = true;
    this.editingCategoryID = c.categoryID;
    this.form = {
      categoryName: c.categoryName,
      description: c.description
    };
    this.scrollFormIntoView();
    this.focusName();
  }

  public updateCategory() {
    if (this.editingCategoryID == null) return;
    this.adminService
      .updateCategory(this.editingCategoryID, this.form)
      .subscribe(() => {
        this.toastSvc.show(`Category updated: “${this.form.categoryName}”`);
        this.resetForm();
        this.loadCategories();
        this.scrollFormIntoView();
        this.focusName();
      });
  }

  public promptDeleteCategory(c: any) {
    this.toDeleteCategory = c;
    this.confirmingDeleteCategory = true;
  }

  public onDeleteCategoryConfirmed() {
    if (!this.toDeleteCategory) return;
    const id = this.toDeleteCategory.categoryID;
    const name = this.toDeleteCategory.categoryName;
    this.adminService.deleteCategory(id).subscribe(() => {
      this.toastSvc.show(`Category deleted: “${name}”`);
      this.loadCategories();
    });
    this.toDeleteCategory = null;
    this.confirmingDeleteCategory = false;
  }

  public onDeleteCategoryCanceled() {
    this.toDeleteCategory = null;
    this.confirmingDeleteCategory = false;
  }

  public resetForm() {
    this.form = { categoryName: '', description: '' };
    this.isEditMode = false;
    this.editingCategoryID = null;
    this.catSubmitted = false;
    this.focusName();
  }

  /** trackBy for smooth list updates */
  trackByCategoryId = (_: number, item: any) => item.categoryID;
}
