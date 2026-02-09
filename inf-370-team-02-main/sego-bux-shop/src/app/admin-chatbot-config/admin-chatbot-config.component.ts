import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators
} from '@angular/forms';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { ToastService } from '../shared/toast.service';

type AdminConfigDto = {
  id: number;
  whatsAppNumber: string;
  supportEmail: string;

  originAddress?: string; // optional
  thresholdKm: number;
  flatShippingFee: number;
};

@Component({
  selector: 'app-admin-chatbot-config',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule],
  templateUrl: './admin-chatbot-config.component.html',
  styleUrls: ['./admin-chatbot-config.component.scss']
})
export class AdminChatbotConfigComponent implements OnInit {
  form!: FormGroup;
  submitted = false;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      id: [1],
      whatsAppNumber: ['', Validators.required],
      supportEmail: ['', [Validators.required, Validators.email]],
      originAddress: [''], // optional
      thresholdKm: [20, [Validators.required, Validators.min(0)]],
      flatShippingFee: [100, [Validators.required, Validators.min(0)]],
    });

    this.http.get<AdminConfigDto>(`${environment.apiUrl}/api/admin/chatbot-config`)
      .subscribe({
        next: data => this.form.patchValue(data),
        error: err => this.toast.show('❌ Failed to load config: ' + err.message)
      });
  }

  save(): void {
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.show('⚠️ Please fix errors before saving.');
      return;
    }

    this.http.put(`${environment.apiUrl}/api/admin/chatbot-config`, this.form.value)
      .subscribe({
        next: () => {
          this.toast.show(' Settings saved successfully.');
          this.submitted = false;
        },
        error: err =>
          this.toast.show('❌ Update failed: ' + (err.error?.message || err.message))
      });
  }
}
