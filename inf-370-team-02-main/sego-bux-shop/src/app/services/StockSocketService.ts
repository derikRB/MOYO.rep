import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class StockSocketService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private stockChangedSource = new BehaviorSubject<{ productId: number, newStock: number } | null>(null);
  public stockChanged$ = this.stockChangedSource.asObservable();

  // ---- Optional: Track if we're connected ----
  private isConnected = false;

  connect(): void {
    if (this.isConnected || this.hubConnection) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${environment.apiUrl}/stockHub`)
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .build();


    // Listen for stock updates from the server
    this.hubConnection.on('ReceiveStockUpdate', (msg: any) => {
      // Support both camelCase and PascalCase property names
      const productId = msg.productId ?? msg.productID;
      const newStock = msg.newStock ?? msg.stockQuantity;
      if (productId !== undefined && newStock !== undefined) {
        this.stockChangedSource.next({ productId, newStock });
      }
    });

    this.hubConnection.start()
      .then(() => {
        this.isConnected = true;
        console.log('[SignalR] StockHub connected');
      })
      .catch(err => {
        this.isConnected = false;
        console.error('[SignalR] StockHub connection failed:', err);
      });

    this.hubConnection.onclose(() => {
      this.isConnected = false;
      console.warn('[SignalR] StockHub disconnected');
    });
  }

  disconnect(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.isConnected = false;
    }
  }

  ngOnDestroy(): void {
    this.disconnect();
  }
}
