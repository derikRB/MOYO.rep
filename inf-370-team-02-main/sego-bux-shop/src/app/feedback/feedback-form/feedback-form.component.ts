import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FeedbackService } from '../../services/feedback.service';
import { CreateFeedbackDto, FeedbackDto } from '../../dto/feedback.dto';
import { ToastService } from '../../shared/toast.service';

@Component({
  selector: 'app-feedback-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './feedback-form.component.html',
  styleUrls: ['./feedback-form.component.scss']
})
export class FeedbackFormComponent implements OnInit {
  form!: FormGroup;
  orderId!: number;
  alreadyGiven = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private feedbackSvc: FeedbackService,
    private toast: ToastService
  ) {}

  ngOnInit() {
    this.orderId = +this.route.snapshot.paramMap.get('orderId')!;
    this.form = this.fb.group({
      orderNumber: [{ value: this.orderId, disabled: true }, Validators.required],
      rating: [null, Validators.required],
      comments: ['', Validators.required],
      recommend: [false]
    });

    // check if already submitted
    this.feedbackSvc.getMine().subscribe((all: FeedbackDto[]) => {
      if (all.some(fb => fb.orderID === this.orderId)) {
        this.alreadyGiven = true;
        this.form.disable();
      }
    });
  }

  onSubmit() {
    if (this.form.invalid) {
      this.toast.show('Please complete all required fields.');
      return;
    }
    const payload: CreateFeedbackDto = {
      orderID: this.orderId,
      rating: this.form.value.rating,
      comments: this.form.value.comments,
      recommend: this.form.value.recommend
    };
    this.feedbackSvc.submitFeedback(payload).subscribe({
      next: () => {
        this.toast.show('Thank you for your feedback!');
        this.router.navigate(['/profile/orders']);
      },
      error: err => {
        // assume backend returns 400 with “already exists” message
        if (err.status === 400 && typeof err.error === 'string' &&
            err.error.toLowerCase().includes('already exists')) {
          this.toast.show('You have already submitted feedback for this order.');
          this.alreadyGiven = true;
          this.form.disable();
        } else {
          this.toast.show('Submission failed. Please try again.');
        }
      }
    });
  }

  onCancel() {
    this.router.navigate(['/profile/orders']);
  }
}
