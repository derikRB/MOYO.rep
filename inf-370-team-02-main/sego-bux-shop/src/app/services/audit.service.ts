import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { PagedResult, AuditLogDto } from '../models/dtos';

@Injectable({ providedIn: 'root' })
export class AuditService {
  constructor(private http: HttpClient) {}

  // Matches: GET /api/audit?from=&to=&user=&action=&entity=&page=&pageSize=
  search(opts: {
    from?: string; to?: string; user?: string; action?: string; entity?: string;
    page?: number; pageSize?: number;
  }): Observable<PagedResult<AuditLogDto>> {
    let p = new HttpParams();
    Object.entries(opts).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') p = p.set(k, String(v));
    });
    return this.http.get<PagedResult<AuditLogDto>>('/api/audit', { params: p });
  }

  getById(id: number): Observable<AuditLogDto> {
    return this.http.get<AuditLogDto>(`/api/audit/${id}`);
  }
}
