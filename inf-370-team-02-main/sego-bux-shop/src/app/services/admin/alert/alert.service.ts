import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, switchMap, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface LowStockAlert {
  lowStockAlertId: number;
  productId: number;
  product: { name: string };
  stockQuantity: number;
  alertDate: string;
  notified: boolean;
  resolved: boolean;
}

@Injectable({ providedIn: 'root' })
export class AlertService {
  private base = `${environment.apiUrl}/api/admin/stock`;
  private countSubject = new BehaviorSubject<number>(0);
  /** Expose as an Observable for your sidebar badge */
  lowStockCount$ = this.countSubject.asObservable();

  constructor(private http: HttpClient) {}

  /** Triggers the backend check & emails, then pushes new count */
  checkLowStock(): Observable<LowStockAlert[]> {
    return this.http.post<LowStockAlert[]>(`${this.base}/alerts/check`, {})
      .pipe(tap(alerts => this.emitCount(alerts)));
  }

  /** Fetches **all** alerts (historical + new) and pushes count */
  getLowStockAlerts(): Observable<LowStockAlert[]> {
    return this.http.get<LowStockAlert[]>(`${this.base}/alerts`)
      .pipe(tap(alerts => this.emitCount(alerts)));
  }

  /** Marks one alert resolved, then re‑fetches everything (and pushes count) */
  resolveAlert(alertId: number): Observable<LowStockAlert[]> {
    return this.http.put<void>(`${this.base}/alerts/${alertId}/resolve`, {})
      .pipe(switchMap(() => this.getLowStockAlerts()));
  }

  /** Internal utility: count unresolved items and next() it */
  private emitCount(alerts: LowStockAlert[]) {
    const unresolved = alerts.filter(a => !a.resolved).length;
    this.countSubject.next(unresolved);
  }
}
