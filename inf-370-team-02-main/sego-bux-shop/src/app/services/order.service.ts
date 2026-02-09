import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import type { OrderDto } from '../dto/order.dto';
import type { OrderResponseDto } from '../dto/order-response.dto';

export interface DeliveryCalcResult {
  deliveryMethod: string;
  distance: number;
  shippingFee: number;
}

export interface DeliveryUpdate {
  deliveryStatus: string;
  waybillNumber?: string;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private base = `${environment.apiUrl}/api/Order`;

  constructor(
    private http: HttpClient,
    private auth: AuthService
  ) {}

  private authHeaders(): HttpHeaders {
    const token = this.auth.getToken() || '';
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  placeOrder(dto: OrderDto): Observable<OrderResponseDto> {
    return this.http.post<OrderResponseDto>(
      `${this.base}/place`,
      dto,
      { headers: this.authHeaders() }
    );
  }

  calculateDelivery(address: string): Observable<DeliveryCalcResult> {
    return this.http.post<DeliveryCalcResult>(
      `${this.base}/calculateDelivery`,
      { address },
      { headers: this.authHeaders() }
    );
  }

  getOrdersByCustomer(customerId: number): Observable<OrderResponseDto[]> {
    return this.http.get<OrderResponseDto[]>(
      `${this.base}/customer/${customerId}`,
      { headers: this.authHeaders() }
    );
  }

  getAllOrders(): Observable<OrderResponseDto[]> {
    return this.http.get<OrderResponseDto[]>(
      `${this.base}/all`,
      { headers: this.authHeaders() }
    );
  }

  getOrderById(orderId: number): Observable<OrderResponseDto> {
    return this.http.get<OrderResponseDto>(
      `${this.base}/${orderId}`,
      { headers: this.authHeaders() }
    );
  }

  updateOrderStatus(orderId: number, newStatusId: number): Observable<void> {
    return this.http.put<void>(
      `${this.base}/${orderId}/status/${newStatusId}`,
      null,
      { headers: this.authHeaders() }
    );
  }

  updateDelivery(orderId: number, payload: DeliveryUpdate): Observable<void> {
    return this.http.put<void>(
      `${this.base}/${orderId}/delivery`,
      payload,
      { headers: this.authHeaders() }
    );
  }

  updateExpectedDeliveryDate(orderId: number, date: string): Observable<void> {
    return this.http.patch<void>(
      `${this.base}/expectedDeliveryDate`,
      date,
      { headers: this.authHeaders() }
    );
  }

  // --- FILE UPLOADS ---
  uploadCustomizationImage(orderId: number, lineId: number, file: File): Observable<string> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ imageUrl: string }>(
      `${environment.apiUrl}/orders/${orderId}/lines/${lineId}/customization/upload-image`,
      form,
      { headers: this.authHeaders() }
    ).pipe(map(r => r.imageUrl));
  }

  uploadCustomizationSnapshot(orderId: number, lineId: number, file: File): Observable<string> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ imageUrl: string }>(
      `${environment.apiUrl}/orders/${orderId}/lines/${lineId}/customization/upload-snapshot`,
      form,
      { headers: this.authHeaders() }
    ).pipe(map(r => r.imageUrl));
  }
}
