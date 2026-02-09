import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { ProductReviewDto, CreateProductReviewDto } from '../dto/product-review';

@Injectable({ providedIn: 'root' })
export class ProductReviewService {
  private baseUrl = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) { }

  submitReview(dto: CreateProductReviewDto): Observable<ProductReviewDto> {
    const formData = new FormData();
    formData.append('ProductID', String(dto.productID));
    formData.append('OrderID', String(dto.orderID));
    formData.append('Rating', String(dto.rating));
    if (dto.reviewTitle) formData.append('ReviewTitle', dto.reviewTitle);
    formData.append('ReviewText', dto.reviewText);
    if (dto.photo) formData.append('Photo', dto.photo);

    return this.http.post<ProductReviewDto>(`${this.baseUrl}/productreview`, formData);
  }

  getProductReviews(productId: number): Observable<ProductReviewDto[]> {
    return this.http.get<ProductReviewDto[]>(`${this.baseUrl}/productreview/product/${productId}`);
  }

  getPendingReviews(): Observable<ProductReviewDto[]> {
    return this.http.get<ProductReviewDto[]>(`${this.baseUrl}/admin/productreview/pending`);
  }

  approveReview(reviewId: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/admin/productreview/${reviewId}/approve`, {});
  }

  declineReview(reviewId: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/admin/productreview/${reviewId}/decline`, {});
  }

  getReviewsByStatus(status: string): Observable<ProductReviewDto[]> {
    return this.http.get<ProductReviewDto[]>(`${this.baseUrl}/admin/productreview/status/${status}`);
  }

  getAllReviewsPaged(page = 1, pageSize = 20): Observable<{ totalCount: number, items: ProductReviewDto[] }> {
    return this.http.get<{ totalCount: number, items: ProductReviewDto[] }>(
      `${this.baseUrl}/admin/productreview/all?page=${page}&pageSize=${pageSize}`
    );
  }

  bulkApprove(reviewIds: number[]): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/admin/productreview/bulk-approve`, reviewIds);
  }

  bulkDecline(reviewIds: number[]): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/admin/productreview/bulk-decline`, reviewIds);
  }

  hasReviewed(orderId: number, productId: number): Observable<boolean> {
    // Backend API endpoint should exist: GET /api/productreview/has-reviewed?orderId=..&productId=..
    return this.http.get<boolean>(
      `${this.baseUrl}/productreview/has-reviewed?orderId=${orderId}&productId=${productId}`
    );
  }

}
