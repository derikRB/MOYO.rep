import { Component, Input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportBrandingHeaderComponent } from './report-branding-header.component';
import { jsPDF, autoTable, addBranding } from './report-utils';
import { SharedStatusService } from '../services/shared-status.service';

type OrderRow = {
  orderID: number | string;
  orderDate: string | Date;
  orderStatusName: string;
  deliveryStatus?: string;
  totalPrice: number;
};

@Component({
  selector: 'app-orders-list-report',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, ReportBrandingHeaderComponent],
  template: `
    <div>
      <app-report-branding-header [date]="date" [userName]="userName"></app-report-branding-header>
      <h2 style="font-weight:bold; margin-top:18px;">Orders Report</h2>

      <!-- Pink search toolbar -->
      <div class="tpl-toolbar">
        <div class="left">
          <input
            class="search-input"
            type="text"
            [(ngModel)]="searchTerm"
            placeholder="Search by order ID or status…" />
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
        </div>
      </div>

      <div *ngIf="exportedDate" class="report-meta">
        <b>Report exported at:</b> {{ exportedDate }}
      </div>

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
            <tr *ngFor="let o of fullyFilteredOrders">
              <td class="report-td">{{ o.orderID }}</td>
              <td class="report-td">{{ o.orderDate | date:'shortDate' }}</td>
              <td class="report-td">{{ statusSvc.getEffectiveStatus(o) }}</td>
              <td class="report-td" style="text-align:right;">R{{ o.totalPrice | number:'1.2-2' }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr>
              <td class="report-td report-total" colspan="3">Grand Total</td>
              <td class="report-td report-total" style="text-align:right;">R{{ getGrandTotal() | number:'1.2-2' }}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  `,
  styles: [`
    .neat-table { width:100%; border-collapse:separate; border-spacing:0; min-width:540px; }
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
export class OrdersListReportComponent {
  @Input() orders: OrderRow[] = [];
  @Input() date: string = '';
  @Input() userName: string = '';
  exportedDate = '';

  fromDate: string = '';
  toDate: string = '';
  filteredOrders: OrderRow[] = [];
  searchTerm = '';

  constructor(public statusSvc: SharedStatusService) {}

  ngOnInit() { this.clearFilter(); }

  applyFilter() {
    const from = this.fromDate ? new Date(this.fromDate) : null;
    const to = this.toDate ? new Date(this.toDate) : null;

    this.filteredOrders = (this.orders || []).filter(o => {
      const d = new Date(o.orderDate);
      return (!from || d >= from) && (!to || d <= to);
    });
  }

  clearFilter() {
    this.fromDate = '';
    this.toDate = '';
    this.filteredOrders = [...this.orders];
  }

  /** search helper */
  get fullyFilteredOrders(): OrderRow[] {
    const q = this.searchTerm.trim().toLowerCase();
    if (!q) return this.filteredOrders;

    return this.filteredOrders.filter(o =>
      String(o.orderID).includes(q) ||
      (this.statusSvc.getEffectiveStatus(o) || '').toLowerCase().includes(q)
    );
  }

  getGrandTotal(): number {
    return (this.fullyFilteredOrders || []).reduce((sum, o) => sum + (o.totalPrice || 0), 0);
  }

  exportPdf() {
    const doc = new jsPDF();
    addBranding(doc, 'Orders Report', this.date, this.userName);

    if (this.searchTerm) {
      doc.setFontSize(11);
    }

    autoTable(doc, {
      head: [['OrderID','Date','Status','Total']],
      body: (this.fullyFilteredOrders || []).map(o => [
        o.orderID,
        new Date(o.orderDate).toLocaleDateString(),
        this.statusSvc.getEffectiveStatus(o),
        `R ${o.totalPrice.toFixed(2)}`
      ]),
      foot: [[
        { content:'Grand Total', colSpan:3, styles:{ fontStyle:'bold', fillColor:[255,223,239] } },
        { content:`R ${this.getGrandTotal().toFixed(2)}`, styles:{ fontStyle:'bold', fillColor:[255,91,170], textColor:[255,255,255] } }
      ]],
      startY: this.searchTerm ? 65 : 58,
      theme: 'striped',
      headStyles:{ fillColor:[255,91,170], textColor:[255,255,255], fontStyle:'bold' },
      footStyles:{ fillColor:[255,91,170], textColor:[255,255,255], fontStyle:'bold' }
    });

    doc.save(`Orders_Report_${new Date().toISOString()}.pdf`);
    this.exportedDate = new Date().toLocaleString();
  }
}
