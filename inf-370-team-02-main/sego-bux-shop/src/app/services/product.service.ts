import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

import type { Product } from '../models/product.model';
import type { ProductImage } from '../models/product-image.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private apiUrl = `${environment.apiUrl}/api/product`;
  public baseUrl = `${environment.apiUrl}/api/product`;
  private imagesBase = environment.apiUrl;  // e.g. https://localhost:7025

  constructor(private http: HttpClient) { }

  getProducts(): Observable<Product[]> {
    const token = localStorage.getItem('token') || '';
    const headers = new HttpHeaders({ Authorization: `Bearer ${token}` });

    return this.http.get<Product[]>(this.apiUrl, { headers }).pipe(
      catchError(err => {
        console.error('Failed to load products', err);
        return throwError(() => new Error('Failed to load products'));
      })
    );
  }


  imageFullUrl(img: ProductImage): string {
    return this.imagesBase + img.imageUrl;
  }

  getProductById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

}
