import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { CartService, CartItem } from '../../services/cart.service';
import { CustomizationModalComponent } from '../../customization-modal.component';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterModule, CustomizationModalComponent],
  templateUrl: './cart.component.html',
  styleUrls: ['./cart.component.scss'],
})
export class CartComponent implements OnInit {
  cartItems: CartItem[] = [];
  
  // VAT %
  vatRate = 0;
  
  // Display amounts (derived)
  private grossIncl = 0; // sum of line prices as captured in cart
  private netExcl = 0;   // SubTotal Excl (from gross)
  private vatAmt = 0;    // VAT Amount (from gross)
  
  // Order limit validation
  orderLimitExceeded = false;
  orderLimit = 50; // 50 items limit
  totalQuantity = 0;
  
  // Customization modal
  customizingItem: CartItem | null = null;
  showCustomizationModal = false;
  
  // Recommendations
  recommendedProducts: Product[] = [];
  allProducts: Product[] = [];

  constructor(
    public cartService: CartService,
    private router: Router,
    private productService: ProductService
  ) {}

  ngOnInit(): void {
    // React to cart changes
    this.cartService.cart$.subscribe(items => {
      this.cartItems = items;
      this.recalcTotals();
      this.checkOrderLimit();
      this.updateRecommended();
    });
    
    // React to VAT rate changes
    this.cartService.vatRate$.subscribe(rate => {
      this.vatRate = rate ?? 0;
      this.recalcTotals();
    });
    
    // Load all products for recommendations
    this.productService.getProducts().subscribe(products => {
      this.allProducts = products;
      this.updateRecommended();
    });
    
    // Initial compute
    this.recalcTotals();
    this.checkOrderLimit();
  }

  /** Check if order exceeds the quantity limit */
  private checkOrderLimit(): void {
    this.totalQuantity = this.cartItems.reduce((total, item) => total + item.quantity, 0);
    this.orderLimitExceeded = this.totalQuantity > this.orderLimit;
  }

  /** Centralized totals calculator (VAT-inclusive pricing) */
  private recalcTotals(): void {
    // 1) Gross (incl. VAT) is how your prices are currently stored in the cart
    this.grossIncl = this.round2(
      this.cartItems.reduce((s, i) => s + i.price * i.quantity, 0)
    );
    
    // 2) Net & VAT derived from gross using the active VAT % (precisely like Checkout)
    if (this.vatRate > 0) {
      const divisor = 1 + (this.vatRate / 100);
      const net = this.grossIncl / divisor;
      this.netExcl = this.round2(net);
      this.vatAmt = this.round2(this.grossIncl - net);
    } else {
      // No VAT configured
      this.netExcl = this.grossIncl;
      this.vatAmt = 0;
    }
  }

  private round2(n: number): number {
    return Math.round((n + Number.EPSILON) * 100) / 100;
  }

  // --- Quantity / cart ops ---
  increaseQuantity(item: CartItem): void { 
    if (!this.orderLimitExceeded || this.totalQuantity < this.orderLimit) {
      this.cartService.increment(item); 
    }
  }
  
  decreaseQuantity(item: CartItem): void { this.cartService.decrement(item); }
  removeItem(item: CartItem): void { this.cartService.remove(item); }

  // --- Customization modal ---
  openCustomizationModal(item: CartItem): void {
    this.customizingItem = item;
    this.showCustomizationModal = true;
  }

  closeCustomizationModal(): void {
    this.customizingItem = null;
    this.showCustomizationModal = false;
  }

  onCustomizationSaved(customization: any): void {
    if (!this.customizingItem) return;
    this.cartService.updateCustomization(this.customizingItem, customization);
    this.closeCustomizationModal();
  }

  // --- Summary getters used by the template (names preserved; logic updated) ---
  getSubtotal(): number { return this.netExcl; } // SubTotal Excl
  getVAT(): number { return this.vatAmt; }       // VAT Amount
  getTotal(): number { return this.grossIncl; }  // Total (incl. VAT)

  // --- Formatting / navigation ---
  formatCurrency(amount: number): string { return 'R' + amount.toFixed(2); }
  
  goToCheckout(): void { 
    if (!this.orderLimitExceeded) {
      this.router.navigate(['/checkout']);
    }
  }

  // ------ For "You may also like" Carousel ------
  updateRecommended() {
    if (!this.allProducts || !this.cartItems) {
      this.recommendedProducts = [];
      return;
    }

    const cartIds = new Set(this.cartItems.map(i => i.id));
    this.recommendedProducts = this.allProducts
      .filter(p => !cartIds.has(p.productID))
      .map(p => ({
        ...p,
        imageUrl: this.getImageUrl(p)
      }));
  }

  getImageUrl(p: Product): string {
    if (p.primaryImageID) {
      const prim = p.productImages?.find(pi => pi.imageID === p.primaryImageID);
      if (prim) return this.productService.imageFullUrl(prim);
    }

    return p.productImages && p.productImages.length
      ? this.productService.imageFullUrl(p.productImages[0])
      : 'assets/logo.png';
  }

  addToCart(p: Product) {
    if (!this.orderLimitExceeded) {
      this.cartService.addToCart({
        id: p.productID,
        name: p.name,
        price: p.price,
        quantity: 1,
        imageUrl: this.getImageUrl(p)
      });
      
      // brief delay for cart$ to emit before recalculating recs
      setTimeout(() => this.updateRecommended(), 150);
    }
  }
}