import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../services/product.service';
import { CartService } from '../services/cart.service';
import { CategoryService } from '../services/category.service';
import { ProductTypeService } from '../services/product-type.service';
import { PaginatePipe } from './../shared/pagination/pagination.pipe';
import { PaginationComponent } from './../shared/pagination/pagination.component';
import { Product } from '../models/product.model';
import { ProductType } from '../models/product-type.model';
import { Category } from './../models/category.model';
import { ToastService } from '../shared/toast.service';
import { StockSocketService } from '../services/StockSocketService'; 
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginatePipe, PaginationComponent, RouterModule],
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.scss']
})
export class ProductListComponent implements OnInit, OnDestroy {
  products: Product[] = [];
  filteredProducts: Product[] = [];
  productTypes: ProductType[] = [];
  categories: Category[] = [];
  hoveredProductID: number | null = null;

  searchQuery: string = '';
  selectedCategoryID: number | null = null;
  selectedTypeID: number | null = null;
  selectedPriceRange: string = '';

  page = 1;
  pageSize = 12;
  get totalPages() {
    return Math.ceil(this.filteredProducts.length / this.pageSize) || 1;
  }

  constructor(
    public productService: ProductService,
    private cartService: CartService,
    private categoryService: CategoryService,
    private productTypeService: ProductTypeService,
    private toastSvc: ToastService,
    private stockSocket: StockSocketService // <--- Correct type!
  ) {}

  ngOnInit() {
    this.productService.getProducts().subscribe(data => {
      this.products = data;
      this.applyFilters();
    });

    this.productTypeService.getProductTypes().subscribe(types => {
      this.productTypes = types;
    });

    this.categoryService.getCategories().subscribe({
      next: cats => {
        this.categories = cats;
      },
      error: err => console.error('Failed to load categories:', err)
    });

    // ---- SignalR connection for real-time stock updates ----
    this.stockSocket.connect();
    this.stockSocket.stockChanged$.subscribe((update: { productId: number; newStock: number } | null) => {
      if (update) {
        const prod = this.products.find(p => p.productID === update.productId);
        if (prod) prod.stockQuantity = update.newStock;
        const fprod = this.filteredProducts.find(p => p.productID === update.productId);
        if (fprod) fprod.stockQuantity = update.newStock;
      }
    });
  }

  ngOnDestroy() {
    this.stockSocket.disconnect();
  }

  getImageUrl(p: Product): string {
    if (p.primaryImageID) {
      const prim = p.productImages.find(pi => pi.imageID === p.primaryImageID);
      if (prim) return this.productService.imageFullUrl(prim);
    }
    return p.productImages.length
      ? this.productService.imageFullUrl(p.productImages[0])
      : 'assets/logo.png';
  }

  getSecondaryImageUrl(p: Product): string | null {
    if (p.secondaryImageID) {
      const sec = p.productImages.find(img => img.imageID === p.secondaryImageID);
      if (sec) return this.productService.imageFullUrl(sec);
    }
    return null;
  }

  addToCart(p: Product) {
    this.cartService.addToCart({
      id: p.productID,
      name: p.name,
      price: p.price,
      quantity: 1,
      imageUrl: this.getImageUrl(p)
    });
    this.toastSvc.show(`Added “${p.name}” to cart`, this.getImageUrl(p));
  }

  applyFilters() {
    this.filteredProducts = this.products.filter(prod => {
      const typeObj = this.productTypes.find(pt => pt.productTypeID === prod.productTypeID);
      const categoryMatch = !this.selectedCategoryID ||
        (typeObj && typeObj.categoryID === this.selectedCategoryID);
      const typeMatch = !this.selectedTypeID || prod.productTypeID === this.selectedTypeID;
      let min = 0, max = Infinity;
      if (this.selectedPriceRange) {
        const [minStr, maxStr] = this.selectedPriceRange.split('-');
        min = +minStr;
        max = +maxStr;
      }
      const priceMatch = prod.price >= min && prod.price <= max;
      const q = this.searchQuery.trim().toLowerCase();
      const searchMatch = !q ||
        prod.name.toLowerCase().includes(q) ||
        prod.description.toLowerCase().includes(q) ||
        (typeObj && typeObj.productTypeName.toLowerCase().includes(q));
      return categoryMatch && typeMatch && priceMatch && searchMatch;
    });
    this.page = 1;
  }
  isLowStock(p: Product): boolean {
    return p.lowStockThreshold > 0 && p.stockQuantity <= p.lowStockThreshold;
  }

  onTypeChange()        { this.applyFilters(); }
  onCategoryChange()    { this.applyFilters(); }
  onPriceChange()       { this.applyFilters(); }
  onSearchChange()      { this.applyFilters(); }
}
