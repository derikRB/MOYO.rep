import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ChatbotConfigService {
  private url = `${environment.apiUrl}/api/admin/chatbot-config`;

  constructor(private http: HttpClient) {}

  getConfig() {
    return this.http.get<any>(this.url);
  }
}
