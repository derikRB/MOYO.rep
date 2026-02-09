// src/app/services/admin/faq/faq.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { FaqItem } from '../../../models/faq-item.model';

@Injectable({
  providedIn: 'root'
})
export class FaqService {
  // ← add /api here
  private base = `${environment.apiUrl}/api/admin/faq`;

  constructor(private http: HttpClient) { }

  getAll(): Observable<FaqItem[]> {
    return this.http.get<FaqItem[]>(this.base);
  }

  get(id: number): Observable<FaqItem> {
    return this.http.get<FaqItem>(`${this.base}/${id}`);
  }

  search(q: string): Observable<FaqItem[]> {
    return this.http.get<FaqItem[]>(`${this.base}/search?q=${encodeURIComponent(q)}`);
  }

  create(item: FaqItem): Observable<FaqItem> {
    return this.http.post<FaqItem>(this.base, item);
  }

  update(item: FaqItem): Observable<FaqItem> {
    return this.http.put<FaqItem>(`${this.base}/${item.faqId}`, item);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.base}/${id}`);
  }

  /** New: export to Rasa NLU */
  exportToNlu(): Observable<any> {
    return this.http.get(`${environment.apiUrl}/api/admin/faq/rasa/export-nlu`);
  }
}
