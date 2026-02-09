import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { EmailService } from '../services/email.service';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './contact.component.html',
  styleUrls: ['./contact.component.scss']
})
export class ContactComponent implements OnInit {
  contactForm!: FormGroup;
  isSending = false;
  messageSent = false;
  errorMessage = '';

  constructor(private fb: FormBuilder, private emailService: EmailService) {}

  ngOnInit(): void {
    this.contactForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
      comment: ['', Validators.required]
    });
  }

  onSubmit(): void {
    if (this.contactForm.invalid) {
      this.contactForm.markAllAsTouched();
      return;
    }

    this.isSending = true;
    this.messageSent = false;
    this.errorMessage = '';

    const formData = {
      from_name: this.contactForm.value.name,
      from_email: this.contactForm.value.email,
      phone: this.contactForm.value.phone,
      message: this.contactForm.value.comment
    };

    this.emailService.sendContactEmail(formData)
      .then(() => {
        this.isSending = false;
        this.messageSent = true;
        this.contactForm.reset();
      })
      .catch((error) => {
        this.isSending = false;
        this.errorMessage = 'Failed to send message. Please try again later.';
        console.error('EmailJS error:', error);
      });
  }
}
