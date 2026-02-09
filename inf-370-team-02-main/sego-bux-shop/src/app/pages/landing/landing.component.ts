import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent implements OnInit, OnDestroy {
  products: Product[] = [];
  carouselProducts: Product[] = [];
  carouselIndex = 0;
  visibleCount = 4; // Cards visible at a time
  autoScrollInterval: any;

  constructor(
    private productService: ProductService,
    private router: Router
  ) {}

  ngOnInit() {
    this.productService.getProducts().subscribe(products => {
      this.products = products;
      // Only show featured products if available, else first 10 products
      const featured = products.filter(p => (p as any).isFeatured); // Adjust if your field is different
      this.carouselProducts = featured.length > 0 ? featured : products.slice(0, 10);
      this.carouselIndex = 0; // reset
    });

    // Auto-scroll every 2.5s
    this.autoScrollInterval = setInterval(() => this.nextSlide(), 2500);
  }

  ngOnDestroy() {
    if (this.autoScrollInterval) clearInterval(this.autoScrollInterval);
  }

  nextSlide() {
    if (!this.carouselProducts.length) return;
    this.carouselIndex = (this.carouselIndex + 1) % (this.carouselProducts.length - this.visibleCount + 1 || 1);
  }

  prevSlide() {
    if (!this.carouselProducts.length) return;
    this.carouselIndex =
      (this.carouselIndex - 1 + (this.carouselProducts.length - this.visibleCount + 1)) %
      (this.carouselProducts.length - this.visibleCount + 1 || 1);
  }

  get visibleProducts() {
    // Infinite loop-like feel: when reaching end, wrap around
    if (this.carouselProducts.length <= this.visibleCount) {
      return this.carouselProducts;
    }
    let result = this.carouselProducts.slice(this.carouselIndex, this.carouselIndex + this.visibleCount);
    // If at end, loop start
    if (result.length < this.visibleCount) {
      result = result.concat(this.carouselProducts.slice(0, this.visibleCount - result.length));
    }
    return result;
  }

  getImageUrl(p: Product): string {
    // Use your cart's getImageUrl logic!
    if (p.primaryImageID) {
      const prim = p.productImages?.find(pi => pi.imageID === p.primaryImageID);
      if (prim) return this.productService.imageFullUrl(prim);
    }
    return p.productImages && p.productImages.length
      ? this.productService.imageFullUrl(p.productImages[0])
      : 'assets/logo.png';
  }

  goToProduct(productID: number) {
    this.router.navigate(['/products', productID]);
  }
}
