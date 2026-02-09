import { Injectable, inject, signal, WritableSignal } from '@angular/core';
import { BehaviorSubject, map } from 'rxjs';
import { Router, ActivatedRoute, NavigationEnd } from '@angular/router';

export type HelpRole = 'Admin' | 'Manager' | 'Employee' | 'Customer' | 'Unknown';

export interface HelpSection {
  key: string;              // unique id, e.g., 'orders'
  title: string;            // visible title
  content: string;          // html string (safe static content)
  keywords?: string[];      // extra search tokens
}

@Injectable({ providedIn: 'root' })
export class HelpService {
  // region: wiring
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  // region: state
  private _isOpen = new BehaviorSubject<boolean>(false);
  isOpen$ = this._isOpen.asObservable();

  private _activeKey = new BehaviorSubject<string>('home');
  activeKey$ = this._activeKey.asObservable();

  private _query = new BehaviorSubject<string>('');
  query$ = this._query.asObservable();

  private _role: WritableSignal<HelpRole> = signal<HelpRole>('Unknown');

  // Admin registry (extend freely)
  private adminSections: HelpSection[] = [
    {
      key: 'home',
      title: 'Welcome to Help (Admin)',
      content: `
        <p>This is the consolidated Help center for administrators, managers, and employees.</p>
        <ul>
          <li>Press <b>F1</b> any time to open context help for your current screen.</li>
          <li>Use the Search box to jump to any topic instantly.</li>
          <li>Click "Open Full Help (PDF)" for the full document.</li>
        </ul>
      `,
      keywords: ['help', 'support', 'guide', 'documentation']
    },
    {
      key: 'products',
      title: 'Products: Add / Edit / Delete',
      content: `
        <h4>Overview</h4>
        <p>Manage product catalog, pricing, images, and availability.</p>
        <h4>Key Actions</h4>
        <ul>
          <li><b>Add Product:</b> Provide name, price, stock policy, and category. Use tooltips to understand fields.</li>
          <li><b>Edit Product:</b> Update price, descriptions, and images. Changes reflect immediately in storefront.</li>
          <li><b>Delete/Archive:</b> Archiving keeps historical orders intact. Use Delete only when safe.</li>
        </ul>
      `,
      keywords: ['sku', 'price', 'image', 'catalog', 'inventory']
    },
    {
      key: 'product-types',
      title: 'Product Types',
      content: `
        <p>Define product families for shared attributes and options. Use this to standardize templates and variants.</p>
      `,
      keywords: ['type', 'family', 'variant']
    },
    {
      key: 'categories',
      title: 'Categories',
      content: `
        <p>Organize products for navigation and filtering. Keep names short and consistent.</p>
      `,
      keywords: ['taxonomy', 'navigation', 'browse']
    },
    {
      key: 'employees',
      title: 'Employees & Roles',
      content: `
        <ul>
          <li><b>Admin</b>: full access.</li>
          <li><b>Manager</b>: elevated access to operations and reports.</li>
          <li><b>Employee</b>: restricted operational access.</li>
        </ul>
        <p>Update roles to change access and UI visibility dynamically.</p>
      `,
      keywords: ['roles', 'rbac', 'permissions', 'staff']
    },
    {
      key: 'templates',
      title: 'Templates & Customizations',
      content: `
        <p>Upload and manage design templates. Configure supported customizations (text, color, images).</p>
        <p>For Konva-based previews, ensure layer naming follows your render logic.</p>
      `,
      keywords: ['konva', 'design', 'customization', 'preview']
    },
    {
      key: 'vat',
      title: 'VAT Setup',
      content: `
        <p>Configure VAT percentage and inclusive/exclusive pricing. Timers for OTP/session are managed in System Config.</p>
      `,
      keywords: ['tax', 'vat', 'finance']
    },
    {
      key: 'stock-purchases',
      title: 'Stock Purchases',
      content: `
        <p>Capture supplier purchases to increase on-hand stock. Attach waybills if required.</p>
      `,
      keywords: ['procurement', 'supplier', 'incoming', 'waybill']
    },
    {
      key: 'stock-receipts',
      title: 'Stock Receipts',
      content: `
        <p>Record physical receipt of stock. Validate quantities and quality before committing.</p>
      `,
      keywords: ['receiving', 'warehouse', 'inventory']
    },
    {
      key: 'stock-adjustments',
      title: 'Stock Adjustments',
      content: `
        <p>Adjust quantities due to shrinkage, damage, or audits. Always include a reason code.</p>
      `,
      keywords: ['shrinkage', 'audit', 'correction']
    },
    {
      key: 'low-stock-alerts',
      title: 'Low Stock Alerts',
      content: `
        <p>View SKUs below threshold. Replenish or adjust thresholds to reduce noise.</p>
      `,
      keywords: ['threshold', 'replenishment', 'alerts']
    },
    {
      key: 'orders',
      title: 'Orders: Manage & Fulfil',
      content: `
        <h4>Status Flow</h4>
        <p>New → In Progress → Dispatched → Delivered / Cancelled.</p>
        <h4>Tips</h4>
        <ul>
          <li>Use filters to locate orders quickly.</li>
          <li>Attach waybills and trigger delivery emails when status updates.</li>
        </ul>
      `,
      keywords: ['delivery', 'waybill', 'status', 'fulfilment']
    },
    {
      key: 'product-reviews',
      title: 'Product Reviews',
      content: `
        <p>Moderate customer reviews. Approve, hide, or respond. Ratings average into product detail pages.</p>
      `,
      keywords: ['rating', 'moderation', 'ugc', 'feedback']
    },
    {
      key: 'feedback',
      title: 'Customer Feedback (Admin View)',
      content: `
        <p>Track and resolve customer feedback tickets. Use tags, assign to staff, and close with resolution notes.</p>
      `,
      keywords: ['ticket', 'resolution', 'support']
    }
  ];

  // Customer registry (not used for Admin brief, but ready)
  private customerSections: HelpSection[] = [
    {
      key: 'home',
      title: 'Welcome to Help (Customer)',
      content: `<p>Search, shop, customize, checkout, and track your orders here.</p>`,
      keywords: ['customer', 'shopping', 'order']
    }
  ];

  // region: public API
  setRole(role: HelpRole) { this._role.set(role); }
  getRole() { return this._role(); }

  open(key?: string) {
    if (key) this._activeKey.next(key);
    this._isOpen.next(true);
  }
  close() { this._isOpen.next(false); }

  toggle(key?: string) {
    if (!this._isOpen.value) {
      this.open(key ?? this._activeKey.value);
    } else {
      if (key) this._activeKey.next(key);
      this._isOpen.next(false);
    }
  }

  setContext(key: string) {
    // For F1 / directive triggers on a page
    this._activeKey.next(key);
    this._isOpen.next(true);
  }

  setQuery(q: string) { this._query.next(q); }

  // Relevant list based on role (Admin, Manager, Employee => admin set; else customer)
  sections$ = this.activeKey$.pipe(
    map(() => {
      const r = this._role();
      if (r === 'Admin' || r === 'Manager' || r === 'Employee') return this.adminSections;
      return this.customerSections;
    })
  );

  filteredSections$ = this.query$.pipe(
    map(q => {
      const query = (q || '').trim().toLowerCase();
      const list = (['Admin','Manager','Employee'].includes(this._role()) ? this.adminSections : this.customerSections);
      if (!query) return list;
      return list.filter(s => {
        const hay = (s.title + ' ' + s.content + ' ' + (s.keywords || []).join(' ')).toLowerCase();
        return hay.includes(query);
      });
    })
  );

  // region: route integration (/help?topic=orders)
  initRouteActivation(): void {
    // Subscribe to router events once to react to /help?topic=...
    this.router.events.subscribe(ev => {
      if (ev instanceof NavigationEnd) {
        const qp = this.route.snapshot.queryParamMap;
        const topic = qp.get('topic');
        if (topic) {
          this.setContext(topic);
        }
      }
    });
  }
}
