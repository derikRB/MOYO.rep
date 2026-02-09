// src/app/services/shared-status.service.ts
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SharedStatusService {

  getEffectiveStatus(order: any): string {
    const ds = (order.deliveryStatus || '').trim().toLowerCase();
    const os = (order.orderStatusName || 'Pending').trim().toLowerCase();

    // Delivery status takes priority
    if (ds.includes('delivered')) return 'Delivered';
    if (ds.includes('dispatched')) return 'Dispatched';
    if (ds && ds !== 'pending') {
      return ds.charAt(0).toUpperCase() + ds.slice(1);
    }

    // Fallback to order status
    if (os.includes('delivered')) return 'Delivered';   // ✅ catch Delivered
    if (os.includes('shipped')) return 'Shipped';
    if (os.includes('processing')) return 'Processing';

    return 'Pending';
  }
}
