// src/app/components/admin-sidebar.component.ts
import {
  Component, Input, Output, EventEmitter,
  HostListener, OnInit, OnDestroy
} from '@angular/core';
import { RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../shared/toast.service';
import { OrdersStateService } from '../services/orders-state.service'; // <-- ADD THIS
import { Subscription } from 'rxjs'; // <-- ADD THIS

interface NavLink {
  label: string;
  route?: string;
  icon: string;
  badge?: () => number | null;
  children?: NavLink[];
}

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './admin-sidebar.component.html',
  styleUrls: ['./admin-sidebar.component.scss'],
})
export class AdminSidebarComponent implements OnInit, OnDestroy {
  private _sidebarOpen = true;
  private pendingCountSub?: Subscription; // <-- ADD THIS
  pendingOrdersCount = 0; // <-- ADD THIS

  @Input() set sidebarOpen(v: boolean) {
    this._sidebarOpen = v;
    this.updateBodyScrollLock();
  }
  get sidebarOpen() { return this._sidebarOpen; }

  @Input() collapsed = false;
  @Input() lowStockCount = 0;
  @Output() closeSidebar = new EventEmitter<void>();

  expandedSections = new Set<string>();
  isMobile = window.innerWidth <= 900;

  navLinks: NavLink[] = [
    { label: 'Dashboard', route: '/admin/system', icon: 'fa-tachometer-alt' },
    { label: 'Reports', route: '/admin/reports', icon: 'fa-chart-bar' },
    {
      label: 'Stock Manager',
      icon: 'fa-cogs',
      children: [
        { label: 'Purchases', route: '/admin/stock-purchases', icon: 'fa-truck-loading' },
        { label: 'Receipts', route: '/admin/stock-receipts', icon: 'fa-truck' },
        { label: 'Adjustments', route: '/admin/stock-adjustments', icon: 'fa-balance-scale' },
        {
          label: 'Low-Stock Alerts',
          route: '/admin/low-stock-alerts',
          icon: 'fa-exclamation-triangle',
          badge: () => this.lowStockCount > 0 ? this.lowStockCount : null
        }
      ]
    },
    { label: 'Categories', route: '/admin/categories', icon: 'fa-folder' },
    { label: 'Product Types', route: '/admin/product-types', icon: 'fa-tags' },
    { label: 'Products', route: '/admin/products', icon: 'fa-box' },
    { label: 'Templates', route: '/admin/templates', icon: 'fa-th-large' },
    { label: 'VAT', route: '/admin/vat', icon: 'fa-coins' },
    { label: 'Employees', route: '/admin/employees', icon: 'fa-users' },
    { 
      label: 'Orders', 
      route: '/admin/orders', 
      icon: 'fa-receipt',
      badge: () => this.pendingOrdersCount > 0 ? this.pendingOrdersCount : null // <-- ADD THIS
    },
    { label: 'Product Reviews', route: '/admin/product-reviews', icon: 'fa-star' },
    { label: 'Customer Feedback', route: '/admin/feedback', icon: 'fa-comments' },
    { label: 'Help|Shipping', route: '/admin/chatbot-config', icon: 'fa-robot' }
  ];

  constructor(
    private authService: AuthService,
    private router: Router,
    private toastSvc: ToastService,
    private ordersStateService: OrdersStateService // <-- ADD THIS
  ) {}

  ngOnInit() {
    this.updateBodyScrollLock();
    this.startPendingOrdersListener(); // <-- ADD THIS
  }

  // <-- ADD THIS METHOD
  private startPendingOrdersListener() {
    this.ordersStateService.startPolling(15000); // Poll every 15 seconds
    
    this.pendingCountSub = this.ordersStateService.pendingCount$.subscribe({
      next: (count) => {
        this.pendingOrdersCount = count;
      },
      error: (err) => {
        console.error('Error in pending orders count:', err);
      }
    });
  }

  @HostListener('window:resize')
  onResize() {
    this.isMobile = window.innerWidth <= 900;
    this.updateBodyScrollLock();
  }

  onLinkClick() {
    if (this.isMobile) this.close();
  }

  toggleSection(label: string) {
    if (this.expandedSections.has(label)) this.expandedSections.delete(label);
    else this.expandedSections.add(label);
  }

  isExpanded(label: string) {
    return this.expandedSections.has(label);
  }

  close() {
    this.closeSidebar.emit();
    document.body.classList.remove('no-scroll');
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
    this.toastSvc.show('You have been logged out successfully.');
  }

  private updateBodyScrollLock() {
    if (this.isMobile && this._sidebarOpen) {
      document.body.classList.add('no-scroll');
    } else {
      document.body.classList.remove('no-scroll');
    }
  }

  ngOnDestroy() {
    document.body.classList.remove('no-scroll');
    if (this.pendingCountSub) {
      this.pendingCountSub.unsubscribe();
    }
    this.ordersStateService.stopPolling(); // <-- ADD THIS
  }
}