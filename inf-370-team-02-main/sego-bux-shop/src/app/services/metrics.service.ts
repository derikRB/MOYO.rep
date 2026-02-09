import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface OrderStatusCountDto { status: string; count: number; }
export interface SalesPointDto { date: string; total: number; }
export interface LowStockPointDto { label: string; qty: number; threshold: number; }

@Injectable({ providedIn: 'root' })
export class MetricsService {
  private base = `${environment.apiUrl}/api/metrics`;
  constructor(private http: HttpClient) {}

 // Add this to your MetricsService
getOrdersWithStatus(range: 'today'|'7d') {
  return this.http.get<any[]>(`${this.base}/orders-with-status`, { params: { range } as any });
}
  getSalesLast30d() {
    return this.http.get<SalesPointDto[]>(`${this.base}/sales-last-30d`);
  }
  getLowStock(top = 10) {
    return this.http.get<LowStockPointDto[]>(`${this.base}/low-stock`, { params: { top } as any });
  }

  toDoughnut(rows: OrderStatusCountDto[])   { return { labels: rows.map(r => r.status), values: rows.map(r => r.count) }; }
  toLine(rows: SalesPointDto[])             { return { labels: rows.map(r => r.date), totals: rows.map(r => r.total) }; }
  toBarLowStock(rows: LowStockPointDto[])   { return { labels: rows.map(r => r.label), qty: rows.map(r => r.qty), thr: rows.map(r => r.threshold) }; }
}
