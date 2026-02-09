import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { AdminService } from '../services/admin.service';
import { PaginatePipe } from '../shared/pagination/pagination.pipe';
import { PaginationComponent } from '../shared/pagination/pagination.component';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';
import { ToastService } from '../shared/toast.service';

@Component({
  selector: 'app-manage-product-types',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PaginatePipe,
    PaginationComponent,
    ConfirmDialogComponent
  ],
  templateUrl: './manage-product-types.component.html',
  styleUrls: ['./manage-product-types.component.scss']
})
export class ManageProductTypesComponent implements OnInit {
  public productTypes: any[] = [];
  public categories:   any[] = [];

  // search
  public searchTerm = '';

  public form = {
    productTypeName: '',
    description:     '',
    categoryID:      null as number | null
  };
  public isEditMode               = false;
  public editingProductTypeID: number | null = null;

  // confirm dialog
  public confirmingDeleteType = false;
  public toDeleteType: any | null = null;

  // pagination
  public page     = 1;
  public pageSize = 7;
  public get totalPages() {
    return Math.ceil(this.filteredProductTypes.length / this.pageSize) || 1;
  }

  // UI helpers
  ptSubmitted = false;

  @ViewChild('ptFormContainer') ptFormContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('ptNameInput')     ptNameInput!:     ElementRef<HTMLInputElement>;

  constructor(
    private adminService: AdminService,
    private toastSvc:     ToastService
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    this.loadProductTypes();
  }

  private scrollFormIntoView() {
    if (!this.ptFormContainer) return;
    const top = this.ptFormContainer.nativeElement.getBoundingClientRect().top + window.scrollY - 120;
    window.scrollTo({ top, behavior: 'smooth' });
  }

  private focusName() {
    setTimeout(() => this.ptNameInput?.nativeElement.focus(), 250);
  }

  public loadCategories() {
    this.adminService.getCategories()
      .subscribe(data => this.categories = data);
  }

  public loadProductTypes() {
    this.adminService.getProductTypes()
      .subscribe(data => this.productTypes = data);
  }

  /** Filtered view used by table + pagination */
  get filteredProductTypes(): any[] {
    const q = this.searchTerm.trim().toLowerCase();
    if (!q) return this.productTypes;

    return this.productTypes.filter(pt => {
      const name = (pt.productTypeName || '').toLowerCase();
      const desc = (pt.description      || '').toLowerCase();
      const cat  = (this.getCategoryName(pt.categoryID) || '').toLowerCase();
      const id   = String(pt.productTypeID || '');
      return (
        name.includes(q) ||
        desc.includes(q) ||
        cat.includes(q)  ||
        id.includes(q)
      );
    });
  }

  /** Called on search change to reset paging */
  onSearchChanged() { this.page = 1; }

  submitProductType(ngf: NgForm) {
    this.ptSubmitted = true;
    if (ngf.invalid) {
      this.scrollFormIntoView();
      return;
    }
    if (this.isEditMode) {
      this.updateProductType();
    } else {
      this.addProductType();
    }
  }

  public addProductType() {
    this.adminService.addProductType(this.form)
      .subscribe((newType: any) => {
        this.toastSvc.show(`Product type added: “${newType.productTypeName}”`);
        this.resetForm();
        this.loadProductTypes();
      });
  }

  public editProductType(pt: any) {
    this.isEditMode               = true;
    this.editingProductTypeID     = pt.productTypeID;
    this.form = {
      productTypeName: pt.productTypeName,
      description:     pt.description,
      categoryID:      pt.categoryID
    };
    this.scrollFormIntoView();
    this.focusName();
  }

  public updateProductType() {
    if (!this.editingProductTypeID) return;
    this.adminService.updateProductType(this.editingProductTypeID, this.form)
      .subscribe(() => {
        this.toastSvc.show(`Product type updated: “${this.form.productTypeName}”`);
        this.resetForm();
        this.loadProductTypes();
        this.scrollFormIntoView();
        this.focusName();
      });
  }

  public promptDeleteProductType(pt: any) {
    this.toDeleteType         = pt;
    this.confirmingDeleteType = true;
  }

  public onDeleteTypeConfirmed() {
    if (!this.toDeleteType) return;
    const id   = this.toDeleteType.productTypeID;
    const name = this.toDeleteType.productTypeName;
    this.adminService.deleteProductType(id)
      .subscribe(() => {
        this.toastSvc.show(`Product type deleted: “${name}”`);
        this.loadProductTypes();
      });
    this.toDeleteType         = null;
    this.confirmingDeleteType = false;
  }

  public onDeleteTypeCanceled() {
    this.toDeleteType         = null;
    this.confirmingDeleteType = false;
  }

  public getCategoryName(id: number): string {
    const cat = this.categories.find(c => c.categoryID === id);
    return cat ? cat.categoryName : '-';
  }

  public resetForm() {
    this.form = {
      productTypeName: '',
      description:     '',
      categoryID:      null
    };
    this.isEditMode           = false;
    this.editingProductTypeID = null;
    this.ptSubmitted          = false;
    this.focusName();
  }

  /** trackBy for smoother list updates */
  trackByTypeId = (_: number, item: any) => item.productTypeID;
}
