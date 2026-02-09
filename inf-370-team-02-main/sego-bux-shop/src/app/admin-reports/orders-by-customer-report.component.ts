import { Component, Input } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportBrandingHeaderComponent } from './report-branding-header.component';
import { jsPDF, autoTable, addBranding } from './report-utils';
import { SharedStatusService } from '../services/shared-status.service';

interface Order {
  orderID: number;
  orderDate: string | Date;
  orderStatusName: string;
  deliveryStatus?: string;
  totalPrice: number;
}

interface Customer {
  customerName: string;
  orders: Order[];
  filteredOrders?: Order[];
}

@Component({
  selector: 'app-orders-by-customer-report',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, DecimalPipe, ReportBrandingHeaderComponent],
  template: `
    <app-report-branding-header [date]="date" [userName]="userName"></app-report-branding-header>
    <h2 style="font-weight:bold; margin-top:18px;">Orders by Customer Report</h2>

    <!-- Pink search toolbar -->
    <div class="tpl-toolbar">
      <div class="left">
        <input
          class="search-input"
          type="text"
          [(ngModel)]="searchTerm"
          placeholder="Search by customer, order ID, or status…" />
      </div>
      <div class="right">
        <button class="clear-btn" type="button" (click)="searchTerm=''">Clear</button>
      </div>
    </div>

    <div class="report-toolbar">
      <div class="left">
        <label>
          From:
          <input type="date" [(ngModel)]="fromDate" (change)="applyFilter()" />
        </label>
        <label style="margin-left:12px;">
          To:
          <input type="date" [(ngModel)]="toDate" (change)="applyFilter()" />
        </label>
        <button class="brand-btn" style="margin-left:12px; background:#666;" (click)="clearFilter()">Clear Filter</button>
      </div>
      <div class="right">
        <button class="brand-btn" (click)="exportPdf()">Export PDF</button>
        <button class="brand-btn" style="margin-left:8px;" (click)="exportJson()">Export JSON</button>
      </div>
    </div>

    <div *ngIf="exportedDate" class="report-meta">
      <b>Report exported at:</b> {{ exportedDate }}
    </div>

    <ng-container *ngFor="let cust of fullyFilteredByCust">
      <div style="font-weight:700; margin:18px 0 4px 0; color:#d4145a;">{{ cust.customerName }}</div>
      <div style="overflow-x:auto;">
        <table class="neat-table">
          <thead>
            <tr>
              <th class="report-th">OrderID</th>
              <th class="report-th">Date</th>
              <th class="report-th">Status</th>
              <th class="report-th" style="text-align:right;">Total</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let o of cust.filteredOrders">
              <td class="report-td">{{ o.orderID }}</td>
              <td class="report-td">{{ o.orderDate | date:'mediumDate' }}</td>
              <td class="report-td">{{ statusSvc.getEffectiveStatus(o) }}</td>
              <td class="report-td" style="text-align:right;">R{{ o.totalPrice | number:'1.2-2' }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr>
              <td class="report-td report-total" colspan="3">Subtotal</td>
              <td class="report-td report-total" style="text-align:right;">
                R{{ getSubtotal(cust.filteredOrders) | number:'1.2-2' }}
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </ng-container>

    <div style="margin: 18px 0 0 0; font-weight:900; font-size:1.15rem; color:#d4145a; text-align:right;">
      Grand Total: R{{ getGrandTotal() | number:'1.2-2' }}
    </div>
  `,
  styles: [`
    .neat-table { width:100%;border-collapse:separate;border-spacing:0;min-width:540px;}
    .report-th { background:#FF5BAA; color:#fff; font-weight:700; padding:11px 18px; text-align:left; border-bottom:2.5px solid #F6A9D5; font-size:1.06rem; }
    .report-td { padding:10px 18px; font-size:1.01rem; border-bottom:1px solid #FFDFEF; vertical-align: middle; background:#fff; }
    .report-total { background:#FFDFEF; font-weight:800; font-size:1.08rem; color:#222; border-top:2.5px solid #F6A9D5; }
    tbody tr:last-child .report-td { border-bottom:none; }

    .report-toolbar { display:flex; align-items:center; justify-content:space-between; margin:12px 0 8px 0; flex-wrap:wrap; gap:8px; }
    .brand-btn { background:#FF5BAA; color:#fff; border:none; padding:8px 14px; border-radius:12px; font-weight:700; cursor:pointer; box-shadow:0 1px 3px rgba(0,0,0,.08); }
    .brand-btn:hover { filter:brightness(0.95); }
    .report-meta { color:#666; margin:4px 0 12px 0; font-size:.95rem; }
    .report-meta b { color:#333; }
    input[type="date"] { padding:6px 10px; border:1px solid #ddd; border-radius:8px; }

    /* ---- PINK SEARCH TOOLBAR ---- */
    .tpl-toolbar { display:flex; gap:.75rem; align-items:center; justify-content:space-between; margin:0 0 1rem 0; }
    .tpl-toolbar .left{ flex:1 1 auto; }
    .tpl-toolbar .right{ flex:0 0 auto; }
    .tpl-toolbar .search-input{ width:100%; padding:.9rem 1.1rem; border-radius:14px; border:1.6px solid #ffe4f1; background:#fef6fb; outline:none; font-size:1rem; box-shadow:0 2px 8px #e91e6314 inset; }
    .tpl-toolbar .search-input::placeholder{ color:#9a8b96; }
    .tpl-toolbar .search-input:focus{ border-color:#e91e63; background:#fff; box-shadow:0 0 0 3px #ffe4f1; }
    .tpl-toolbar .clear-btn{ border:none; border-radius:12px; padding:.65rem 1.0rem; background:#f6edf4; color:#222; font-weight:800; box-shadow:0 2px 6px rgba(233,30,99,.12); cursor:pointer; transition:background-color .18s; }
    .tpl-toolbar .clear-btn:hover{ background:#f2d9e6; }
  `]
})
export class OrdersByCustomerReportComponent {
  @Input() byCust: Customer[] = [];
  @Input() userName: string = '';
  @Input() date: string = '';
  exportedDate = '';

  fromDate: string = '';
  toDate: string = '';
  filteredByCust: Customer[] = [];
  searchTerm = '';

  constructor(public statusSvc: SharedStatusService) {}

  ngOnInit() { this.applyFilter(); }

  applyFilter() {
    const from = this.fromDate ? new Date(this.fromDate) : null;
    const to = this.toDate ? new Date(this.toDate) : null;

    this.filteredByCust = (this.byCust || []).map(cust => {
      const filteredOrders = (cust.orders || []).filter((o: Order) => {
        const d = new Date(o.orderDate);
        return (!from || d >= from) && (!to || d <= to);
      });
      return { ...cust, filteredOrders };
    }).filter(cust => cust.filteredOrders!.length > 0);
  }

  clearFilter() {
    this.fromDate = '';
    this.toDate = '';
    this.filteredByCust = this.byCust.map(cust => ({
      ...cust,
      filteredOrders: cust.orders
    }));
  }

  /** search helper */
  get fullyFilteredByCust(): Customer[] {
    const q = this.searchTerm.trim().toLowerCase();
    if (!q) return this.filteredByCust;

    return this.filteredByCust
      .map(cust => {
        const filteredOrders = (cust.filteredOrders || []).filter(o =>
          (cust.customerName || '').toLowerCase().includes(q) ||
          String(o.orderID).includes(q) ||
          (this.statusSvc.getEffectiveStatus(o) || '').toLowerCase().includes(q)
        );
        return { ...cust, filteredOrders };
      })
      .filter(cust => cust.filteredOrders!.length > 0);
  }

  getSubtotal(orders: Order[] = []): number {
    return (orders || []).reduce((sum, o) => sum + (o.totalPrice || 0), 0);
  }

  getGrandTotal(): number {
    return (this.fullyFilteredByCust || [])
      .reduce((grand, cust) => grand + this.getSubtotal(cust.filteredOrders), 0);
  }

  exportPdf() {
    const doc = new jsPDF();
    addBranding(doc, 'Orders by Customer Report', this.date, this.userName);

    if (this.searchTerm) {
      doc.setFontSize(11);
    }

    (this.fullyFilteredByCust || []).forEach((cust, idx) => {
      if (idx !== 0) doc.addPage();
      doc.setFontSize(14);
      doc.text(cust.customerName, 15, 75);

      autoTable(doc, {
        head: [['OrderID','Date','Status','Total']],
        body: (cust.filteredOrders || []).map((o: Order) => [
          o.orderID,
          new Date(o.orderDate).toLocaleDateString(),
          this.statusSvc.getEffectiveStatus(o),
          `R${o.totalPrice.toLocaleString(undefined,{ minimumFractionDigits:2, maximumFractionDigits:2 })}`
        ]),
        foot: [[
          { content: 'Subtotal', colSpan: 3, styles: { fontStyle:'bold', fillColor:[255,223,239] } },
          { content: `R${this.getSubtotal(cust.filteredOrders).toLocaleString(undefined,{ minimumFractionDigits:2, maximumFractionDigits:2 })}`, styles: { fontStyle:'bold', fillColor:[255,91,170], textColor:[255,255,255] } }
        ]],
        startY: 80,
        theme: 'striped',
        headStyles: { fillColor:[255,91,170], textColor:[255,255,255], fontStyle:'bold' },
        footStyles: { fillColor:[255,91,170], textColor:[255,255,255], fontStyle:'bold' }
      });
    });

    doc.save(`OrdersByCustomer_Report_${new Date().toISOString()}.pdf`);
    this.exportedDate = new Date().toLocaleString();
  }

  async exportJson() {
    const payload = this.fullyFilteredByCust.map(c => ({
      customerName: c.customerName,
      orders: (c.filteredOrders || []).map(o => ({
        orderID: o.orderID,
        orderDate: this.toDateOnly(o.orderDate),
        status: this.statusSvc.getEffectiveStatus(o),
        totalPrice: Number(o.totalPrice.toFixed(2))
      })),
      subtotal: Number(this.getSubtotal(c.filteredOrders).toFixed(2))
    }));

    const grandTotal = Number(this.getGrandTotal().toFixed(2));
    const finalPayload = {
      generatedAt: new Date().toISOString(),
      searchQuery: this.searchTerm || null,
      dateRange: { from: this.fromDate || null, to: this.toDate || null },
      byCustomer: payload,
      grandTotal
    };

    const blob = new Blob([JSON.stringify(finalPayload, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = `OrdersByCustomer_${new Date().toISOString()}.json`;
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
    this.exportedDate = new Date().toLocaleString();
  }

  private toDateOnly(d: string | Date): string {
    const dt = (d instanceof Date) ? d : new Date(d);
    const y = dt.getFullYear();
    const m = String(dt.getMonth() + 1).padStart(2, '0');
    const day = String(dt.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
