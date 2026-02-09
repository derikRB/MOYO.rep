import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { ConfigService } from '../../services/config.service';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './verify-otp.component.html',
  styleUrls: ['./verify-otp.component.scss']
})
export class VerifyOtpComponent implements OnInit, OnDestroy {
  otpForm!: FormGroup;
  email = '';
  isSubmitting = false;
  isResending = false;
  errorMsg = '';
  infoMsg = '';

  otpSecondsLeft = 0;

  /** Arrival loader */
  isBootstrapping = true;
  private bootStart = 0;
  private readonly minLoaderMs = 800; // guarantee visibility

  private policySeconds = 600; // fallback; overridden by server policy
  private otpIntervalId: any;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private auth: AuthService,
    private config: ConfigService
  ) {}

  ngOnInit(): void {
    this.bootStart = performance.now();
    this.isBootstrapping = true;

    this.email = this.route.snapshot.queryParamMap.get('email') || '';
    this.otpForm = this.fb.group({
      otp: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
    });

    this.config.getOtpMinutes().subscribe({
      next: mins => {
        this.policySeconds = Math.max(60, (mins || 10) * 60);
        this.startOtpCountdown(this.policySeconds);
        this.finishBootstrap();
      },
      error: () => {
        this.startOtpCountdown(this.policySeconds);
        this.finishBootstrap();
      }
    });
  }

  ngOnDestroy(): void {
    clearInterval(this.otpIntervalId);
  }

  private finishBootstrap(): void {
    const elapsed = performance.now() - this.bootStart;
    const remaining = Math.max(0, this.minLoaderMs - elapsed);
    setTimeout(() => (this.isBootstrapping = false), remaining);
  }

  private startOtpCountdown(totalSeconds: number) {
    clearInterval(this.otpIntervalId);
    this.otpSecondsLeft = totalSeconds;
    this.otpIntervalId = setInterval(() => {
      this.otpSecondsLeft = Math.max(0, this.otpSecondsLeft - 1);
      if (this.otpSecondsLeft === 0) clearInterval(this.otpIntervalId);
    }, 1000);
  }

  get hh(): number { return Math.floor(this.otpSecondsLeft / 3600); }
  get mm(): number { return Math.floor((this.otpSecondsLeft % 3600) / 60); }
  get ss(): number { return Math.floor(this.otpSecondsLeft % 60); }

  onVerify() {
    this.errorMsg = '';
    if (this.otpForm.invalid) {
      this.otpForm.markAllAsTouched();
      return;
    }
    this.isSubmitting = true;
    this.auth.verifyOtp({ email: this.email, otp: this.otpForm.value.otp }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/auth/login'], { queryParams: { verified: 'true' } });
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMsg = err?.error?.message || 'Invalid or expired OTP. Try again.';
      }
    });
  }

  onResend() {
    if (this.otpSecondsLeft > 0 || this.isResending) return;

    this.isResending = true;
    this.infoMsg = '';
    this.errorMsg = '';

    this.auth.resendOtp({ email: this.email }).subscribe({
      next: () => {
        this.isResending = false;
        this.infoMsg = 'OTP resent. Check your inbox!';
        this.startOtpCountdown(this.policySeconds);
      },
      error: (err) => {
        this.isResending = false;
        this.errorMsg = err?.error?.message || 'Failed to resend OTP.';
      }
    });
  }
}
