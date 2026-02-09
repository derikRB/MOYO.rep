import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { AdminService } from '../services/admin.service';
import { Product } from '../models/product.model';
import { ProductImage } from '../models/product-image.model';
import { PaginatePipe } from '../shared/pagination/pagination.pipe';
import { PaginationComponent } from '../shared/pagination/pagination.component';
import { ToastService } from '../shared/toast.service';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-manage-products',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PaginatePipe,
    PaginationComponent,
    ConfirmDialogComponent
  ],
  templateUrl: './manage-products.component.html',
  styleUrls: ['./manage-products.component.scss']
})
export class ManageProductsComponent implements OnInit {
  products: Product[] = [];
  productTypes: any[] = [];
  categories: any[] = [];

  filteredProductTypes: any[] = [];

  form: any = {
    name: '', description: '',
    price: 0,
    stockQuantity: 0,
    categoryID: null,
    productTypeID: null,
    primaryImageID: null,
    secondaryImageID: null
  };

  selectedFiles?: FileList;
  currentImages: ProductImage[] = [];
  selectedPrimaryImageId: number | null = null;
  selectedSecondaryImageId: number | null = null;

  isEditMode = false;
  editingProductId: number | null = null;

  confirmingDelete = false;
  toDelete?: Product;

  page = 1;
  pageSize = 7;

  /** search (styled like your categories filter) */
  searchTerm = '';

  get totalPages() {
    return Math.ceil(this.filteredProducts.length / this.pageSize) || 1;
  }

  // === UX helpers for scroll + validation ===
  prodSubmitted = false;
  @ViewChild('formTop') formTop!: ElementRef<HTMLElement>;
  @ViewChild('nameInput') nameInput!: ElementRef<HTMLInputElement>;

  constructor(
    private adminService: AdminService,
    private toastSvc: ToastService
  ) {}

  ngOnInit(): void {
    this.loadProducts();
    this.loadProductTypes();
    this.loadCategories();
  }

  private scrollFormIntoView() {
    if (!this.formTop) return;
    const top =
      this.formTop.nativeElement.getBoundingClientRect().top +
      window.scrollY - 120;
    window.scrollTo({ top, behavior: 'smooth' });
  }
  private focusName() {
    setTimeout(() => this.nameInput?.nativeElement.focus(), 200);
  }

  // ---- loads ----
  loadProducts() {
    this.adminService.getProducts()
      .subscribe(data => this.products = data);
  }
  loadProductTypes() {
    this.adminService.getProductTypes()
      .subscribe(types => this.productTypes = types);
  }
  loadCategories() {
    this.adminService.getCategories()
      .subscribe(data => this.categories = data);
  }

  // ---- CASCADING LOGIC ----
  onCategoryChange() {
    this.filteredProductTypes = this.productTypes.filter((pt: any) => pt.categoryID === this.form.categoryID);
    if (!this.filteredProductTypes.some((pt: any) => pt.productTypeID === this.form.productTypeID)) {
      this.form.productTypeID = null;
    }
  }

  onFileChange(e: Event) {
    const inp = (e.target as HTMLInputElement);
    this.selectedFiles = inp.files ?? undefined;
  }

  // unified submit with validation + scroll
  onSubmit(formRef: NgForm) {
    this.prodSubmitted = true;
    if (formRef.invalid) {
      this.scrollFormIntoView();
      return;
    }
    if (this.isEditMode) this.updateProduct();
    else this.addProduct();
  }

  addProduct() {
    this.adminService.addProduct(this.form)
      .subscribe(newProd => {
        if (this.selectedFiles) {
          this.adminService
            .uploadProductImages(newProd.productID, this.selectedFiles)
            .subscribe(() => this.selectedFiles = undefined);
        }
        this.resetForm();
        this.loadProducts();
        this.toastSvc.show(`Added product: "${newProd.name}"`);
      });
  }

  editProduct(p: Product) {
    this.isEditMode = true;
    this.editingProductId = p.productID;
    this.form = {
      name: p.name,
      description: p.description,
      price: p.price,
      stockQuantity: p.stockQuantity,
      categoryID: null, // set below
      productTypeID: p.productTypeID,
      primaryImageID: p.primaryImageID ?? null,
      secondaryImageID: p.secondaryImageID ?? null
    };

    const prodType = this.productTypes.find((pt: any) => pt.productTypeID === p.productTypeID);
    if (prodType) {
      this.form.categoryID = prodType.categoryID;
      this.filteredProductTypes = this.productTypes.filter((pt: any) => pt.categoryID === prodType.categoryID);
    }

    this.currentImages = p.productImages;
    this.selectedPrimaryImageId = p.primaryImageID ?? null;
    this.selectedSecondaryImageId = p.secondaryImageID ?? null;

    // UX: scroll + focus
    this.scrollFormIntoView();
    this.focusName();
  }

  updateProduct() {
    if (!this.editingProductId) return;
    const dto = {
      ...this.form,
      primaryImageID: this.selectedPrimaryImageId,
      secondaryImageID: this.selectedSecondaryImageId
    };
    this.adminService.updateProduct(this.editingProductId, dto)
      .subscribe(() => {
        if (this.selectedFiles) {
          this.adminService
            .uploadProductImages(this.editingProductId!, this.selectedFiles)
            .subscribe(() => this.selectedFiles = undefined);
        }
        this.resetForm();
        this.loadProducts();
        this.toastSvc.show(`Updated product: "${dto.name}"`);
        // UX: keep the form in view after update
        this.scrollFormIntoView();
        this.focusName();
      });
  }

  promptDelete(p: Product) { this.toDelete = p; this.confirmingDelete = true; }

  onDeleteConfirmed() {
    if (!this.toDelete) return;
    const name = this.toDelete.name;
    this.adminService.deleteProduct(this.toDelete.productID)
      .subscribe(() => {
        this.loadProducts();
        this.toastSvc.show(`Deleted product: "${name}"`);
      });
    this.confirmingDelete = false;
    this.toDelete = undefined;
  }
  onDeleteCanceled() { this.confirmingDelete = false; this.toDelete = undefined; }

  deleteImage(img: ProductImage) {
    this.adminService.deleteProductImage(img.imageID)
      .subscribe(() => {
        this.currentImages = this.currentImages.filter(i => i.imageID !== img.imageID);
        if (this.selectedPrimaryImageId === img.imageID)   this.selectedPrimaryImageId   = null;
        if (this.selectedSecondaryImageId === img.imageID) this.selectedSecondaryImageId = null;
      });
  }

  resetForm() {
    this.form = {
      name:'', description:'', price:0, stockQuantity:0,
      categoryID:null, productTypeID:null, primaryImageID:null, secondaryImageID:null
    };
    this.filteredProductTypes = [];
    this.selectedFiles = undefined;
    this.currentImages = [];
    this.selectedPrimaryImageId = null;
    this.selectedSecondaryImageId = null;
    this.isEditMode = false;
    this.editingProductId = null;
    this.prodSubmitted = false;
    this.focusName();
  }

  getProductTypeName(id: number): string {
    const pt = this.productTypes.find((x: any) => x.productTypeID === id);
    return pt ? pt.productTypeName : '';
  }

  /** ---------- SEARCH (used by table + pagination) ---------- */
  get filteredProducts(): Product[] {
    const q = this.searchTerm.trim().toLowerCase();
    if (!q) return this.products;

    return this.products.filter(p => {
      const name = (p.name || '').toLowerCase();
      const desc = (p.description || '').toLowerCase();
      const typeName = (this.getProductTypeName(p.productTypeID) || '').toLowerCase();

      let catName = '';
      const pt = this.productTypes.find((x: any) => x.productTypeID === p.productTypeID);
      if (pt) {
        const cat = this.categories.find((c: any) => c.categoryID === pt.categoryID);
        catName = (cat?.categoryName || '').toLowerCase();
      }

      return (
        name.includes(q) ||
        desc.includes(q) ||
        typeName.includes(q) ||
        catName.includes(q)
      );
    });
  }

  clearSearch() { if (this.searchTerm) { this.searchTerm = ''; this.page = 1; } }

  // ---- IMPORT (Excel/CSV) ----
  onImportExcel(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];
    const reader = new FileReader();
    reader.onload = (e: any) => {
      const data = new Uint8Array(e.target.result);
      const workbook = XLSX.read(data, { type: 'array' });
      const sheet = workbook.Sheets[workbook.SheetNames[0]];
      const json: any[] = XLSX.utils.sheet_to_json(sheet, { defval: '' });

      const products = json.map(row => ({
        name: row['Name'] ?? '',
        description: row['Description'] ?? '',
        price: +row['Price'] || 0,
        stockQuantity: +row['StockQuantity'] || 0,
        productTypeID: +row['ProductTypeID'] || 0,
        primaryImageID: row['PrimaryImageID'] ? +row['PrimaryImageID'] : null,
        secondaryImageID: row['SecondaryImageID'] ? +row['SecondaryImageID'] : null,
        lowStockThreshold: row['LowStockThreshold'] ? +row['LowStockThreshold'] : 10
      }));

      this.adminService.bulkImportProducts(products).subscribe({
        next: () => {
          this.toastSvc.show('Import completed');
          this.loadProducts();
        },
        error: (err) => this.toastSvc.show('Import failed: ' + err.message)
      });
    };
    reader.readAsArrayBuffer(file);
  }
}
