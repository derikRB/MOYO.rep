// import { Injectable } from '@angular/core';
// import { HttpClient } from '@angular/common/http';
// import { environment } from '../../environments/environment';
// import { Observable } from 'rxjs';

// export interface SalesReport {
//   orderDate: string;
//   orders: number;
//   revenue: number;
// }

// export interface OrderReport {
//   orderID: number;
//   orderDate: string;
//   customerName: string;
//   totalPrice: number;
//   orderStatusName: string;
//   deliveryMethod: string;
//   deliveryAddress: string;
// }

// export interface FinancialReport {
//   totalRevenue: number;
//   vatRate: number;
//   totalVAT: number;
//   netRevenue: number;
// }

// export interface StockForecast {
//   productID: number;
//   productName: string;
//   avgDailyQty: number;
//   predicted7DayQty: number;
// }

// @Injectable({ providedIn: 'root' })
// export class AdminReportsService {
//   // Change to 'apiUrl' (not apiBaseUrl!)
//   private baseUrl = environment.apiUrl + '/api/reports';

//   constructor(private http: HttpClient) {}

//   getSalesReport(): Observable<SalesReport[]> {
//     return this.http.get<SalesReport[]>(`${this.baseUrl}/sales`);
//   }

//   getOrderReport(): Observable<OrderReport[]> {
//     return this.http.get<OrderReport[]>(`${this.baseUrl}/orders`);
//   }

//   getFinancialReport(): Observable<FinancialReport> {
//     return this.http.get<FinancialReport>(`${this.baseUrl}/financials`);
//   }

//   getStockForecast(): Observable<StockForecast[]> {
//     return this.http.get<StockForecast[]>(`${this.baseUrl}/forecast`);
//   }
// }
