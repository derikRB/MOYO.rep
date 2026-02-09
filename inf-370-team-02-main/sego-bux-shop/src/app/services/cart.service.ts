import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { VatService } from './vat.service';

export interface CartItem {
  id: number;
  name: string;
  price: number;
  quantity: number;
  imageUrl?: string;
  customization?: {
    template: string;
    customText?: string;
    font: string;
    fontSize: number;
    color: string;
    uploadedImagePath?: string;
    snapshot?: string;
    uploadedImageFile?: File;
    snapshotFile?: File;
  };
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private cart: CartItem[] = [];
  private cartSubject = new BehaviorSubject<CartItem[]>([]);
  public cart$ = this.cartSubject.asObservable();

  constructor(
    private authService: AuthService,
    private vatService: VatService
  ) {
    this.loadCart();
    this.authService.isLoggedIn$.subscribe(() => this.loadCart());
  }

  public get vatRate$(): Observable<number> {
    return this.vatService.vatRate$;
  }

  private getStorageKey(): string {
    const uid = this.authService.getUserId();
    return uid ? `cart_${uid}` : 'cart_guest';
  }

  private loadCart() {
    try {
      const raw = localStorage.getItem(this.getStorageKey());
      this.cart = raw ? JSON.parse(raw) : [];
      
      // Remove large files from customization to prevent quota issues
      this.cart.forEach(item => {
        if (item.customization) {
          delete item.customization.uploadedImageFile;
          delete item.customization.snapshotFile;
        }
      });
      
      this.cartSubject.next([...this.cart]);
    } catch (error) {
      console.error('Error loading cart from localStorage:', error);
      this.cart = [];
      this.cartSubject.next([]);
    }
  }

  private saveCart() {
    try {
      // Create a copy without large files for storage
      const cartForStorage = this.cart.map(item => ({
        ...item,
        customization: item.customization ? {
          ...item.customization,
          uploadedImageFile: undefined,
          snapshotFile: undefined
        } : undefined
      }));
      
      localStorage.setItem(this.getStorageKey(), JSON.stringify(cartForStorage));
      this.cartSubject.next([...this.cart]);
    } catch (error) {
      console.error('Error saving cart to localStorage:', error);
      // If we can't save to localStorage, at least keep the cart in memory
      this.cartSubject.next([...this.cart]);
    }
  }

  getItems(): CartItem[] {
    return [...this.cart];
  }

  addToCart(item: CartItem) {
    const existing = this.cart.find(p =>
      p.id === item.id &&
      JSON.stringify(p.customization) === JSON.stringify(item.customization)
    );
    if (existing) {
      existing.quantity += item.quantity;
    } else {
      this.cart.push({ ...item });
    }
    this.saveCart();
  }

  increment(item: CartItem) { this._increaseQuantity(item); }
  decrement(item: CartItem) { this._decreaseQuantity(item); }
  remove(item: CartItem)    { this._removeItem(item); }

  private _removeItem(item: CartItem) {
    this.cart = this.cart.filter(p => p !== item);
    this.saveCart();
  }

  private _increaseQuantity(item: CartItem) {
    const found = this.cart.find(p => p === item);
    if (found) {
      found.quantity++;
      this.saveCart();
    }
  }

  private _decreaseQuantity(item: CartItem) {
    const found = this.cart.find(p => p === item);
    if (!found) return;
    if (found.quantity > 1) {
      found.quantity--;
    } else {
      this.cart = this.cart.filter(p => p !== found);
    }
    this.saveCart();
  }

  clearCart() {
    this.cart = [];
    this.saveCart();
  }

  /** === Legacy methods kept intact (no breaking changes) === */
  getSubtotal(): number {
    // Historically used across the app. Represents cart sum of line prices.
    return this.cart.reduce((s, i) => s + i.price * i.quantity, 0);
  }
  getVATAmount(): number {
    return this.getSubtotal() * (this.vatService.vatRateValue / 100);
  }
  getTotalWithVAT(): number {
    return this.getSubtotal() + this.getVATAmount();
  }

  updateCustomization(item: CartItem, customization: CartItem['customization']) {
    const idx = this.cart.findIndex(p => p === item);
    if (idx > -1) {
      this.cart[idx].customization = customization;
      this.saveCart();
    }
  }

  /** === New helpers for VAT-inclusive pricing (used by CheckoutComponent) === */
  getGrossSubtotal(): number {
    // Gross = as stored in cart (your product prices)
    return this.getSubtotal();
  }
  getNetFromGross(vatRate: number): number {
    const gross = this.getGrossSubtotal();
    if (!vatRate) return gross;
    return Math.round((gross / (1 + (vatRate / 100))) * 100) / 100;
  }
  getVatFromGross(vatRate: number): number {
    const gross = this.getGrossSubtotal();
    if (!vatRate) return 0;
    const net = gross / (1 + (vatRate / 100));
    return Math.round((gross - net) * 100) / 100;
  }
}