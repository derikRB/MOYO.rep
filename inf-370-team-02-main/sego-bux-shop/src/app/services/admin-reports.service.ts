// src/app/services/admin-reports.service.ts

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable }            from 'rxjs';
import { environment }           from '../../environments/environment';

export interface SalesReport {
  orderDate: string;
  orders:    number;
  revenue:   number;
}

export interface OrderReport {
  orderID:         number;
  orderDate:       string;
  customerName:    string;
  orderStatusName: string;
  totalPrice:      number;
  deliveryMethod:  string;
  deliveryAddress: string;
}

export interface FinancialReport {
  totalRevenue: number;
  vatRate:      number;
  totalVAT:     number;
  netRevenue:   number;
}

export interface StockForecast {
  productID:       number;
  productName:     string;
  avgDailyQty:     number;
  predicted7DayQty:number;
}

export interface StockReport {
  categoryID:   number;
  categoryName: string;
  totalStock:   number;
}

export interface OrdersByCustomer {
  customerID:   number;
  customerName: string;
  orderID:      number;
  orderDate:    string;
  status:       string;
  total:        number;
}

export interface StockTransaction {
  productID:   number;
  productName: string;
  tranDate:    string | null;
  received:    number | null;
  adjusted:    number | null;
}

/** “Flat” 3‐period snapshot DTO from `/financials-by-period` */
export interface FinancialReportByPeriod {
  period:       string;  // e.g. "All Time", "August 2025", "Week 1 of August 2025"
  totalRevenue: number;
  vatRate:      number;
  totalVAT:     number;
  netRevenue:   number;
}

/** Hierarchical node for “All Time” → months → weeks */
export interface PeriodNode {
  period:       string;    // e.g. "All Time", "July 2025", "Week 2 of July 2025"
  totalRevenue: number;
  vatRate:      number;
  totalVAT:     number;
  netRevenue:   number;
  children:     PeriodNode[];
}

@Injectable({ providedIn: 'root' })
export class AdminReportsService {
  private base = `${environment.apiUrl}/api/reports`;

  constructor(private http: HttpClient) {}

  /** Sales data (optionally filtered by from/to ISO dates) */
  getSalesReport(from?: string, to?: string): Observable<SalesReport[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to)   params = params.set('to',   to);
    return this.http.get<SalesReport[]>(`${this.base}/sales`, { params });
  }

  /** All orders */
  getOrderReport(): Observable<OrderReport[]> {
    return this.http.get<OrderReport[]>(`${this.base}/orders`);
  }

  /** Single‐row financial snapshot */
  getFinancialReport(): Observable<FinancialReport> {
    return this.http.get<FinancialReport>(`${this.base}/financials`);
  }

  /** 7-day stock forecast per product */
  getStockForecast(): Observable<StockForecast[]> {
    return this.http.get<StockForecast[]>(`${this.base}/forecast`);
  }

  /** Current stock by category */
  getStockReport(): Observable<StockReport[]> {
    return this.http.get<StockReport[]>(`${this.base}/stockreport`);
  }

  /** Orders grouped by customer */
  getOrdersByCustomer(): Observable<OrdersByCustomer[]> {
    return this.http.get<OrdersByCustomer[]>(`${this.base}/ordersbycustomer`);
  }

  /** All stock transaction history */
  getStockTransactions(): Observable<StockTransaction[]> {
    return this.http.get<StockTransaction[]>(`${this.base}/stocktransactions`);
  }

  /** Flat 3-period report (All Time, current month, current week) */
  getFinancialReportsByPeriod(): Observable<FinancialReportByPeriod[]> {
    return this.http.get<FinancialReportByPeriod[]>(
      `${this.base}/financials-by-period`
    );
  }

  /**
   * Hierarchical financials:
   *  • root = “All Time”
   *  • root.children = each month that has orders
   *  • each month.children = the weeks within that month
   */
  getFinancialHierarchy(): Observable<PeriodNode> {
    return this.http.get<PeriodNode>(
      `${this.base}/financials-hierarchy`
    );
  }
}
