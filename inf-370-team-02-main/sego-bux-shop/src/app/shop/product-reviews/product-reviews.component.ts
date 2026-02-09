import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductReviewService } from '../../services/product-review.service';
import { ProductReviewDto } from '../../dto/product-review';
import { environment } from '../../../environments/environment';
import { FeedbackService } from '../../services/feedback.service';
import { FeedbackDto } from '../../dto/feedback.dto';

@Component({
  selector: 'app-product-reviews',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './product-reviews.component.html',
  styleUrls: ['./product-reviews.component.scss']
})
export class ProductReviewsComponent implements OnInit {
  @Input() productId!: number;

  reviews: ProductReviewDto[] = [];
  feedbacks: FeedbackDto[] = []; // NEW
  loading = true;

  constructor(
    private reviewSvc: ProductReviewService,
    private feedbackSvc: FeedbackService
  ) {}

  ngOnInit() {
    if (!this.productId) return;

    // Load reviews
    this.reviewSvc.getProductReviews(this.productId).subscribe({
      next: data => { this.reviews = data || []; this.loading = false; },
      error: () => this.loading = false
    });

    // Load generic feedback tied to orders that contained this product
    this.feedbackSvc.getForProduct(this.productId).subscribe({
      next: data => { this.feedbacks = data || []; },
      error: () => { this.feedbacks = []; }
    });
  }

  getFullPhotoUrl(path: string): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    return `${environment.apiUrl.replace(/\/$/, '')}${path.startsWith('/') ? '' : '/'}${path}`;
  }
}
