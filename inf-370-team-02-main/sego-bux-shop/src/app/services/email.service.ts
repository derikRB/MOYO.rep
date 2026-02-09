import { Injectable } from '@angular/core';import emailjs, { EmailJSResponseStatus } from '@emailjs/browser';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EmailService {
  constructor() {}

  sendContactEmail(data: {
    from_name: string;
    from_email: string;
    phone: string;
    message: string;
  }): Promise<EmailJSResponseStatus> {  // ✅ FIXED return type
    const templateParams = {
      from_name: data.from_name,
      from_email: data.from_email,
      phone: data.phone || 'N/A',
      message: data.message
    };

    return emailjs.send(
      environment.emailJsServiceId,
      'template_a4v3v32',  // your contact form template ID
      templateParams,
      environment.emailJsUserId
    );
  }
}
