import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
} from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { startWith } from 'rxjs/operators';

import { AuthService } from '../../services/auth.service';
import { SessionTimerService } from '../../services/session-timer.service';
import { ToastService } from '../../shared/toast.service';

type Mode = 'email' | 'phone';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  loginError = '';
  isLoading = false;
  mode: Mode = 'email';

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private sessionTimer: SessionTimerService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      identifier: ['', Validators.required], // validators set dynamically
      password: ['', Validators.required],
    });

    this.identifier.valueChanges
      .pipe(startWith(this.identifier.value ?? ''))
      .subscribe((val: string) => this.applyIdentifierMode(String(val ?? '')));
  }

  get identifier(): AbstractControl {
    return this.loginForm.get('identifier')!;
  }

  private applyIdentifierMode(raw: string) {
    const v = (raw || '').trim();

    // Treat as "phone UI" for digits-only (any length) to show numeric keypad and messages,
    // but enforce exact 10 digits feedback via min/max below.
    const looksLikePhoneUI = /^\d+$/.test(v) || /^\+?27\d*$/.test(v);
    const nextMode: Mode = looksLikePhoneUI ? 'phone' : 'email';
    if (nextMode !== this.mode) this.mode = nextMode;

    if (looksLikePhoneUI) {
      this.identifier.setValidators([
        Validators.required,
        Validators.pattern(/^\d+$/), // user message: digits only
        Validators.minLength(10),
        Validators.maxLength(13), // allow +27 pattern typed without '+'
      ]);
    } else {
      this.identifier.setValidators([Validators.required, Validators.email]);
    }
    this.identifier.updateValueAndValidity({ emitEvent: false });
  }

  err(ctrl: string, name: string): boolean {
    const c = this.loginForm.get(ctrl);
    return !!(c && c.touched && c.hasError(name));
  }

  onLogin(): void {
    this.loginError = '';
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const creds = this.loginForm.value;

    this.auth.loginAuto({
      emailOrUsername: creds.identifier,
      password: creds.password,
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.sessionTimer.start();
        this.toast.show('Welcome back! You are now signed in.');
        const role = this.auth.getRoleFromToken();
        this.router.navigate([role === 'Customer' ? '/products' : '/admin']);
      },
      error: err => {
        this.isLoading = false;
        const msg = (err?.error?.message || err?.error || err?.message || '').toLowerCase();

        if (msg.includes('verify your email')) {
          this.router.navigate(['/auth/verify-otp'], { queryParams: { email: creds.identifier } });
          this.toast.show('Please verify your email to continue.');
          return;
        }

        this.loginError =
          (err?.error?.message || err?.error || err?.message) ??
          'Invalid credentials';
      }
    });
  }
}
