// src/app/services/product-type.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError }  from 'rxjs';
import { catchError }              from 'rxjs/operators';
import { environment }             from '../../environments/environment';
import { ProductType } from '../models/product-type.model';

@Injectable({ providedIn: 'root' })
export class ProductTypeService {
  private apiUrl = `${environment.apiUrl}/api/producttype`;

  constructor(private http: HttpClient) {}

  getProductTypes(): Observable<ProductType[]> {
    const token   = localStorage.getItem('token') || '';
    const headers = new HttpHeaders({ Authorization: `Bearer ${token}` });
    return this.http.get<ProductType[]>(this.apiUrl, { headers })
      .pipe(catchError(err => throwError(() => err)));
  }
}
