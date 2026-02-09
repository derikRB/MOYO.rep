import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface StockAdjustmentDto {
  productId: number;
  adjustmentQty: number;
  reason: string;     // stored as TEXT for immutable history
  adjustedBy: string;
}

export interface StockAdjustment {
  stockAdjustmentId: number;
  productId: number;
  adjustmentQty: number;
  reason: string;
  adjustedBy: string;
  adjustmentDate: string;
}

/** Reason DTOs */
export interface StockReasonDto {
  name: string;
  sortOrder: number;
  isActive: boolean;
}
export interface StockReason {
  stockReasonId: number;
  name: string;
  sortOrder: number;
  isActive: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AdjustmentService {
  private base = `${environment.apiUrl}/api/admin/stock`;

  constructor(private http: HttpClient) {}

  // Adjustments
  adjustStock(dto: StockAdjustmentDto): Observable<StockAdjustment> {
    return this.http.post<StockAdjustment>(`${this.base}/adjustments`, dto);
  }
  getAllAdjustments(): Observable<StockAdjustment[]> {
    return this.http.get<StockAdjustment[]>(`${this.base}/adjustments`);
  }

  // Reasons
  getReasons(includeInactive = false): Observable<StockReason[]> {
    const params = new HttpParams().set('includeInactive', String(includeInactive));
    return this.http.get<StockReason[]>(`${this.base}/reasons`, { params });
  }
  createReason(dto: StockReasonDto): Observable<StockReason> {
    return this.http.post<StockReason>(`${this.base}/reasons`, dto);
  }
  updateReason(id: number, dto: StockReasonDto): Observable<StockReason> {
    return this.http.put<StockReason>(`${this.base}/reasons/${id}`, dto);
  }
  /** Soft delete (deactivate) */
  deleteReason(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/reasons/${id}`);
  }
}
