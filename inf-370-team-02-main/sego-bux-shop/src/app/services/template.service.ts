// src/app/services/template.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

export interface TemplateDto {
  templateID: number;
  name:       string;
  filePath:   string;
  productIDs?: number[];
}

@Injectable({ providedIn: 'root' })
export class TemplateService {
  // ← full absolute URL, not a relative path
  private base = `${environment.apiUrl}/api/Template`;

  constructor(private http: HttpClient) {}

  /** fetch all templates (no auth header) */
  getAll(): Observable<TemplateDto[]> {
    return this.http.get<TemplateDto[]>(this.base);
  }

  /** fetch templates assigned to a specific product (no auth header) */
  getByProduct(productID: number): Observable<TemplateDto[]> {
    return this.http.get<TemplateDto[]>(`${this.base}/ByProduct/${productID}`);
  }

  /** helper to build your Authorization header */
  private authHeaders(): HttpHeaders {
    const token = localStorage.getItem('token') || '';
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  /** create a new template */
  create(fd: FormData): Observable<TemplateDto> {
    return this.http.post<TemplateDto>(this.base, fd, {
      headers: this.authHeaders()
    });
  }

  /** update existing */
  update(id: number, fd: FormData): Observable<TemplateDto> {
    return this.http.put<TemplateDto>(`${this.base}/${id}`, fd, {
      headers: this.authHeaders()
    });
  }

  /** delete */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`, {
      headers: this.authHeaders()
    });
  }
}
