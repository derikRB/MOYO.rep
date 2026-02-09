import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../services/product.service';
import { ProductReviewService } from '../../services/product-review.service';
import { Product } from '../../models/product.model';
import { ProductReviewDto } from '../../dto/product-review';
import { ProductReviewsComponent } from '../product-reviews/product-reviews.component';
import { CartService } from '../../services/cart.service';  
import { ToastService } from '../../shared/toast.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, ProductReviewsComponent],
  templateUrl: './product-detail.component.html',
  styleUrls: ['./product-detail.component.scss']
})
export class ProductDetailComponent implements OnInit {
  product!: Product | null;
  isLoading = true;
  reviews: ProductReviewDto[] = [];
  averageRating = 0;
  Math = Math;

  constructor(
    private route: ActivatedRoute,
    public productService: ProductService,
    private reviewService: ProductReviewService,
    private cartService: CartService,
    private toastService: ToastService 
  ) {}

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.productService.getProductById(id).subscribe({
      next: p => {
        this.product = p;
        this.isLoading = false;
        if (this.product) this.fetchReviews(this.product.productID);
      },
      error: () => {
        this.product = null;
        this.isLoading = false;
      }
    });
  }

  fetchReviews(productId: number) {
    this.reviewService.getProductReviews(productId).subscribe({
      next: (reviews) => {
        this.reviews = reviews;
        this.calculateAverageRating();
      }
    });
  }

  calculateAverageRating() {
    if (!this.reviews.length) {
      this.averageRating = 0;
      return;
    }
    const sum = this.reviews.reduce((acc, r) => acc + r.rating, 0);
    this.averageRating = sum / this.reviews.length;
  }

  addToCart() {
    if (!this.product) return;
    this.cartService.addToCart({
      id: this.product.productID,
      name: this.product.name,
      price: this.product.price,
      quantity: 1,
      imageUrl: this.product.productImages.length
        ? this.productService.imageFullUrl(this.product.productImages[0])
        : 'assets/logo.png'
    });
    this.toastService.show(`Added "${this.product.name}" to cart!`, this.product.productImages.length
        ? this.productService.imageFullUrl(this.product.productImages[0])
        : 'assets/logo.png');
  }
}
