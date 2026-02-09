import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  private base = `${environment.apiUrl}/api/maintenance`;

  constructor(private http: HttpClient) {}

  backup(): Observable<Blob> {
    return this.http.post(`${this.base}/backup`, null, { responseType: 'blob' as 'json' }) as unknown as Observable<Blob>;
  }

  restore(file: File): Observable<HttpEvent<any>> {
    const form = new FormData();
    form.append('file', file);
    const req = new HttpRequest('POST', `${this.base}/restore`, form, { reportProgress: true });
    return this.http.request(req);
  }

  getMode() {
    return this.http.get<{enabled:boolean;message?:string;allowRestore:boolean}>(`${this.base}/mode`);
  }
  setMode(enabled: boolean, message?: string) {
    return this.http.post(`${this.base}/mode`, { enabled, message });
  }
}
