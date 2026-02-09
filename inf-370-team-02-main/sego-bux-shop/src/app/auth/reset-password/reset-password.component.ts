import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../shared/toast.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss']
})
export class ResetPasswordComponent implements OnInit {
  resetForm!: FormGroup;
  resetError = '';
  isSubmitting = false;

  /** for resend */
  private emailFromLink: string | null = null;
  get canResend(): boolean {
    const msg = (this.resetError || '').toLowerCase();
    return this.emailFromLink !== null && (msg.includes('invalid') || msg.includes('expired'));
  }

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private auth: AuthService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.emailFromLink = this.route.snapshot.queryParamMap.get('email');

    this.resetForm = this.fb.group(
      {
        newPassword: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', Validators.required]
      },
      { validators: this.passwordsMatchValidator }
    );
  }

  passwordsMatchValidator(form: AbstractControl) {
    const pw = form.get('newPassword')?.value;
    const cpw = form.get('confirmPassword')?.value;
    return pw === cpw ? null : { mismatch: true };
  }

  onReset() {
    this.resetError = '';
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const { newPassword, confirmPassword } = this.resetForm.value;
    const email = this.route.snapshot.queryParamMap.get('email')!;
    const token = this.route.snapshot.queryParamMap.get('token')!;

    this.auth.resetPassword({ email, token, newPassword, confirmPassword })
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          this.toast.show('Password reset successful. Please sign in.');
          this.router.navigate(['/auth/login'], { queryParams: { reset: 'success' }});
        },
        error: (err: any) => {
          this.isSubmitting = false;
          this.resetError = err?.error?.message || 'Invalid or expired reset token.';
        }
      });
  }

  resendLink() {
    if (!this.emailFromLink) return;
    this.auth.forgotPassword(this.emailFromLink).subscribe({
      next: () => this.toast.show('A new reset link has been sent to your email.'),
      error: () => this.toast.show('Could not send a new link. Please try again.')
    });
  }

  get f() { return this.resetForm.controls; }
}
