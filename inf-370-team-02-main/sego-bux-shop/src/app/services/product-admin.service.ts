import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProductDto {
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  productTypeID: number;
}

@Injectable({ providedIn: 'root' })
export class ProductAdminService {
  private apiUrl = 'https://localhost:7025/api/Product';

  constructor(private http: HttpClient) {}

  private getHeaders() {
    return {
      headers: new HttpHeaders({
        Authorization: `Bearer ${localStorage.getItem('token') || ''}`
      })
    };
  }

  getAll(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl, this.getHeaders());
  }

  create(product: ProductDto): Observable<any> {
    return this.http.post(this.apiUrl, product, this.getHeaders());
  }

  update(id: number, product: ProductDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, product, this.getHeaders());
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`, this.getHeaders());
  }
}
