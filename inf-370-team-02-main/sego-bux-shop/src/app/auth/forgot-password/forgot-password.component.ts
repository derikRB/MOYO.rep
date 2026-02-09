import { Component } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  AbstractControl
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { ToastService } from '../../shared/toast.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  forgotForm: FormGroup;
  loading = false;
  serverError = '';

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private toast: ToastService
  ) {
    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  get email(): AbstractControl {
    return this.forgotForm.get('email')!;
  }

  err(name: string): boolean {
    const c = this.email;
    return !!(c.touched && c.hasError(name));
  }

  onSubmit(): void {
    this.serverError = '';
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    const email = String(this.email.value).trim();

    this.auth.forgotPassword(email).subscribe({
      next: () => {
        this.loading = false;
        // user-enumeration safe message
        this.toast.show('If that email exists, we’ve sent a reset link. Please check your inbox.');
        // Stay on page or navigate back to login—your call
        // this.router.navigate(['/auth/login']);
      },
      error: (err) => {
        this.loading = false;
        this.serverError = err?.error?.message || 'Something went wrong. Try again.';
      }
    });
  }
}
