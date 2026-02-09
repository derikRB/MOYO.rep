// src/app/services/feature-access.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

export interface FeatureAccessRow {
  key: string;
  displayName: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class FeatureAccessService {
  private api = environment.apiUrl;

  constructor(private http: HttpClient) {}

  list(): Observable<FeatureAccessRow[]> {
    return this.http.get<FeatureAccessRow[]>(`${this.api}/api/admin/feature-access`);
  }

  save(rows: FeatureAccessRow[]): Observable<void> {
    return this.http.put<void>(`${this.api}/api/admin/feature-access`, rows);
  }

  my(): Observable<{features: string[]}> {
    return this.http.get<{features: string[]}>(`${this.api}/api/admin/feature-access/my`);
  }
}
