// src/app/services/admin.service.ts

import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError }  from 'rxjs';
import { catchError }              from 'rxjs/operators';
import { Product }                 from '../models/product.model';
import { ProductImage }            from '../models/product-image.model';
import { environment }             from '../../environments/environment'; // ← ADD THIS LINE

export interface Employee {
  employeeID: number;
  username:   string;
  email:      string;
  role:       string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private baseUrl = 'https://localhost:7025/api';

  private get headers(): { headers: HttpHeaders } {
    return {
      headers: new HttpHeaders({
        Authorization: `Bearer ${localStorage.getItem('token')}`
      })
    };
  }

  constructor(private http: HttpClient) {}

  // ─── PRODUCTS ─────────────────────────────────────────

  getProducts(): Observable<Product[]> {
    return this.http
      .get<Product[]>(`${this.baseUrl}/product`, this.headers)
      .pipe(
        catchError(err => {
          console.error('Failed to load products', err);
          return throwError(() => new Error('Failed to load products'));
        })
      );
  }

  addProduct(data: any): Observable<Product> {
    return this.http.post<Product>(
      `${this.baseUrl}/product`,
      data,
      this.headers
    );
  }

  updateProduct(id: number, data: any): Observable<Product> {
    return this.http.put<Product>(
      `${this.baseUrl}/product/${id}`,
      data,
      this.headers
    );
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/product/${id}`,
      this.headers
    );
  }

  // ─── IMAGE ENDPOINTS ─────────────────────────────────

  uploadProductImages(
    productId: number,
    files: FileList
  ): Observable<void> {
    const formData = new FormData();
    Array.from(files).forEach(f => formData.append('files', f));
    return this.http.post<void>(
      `${this.baseUrl}/product/${productId}/images`,
      formData,
      this.headers
    );
  }

  deleteProductImage(imageId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/product/images/${imageId}`,
      this.headers
    );
  }

  // ─── PRODUCT TYPE ────────────────────────────────────

  getProductTypes(): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.baseUrl}/producttype`,
      this.headers
    );
  }
  addProductType(data: any): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}/producttype`,
      data,
      this.headers
    );
  }
  updateProductType(id: number, data: any): Observable<any> {
    return this.http.put<any>(
      `${this.baseUrl}/producttype/${id}`,
      data,
      this.headers
    );
  }
  deleteProductType(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/producttype/${id}`,
      this.headers
    );
  }

  // ─── CATEGORY ────────────────────────────────────────

  getCategories(): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.baseUrl}/category`,
      this.headers
    );
  }
  addCategory(data: any): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}/category`,
      data,
      this.headers
    );
  }
  updateCategory(id: number, data: any): Observable<any> {
    return this.http.put<any>(
      `${this.baseUrl}/category/${id}`,
      data,
      this.headers
    );
  }
  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/category/${id}`,
      this.headers
    );
  }

  // ─── EMPLOYEE ─────────────────────────────────────────

  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(
      `${this.baseUrl}/employee/all`,
      this.headers
    );
  }
  registerEmployee(data: {
    emailOrUsername: string;
    password:        string;
  }): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}/employee/register`,
      data,
      this.headers
    );
  }
  updateEmployee(id: number, data: Employee): Observable<Employee> {
    return this.http.put<Employee>(
      `${this.baseUrl}/employee/${id}`,
      data,
      this.headers
    );
  }

  bulkImportProducts(products: any[]): Observable<any> {
return this.http.post(`${environment.apiUrl}/api/Product/import`, products);
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/employee/${id}`,
      this.headers
    );
  }
  searchEmployees(query: string): Observable<Employee[]> {
    return this.http.get<Employee[]>(
      `${this.baseUrl}/employee/search?q=${query}`,
      this.headers
    );
  }
}
