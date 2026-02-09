import { Component, OnInit, AfterViewInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../shared/toast.service';

declare const google: any;

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit, AfterViewInit {
  registerForm!: FormGroup;
  registerError = '';
  justRegisteredEmail: string | null = null;

  /** Pre-navigation loader flag */
  isRouting = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      // ── Username: 3–20 chars, letters/numbers/underscores only
      username: [
        '',
        [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(20),
          Validators.pattern(/^[A-Za-z0-9_]+$/)
        ]
      ],

      // ── First/Last names: 2–40 chars, letters plus optional space/'/-
      name: [
        '',
        [
          Validators.required,
          Validators.minLength(2),
          Validators.maxLength(40),
          Validators.pattern(/^[A-Za-z]+(?:[ '\-][A-Za-z]+)*$/)
        ]
      ],
      surname: [
        '',
        [
          Validators.required,
          Validators.minLength(2),
          Validators.maxLength(40),
          Validators.pattern(/^[A-Za-z]+(?:[ '\-][A-Za-z]+)*$/)
        ]
      ],

      // ── Email: built-in validator
      email: ['', [Validators.required, Validators.email]],

      // ── Phone: digits only, exactly 10
      phone: [
        '',
        [
          Validators.required,
          Validators.pattern(/^\d*$/),
          Validators.minLength(10),
          Validators.maxLength(10)
        ]
      ],

      address: ['', Validators.required],
      password: ['', Validators.required]
    });
  }

  ngAfterViewInit(): void {
    if (typeof google !== 'undefined' && google.maps?.places) {
      const input = document.getElementById('autocomplete') as HTMLInputElement | null;
      if (input) {
        const ac = new google.maps.places.Autocomplete(input, { types: ['geocode'] });
        ac.addListener('place_changed', () => {
          const place = ac.getPlace();
          if (place?.formatted_address) {
            this.registerForm.patchValue({ address: place.formatted_address });
          }
        });
      }
    }
  }

  /** helper: show specific error only after touch */
  err(controlName: string, errorName: string): boolean {
    const c = this.registerForm.get(controlName);
    return !!(c && c.touched && c.hasError(errorName));
  }

  onSubmit(): void {
    this.registerError = '';
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.authService.register(this.registerForm.value).subscribe({
      next: _ => {
        this.justRegisteredEmail = this.registerForm.value.email;
        this.toast.show('Account created! Please verify your email.');
      },
      error: err => {
        this.registerError = err?.error?.message || err?.message || 'Registration failed.';
      }
    });
  }

  /** Navigate to Verify OTP with a pre-navigation overlay */
  onVerify(): void {
    if (!this.justRegisteredEmail) { return; }
    this.isRouting = true;

    // Allow the overlay to paint before route change
    setTimeout(() => {
      this.router.navigate(['/auth/verify-otp'], {
        queryParams: { email: this.justRegisteredEmail }
      });
      // No need to reset isRouting; component unmounts on navigation.
    }, 0);
  }
}
