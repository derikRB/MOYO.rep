import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

// DTO definitions matching our C# responses
export interface StockPurchaseLine {
  productId:   number;
  productName: string;
  quantity:    number;
  unitPrice:   number;
}
export interface StockPurchase {
  stockPurchaseId: number;
  supplierName:    string;
  purchaseDate:    string;
  lines:           StockPurchaseLine[];
}

export interface PurchaseLine {
  productId: number;
  quantity:  number;
  unitPrice: number;
}

export interface CaptureDto {
  supplierName: string;
  lines: PurchaseLine[];
}

@Injectable({
  providedIn: 'root'
})
export class StockService {
  private base = `${environment.apiUrl}/api/admin/stock`;

  constructor(private http: HttpClient) {}

  capturePurchase(dto: CaptureDto): Observable<any> {
    return this.http.post<any>(`${this.base}/purchases`, dto);
  }

  getAllPurchases(): Observable<StockPurchase[]> {
    return this.http.get<StockPurchase[]>(`${this.base}/purchases`);
  }
}
