// src/app/services/admin/receipt/receipt.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Employee } from '../../../models/employee';

// Purchase DTOs (unchanged)
export interface StockPurchaseLine {
  stockPurchaseLineId: number;
  stockPurchaseId:     number;
  productId:           number;
  product: { name: string };
}
export interface StockPurchase {
  stockPurchaseId: number;
  supplierName:    string;
  purchaseDate:    string;
  lines:           StockPurchaseLine[];
}

// Receipt‑side DTOs
export interface ReceiptLineDto {
  productId:        number;
  quantityReceived: number;
}

export interface StockReceiptDto {
  stockPurchaseId: number;
  receivedBy:      string;   // employeeID as string
  lines:           ReceiptLineDto[];
}

// Response DTO for a single line
export interface StockReceiptLineResponseDto {
  stockReceiptLineId: number;
  productId:          number;
  productName:        string;
  quantityReceived:   number;
}

// Response DTO for a full receipt
export interface StockReceiptResponseDto {
  stockReceiptId:   number;
  stockPurchaseId:  number;
  receiptDate:      string;
  receivedBy:       string;
  lines:            StockReceiptLineResponseDto[];
}

@Injectable({
  providedIn: 'root'
})
export class ReceiptService {
  private base = `${environment.apiUrl}/api/admin/stock`;

  constructor(private http: HttpClient) {}

  /** POST a new receipt */
  receiveStock(dto: StockReceiptDto): Observable<StockReceiptResponseDto> {
    return this.http.post<StockReceiptResponseDto>(`${this.base}/receipts`, dto);
  }

  /** GET lines for a single purchase (to build the form) */
  getPurchaseById(id: number): Observable<StockPurchase> {
    return this.http.get<StockPurchase>(`${this.base}/purchases/${id}`);
  }

  /** GET all purchases for the purchase‐selector dropdown */
  getAllPurchases(): Observable<StockPurchase[]> {
    return this.http.get<StockPurchase[]>(`${this.base}/purchases`);
  }

  /** GET all employees for the “received by” dropdown */
  getAllEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(`${environment.apiUrl}/api/admin/employees`);
  }

  /** GET all past receipts (with productName + qty) */
  getAllReceipts(): Observable<StockReceiptResponseDto[]> {
    return this.http.get<StockReceiptResponseDto[]>(`${this.base}/receipts`);
  }
}
