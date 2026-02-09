import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

export type HelpRole = 'Customer' | 'Employee' | 'Manager' | 'Admin' | 'Guest';

/**
 * Central Help state: open/close, current context key (set by routes or appContextHelp),
 * and role-awareness (for tailoring topics).
 */
@Injectable({ providedIn: 'root' })
export class HelpService {
  private isOpenSubject = new BehaviorSubject<boolean>(false);
  readonly isOpen$ = this.isOpenSubject.asObservable();

  private keySubject = new BehaviorSubject<string | null>(null);
  /** Current help key as driven by route or directive */
  readonly keyContext$ = this.keySubject.asObservable();

  // AuthService can be unavailable during bootstrap; guard all calls.
  private auth = inject(AuthService, { optional: true } as any);

  private roleSubject = new BehaviorSubject<HelpRole>(this.computeRole());
  readonly role$: Observable<HelpRole> = this.roleSubject.asObservable();

  /** Route → default help key mapping (fallback if directive not used) */
  private routeKeyMap: Record<string, string> = {
    // public/customer areas
    '': 'home',
    'products': 'shop',
    'products/:id': 'shop-detail',
    'account': 'account',
    'profile': 'account',
    'profile/orders': 'orders-customer',
    'cart': 'cart',
    'checkout': 'checkout',
    'product-review/:orderId': 'product-review',
    'feedback/:orderId': 'feedback',
    // admin areas
    'admin': 'system',
    'admin/system': 'system',
    'admin/reports': 'reports',
    'admin/products': 'products',
    'admin/product-types': 'product-types',
    'admin/categories': 'categories',
    'admin/employees': 'employees',
    'admin/templates': 'templates',
    'admin/vat': 'vat',
    'admin/stock-purchases': 'stock-purchases',
    'admin/stock-receipts': 'stock-receipts',
    'admin/stock-adjustments': 'stock-adjustments',
    'admin/low-stock-alerts': 'low-stock-alerts',
    'admin/faqs': 'faq',
    'admin/chatbot-config': 'chatbot',
    'admin/orders': 'orders'
  };

  /** Toggle modal. If key provided, set context before toggling. */
  toggle(key?: string) {
    if (key) this.setKey(key);
    this.isOpenSubject.next(!this.isOpenSubject.value);
  }
  open(key?: string) {
    if (key) this.setKey(key);
    this.isOpenSubject.next(true);
  }
  close() { this.isOpenSubject.next(false); }

  /** Set current contextual key (used by directive and router) */
  setKey(key: string | null) { this.keySubject.next(key); }

  /** Set context based on a route string (e.g., 'admin/orders') */
  setKeyFromRoute(routeKey: string) {
    const k = this.routeKeyMap[routeKey] || null;
    this.setKey(k);
  }

  /** Force role refresh (e.g., after login/logout) */
  refreshRole() { this.roleSubject.next(this.computeRole()); }

  private computeRole(): HelpRole {
    // Safe access: Auth may not be available yet; guard calls
    const tokenRole =
      (this.auth && typeof this.auth.getRoleFromToken === 'function')
        ? this.auth.getRoleFromToken()
        : null;

    const r = tokenRole || localStorage.getItem('userRole') || '';

    if (r === 'Admin' || r === 'Manager' || r === 'Employee') return r as HelpRole;
    if (r === 'Customer') return 'Customer';
    return 'Guest';
  }
}
