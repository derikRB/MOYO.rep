// src/app/services/orders-state.service.ts
import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, interval, Subscription } from 'rxjs';
import { startWith, switchMap, map, distinctUntilChanged, catchError } from 'rxjs/operators';
import { OrderService } from './order.service';
import type { OrderResponseDto } from '../dto/order-response.dto';

@Injectable({ providedIn: 'root' })
export class OrdersStateService implements OnDestroy {
  // Public observable components can subscribe to
  private _pendingCount = new BehaviorSubject<number>(0);
  public pendingCount$ = this._pendingCount.asObservable();

  private pollSub?: Subscription;
  private isPolling = false;

  constructor(private orderService: OrderService) {}

  /**
   * Start polling GET /api/Order/all and update pending-count.
   * Default poll interval is 15s (15000 ms). Tune as needed.
   */
  startPolling(intervalMs = 15000) {
    if (this.isPolling) return; // already started

    this.isPolling = true;
    this.pollSub = interval(intervalMs)
      .pipe(
        startWith(0),
        switchMap(() => this.orderService.getAllOrders().pipe(
          catchError(() => []) // return empty array on error to keep polling alive
        )),
        map(orders => this.calculatePending(orders || [])),
        distinctUntilChanged()
      )
      .subscribe({
        next: (count) => this._pendingCount.next(count),
        error: (err) => {
          console.error('Polling error:', err);
          this.isPolling = false;
        }
      });
  }

  stopPolling() {
    if (this.pollSub) {
      this.pollSub.unsubscribe();
      this.pollSub = undefined;
      this.isPolling = false;
    }
  }

  /**
   * Immediate population from a known order list (used by AdminManageOrdersComponent after manual loads).
   */
  updateFromOrders(orders: OrderResponseDto[] | null | undefined) {
    const cnt = this.calculatePending(orders || []);
    this._pendingCount.next(cnt);
  }

  /**
   * Count orders that are NOT delivered (Pending, Processing, Shipped, Dispatched)
   */
  private calculatePending(orders: OrderResponseDto[]): number {
    let count = 0;
    for (const o of orders || []) {
      // Only count orders that are NOT delivered
      if (this.effectiveStatus(o) !== 'Delivered') count++;
    }
    return count;
  }

  /**
   * Matches the same effectiveStatus logic used in AdminManageOrdersComponent.
   * BUT we only care about counting NON-DELIVERED orders for the badge.
   */
  private effectiveStatus(o: { orderStatusName?: string | null; deliveryStatus?: string | null; }): 'Pending'|'Processing'|'Shipped'|'Delivered'|'Dispatched' {
    const ds = (o.deliveryStatus || '').trim().toLowerCase();
    const os = (o.orderStatusName || 'Pending').trim().toLowerCase();

    // Delivery status takes priority - if delivered, it's delivered regardless of order status
    if (ds.includes('delivered')) return 'Delivered';
    if (ds.includes('dispatched')) return 'Dispatched';
    if (ds && ds !== 'pending') return (ds.charAt(0).toUpperCase() + ds.slice(1)) as any;

    // Only check order status if delivery status is not informative
    if (os.includes('shipped')) return 'Shipped';
    if (os.includes('processing')) return 'Processing';

    return 'Pending';
  }

  ngOnDestroy() {
    this.stopPolling();
    this._pendingCount.complete();
  }
}