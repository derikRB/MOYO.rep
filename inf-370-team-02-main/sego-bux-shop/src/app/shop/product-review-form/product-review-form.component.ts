import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ProductReviewService } from '../../services/product-review.service';
import { CreateProductReviewDto } from '../../dto/product-review';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../shared/toast.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';

@Component({
  selector: 'app-product-review-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, ConfirmDialogComponent],
  templateUrl: './product-review-form.component.html',
  styleUrls: ['./product-review-form.component.scss']
})
export class ProductReviewFormComponent implements OnInit {
  productId!: number;
  orderId!: number;
  form!: FormGroup;
  submitting = false;
  alreadyReviewed = false;

  // For confirm dialog
  confirming = false;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private reviewSvc: ProductReviewService,
    private toastSvc: ToastService
  ) {}

  ngOnInit() {
    this.orderId = Number(this.route.snapshot.paramMap.get('orderId'));
    this.productId = Number(this.route.snapshot.queryParamMap.get('productId'));
    this.form = this.fb.group({
      rating: [null, [Validators.required, Validators.min(1), Validators.max(5)]],
      reviewTitle: [''],
      reviewText: ['', Validators.required],
      photo: [null]
    });

    // Check for duplicate review (assume hasReviewed returns Observable<boolean>)
    this.reviewSvc.hasReviewed(this.orderId, this.productId).subscribe({
      next: (exists) => {
        if (exists) {
          this.alreadyReviewed = true;
          this.form.disable();
          this.toastSvc.show('You have already reviewed this product for this order.', 'error');
        }
      }
    });
  }

  onFileChange(event: Event) {
    const file = (event.target as HTMLInputElement)?.files?.[0];
    if (file) this.form.patchValue({ photo: file });
  }

  openConfirm() { this.confirming = true; }
  closeConfirm() { this.confirming = false; }

  confirmSubmit() {
    this.confirming = false;
    this.onSubmit(true);
  }

  onSubmit(confirmed = false) {
    if (this.form.invalid || this.submitting) return;
    if (!confirmed) {
      this.openConfirm();
      return;
    }
    this.submitting = true;
    const dto: CreateProductReviewDto = {
      productID: this.productId,
      orderID: this.orderId,
      rating: this.form.value.rating,
      reviewTitle: this.form.value.reviewTitle,
      reviewText: this.form.value.reviewText,
      photo: this.form.value.photo
    };
    this.reviewSvc.submitReview(dto).subscribe({
      next: () => {
        this.toastSvc.show('Thank you for your review!');
        this.router.navigate(['/profile/orders']);
      },
      error: err => {
        if (err.status === 400 && /already.*reviewed/i.test(err.error)) {
          this.toastSvc.show('You have already reviewed this product for this order.', 'error');
          this.alreadyReviewed = true;
          this.form.disable();
        } else {
          this.toastSvc.show('Submission failed. Please try again.', 'error');
        }
        this.submitting = false;
      }
    });
  }
}
