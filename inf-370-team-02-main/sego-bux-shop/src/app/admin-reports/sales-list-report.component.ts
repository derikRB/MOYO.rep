import { Component, Input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportBrandingHeaderComponent } from './report-branding-header.component';
import { jsPDF, autoTable, addBranding } from './report-utils';

type SalesRow = { orderDate: string | Date; orders: number | string; revenue: number | string };

@Component({
  selector: 'app-sales-list-report',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, ReportBrandingHeaderComponent],
  template: `
    <app-report-branding-header [date]="date" [userName]="userName"></app-report-branding-header>

    <h2 style="font-weight:bold; margin-top:18px;">Sales Report</h2>

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
        <!-- <button class="brand-btn" style="margin-left:12px;" (click)="applyFilter()">Apply Filter</button> -->
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

    <div style="overflow-x:auto;">
      <table class="neat-table">
        <thead>
          <tr>
            <th class="report-th">Date</th>
            <th class="report-th" style="text-align:right;">Orders</th>
            <th class="report-th" style="text-align:right;">Revenue</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let s of filteredSales">
            <td class="report-td">{{ s.orderDate | date:'mediumDate' }}</td>
            <td class="report-td" style="text-align:right;">{{ s.orders | number }}</td>
            <td class="report-td" style="text-align:right;">R{{ s.revenue | number:'1.2-2' }}</td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <td class="report-td report-total">Grand Total</td>
            <td class="report-td report-total" style="text-align:right;">{{ getTotalOrders() | number }}</td>
            <td class="report-td report-total" style="text-align:right;">R{{ getTotalRevenue() | number:'1.2-2' }}</td>
          </tr>
        </tfoot>
      </table>
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
  `]
})
export class SalesListReportComponent {
  @Input() sales: SalesRow[] = [];
  @Input() date: string = '';
  @Input() userName: string = '';

  exportedDate = '';
  fromDate: string = '';
  toDate: string = '';
  filteredSales: SalesRow[] = [];

  ngOnInit() {
    this.clearFilter(); // Show all sales initially
  }

  applyFilter() {
    const from = this.fromDate ? new Date(this.fromDate) : null;
    const to = this.toDate ? new Date(this.toDate) : null;

    this.filteredSales = (this.sales || []).filter(s => {
      const d = new Date(s.orderDate);
      return (!from || d >= from) && (!to || d <= to);
    });
  }

  /** Clears filter and shows all sales */
  clearFilter() {
    this.fromDate = '';
    this.toDate = '';
    this.filteredSales = [...this.sales];
  }

  getTotalOrders(): number {
    return (this.filteredSales || []).reduce((sum, s) => sum + this.toNumber(s.orders), 0);
  }

  getTotalRevenue(): number {
    return (this.filteredSales || []).reduce((sum, s) => sum + this.toNumber(s.revenue), 0);
  }

  exportPdf() {
    const doc = new jsPDF();
    addBranding(doc, 'Sales Report', this.date, this.userName);
    autoTable(doc, {
      head: [['Date','Orders','Revenue']],
      body: (this.filteredSales || []).map(s => [
        this.toMediumDate(s.orderDate),
        this.toNumber(s.orders),
        `R${this.toNumber(s.revenue).toLocaleString(undefined,{ minimumFractionDigits:2, maximumFractionDigits:2 })}`
      ]),
      foot: [[
        'Grand Total',
        this.getTotalOrders(),
        `R${this.getTotalRevenue().toLocaleString(undefined,{ minimumFractionDigits:2, maximumFractionDigits:2 })}`
      ]],
      startY: 58,
      theme: 'striped',
      headStyles: { fillColor:[255,91,170], textColor:[255,255,255], fontStyle:'bold' },
      footStyles: { fillColor:[255,91,170], textColor:[255,255,255], fontStyle:'bold' }
    });
    doc.save(`Sales_Report_${new Date().toISOString()}.pdf`);
    this.exportedDate = new Date().toLocaleString();
  }

  async exportJson() {
    const todayISO = this.toDateOnly(new Date());
    const byDay = (this.filteredSales || []).map(s => ({
      date: this.toDateOnly(s.orderDate),
      orders: this.toNumber(s.orders),
      revenue: Number(this.toNumber(s.revenue).toFixed(2))
    }));

    const ordersCount = byDay.reduce((a, x) => a + x.orders, 0);
    const grossSales  = Number(byDay.reduce((a, x) => a + x.revenue, 0).toFixed(2));

    const payloadWithoutSignature = {
      exportVersion: '1.0',
      generatedAt: new Date().toISOString(),
      store: { name: 'By Sego and Bux', timezone: Intl.DateTimeFormat().resolvedOptions().timeZone, currency: 'ZAR' },
      period: { label: (this.date && this.date.trim().length) ? this.date : todayISO },
      totals: { days: byDay.length, ordersCount, grossSales },
      byDay
    };

    const canonical = JSON.stringify(payloadWithoutSignature);
    const checksumHex = await this.sha256Hex(canonical);

    const finalPayload = { ...payloadWithoutSignature, signing: { algorithm: 'sha256', checksum: checksumHex } };

    this.downloadJson(finalPayload, `daily-sales-${todayISO}.json`);
    this.exportedDate = new Date().toLocaleString();
  }

  private toNumber(v: any): number {
    if (typeof v === 'number') return Number.isFinite(v) ? v : 0;
    if (typeof v === 'string') {
      const n = Number(v.replace(/[^\d.-]/g, ''));
      return Number.isFinite(n) ? n : 0;
    }
    return 0;
  }

  private toMediumDate(d: any): string {
    const dt = (d instanceof Date) ? d : new Date(d);
    return dt.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
  }

  private toDateOnly(d: any): string {
    if (!d) return '';
    if (typeof d === 'string' && /^\d{4}-\d{2}-\d{2}/.test(d)) return d.slice(0,10);
    const dt = (d instanceof Date) ? d : new Date(d);
    return `${dt.getFullYear()}-${String(dt.getMonth()+1).padStart(2,'0')}-${String(dt.getDate()).padStart(2,'0')}`;
  }

  private async sha256Hex(input: string): Promise<string> {
    const enc = new TextEncoder().encode(input);
    const digest = await crypto.subtle.digest('SHA-256', enc);
    return Array.from(new Uint8Array(digest)).map(b => b.toString(16).padStart(2,'0')).join('');
  }

  private downloadJson(obj: unknown, filename: string) {
    const blob = new Blob([JSON.stringify(obj, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename;
    document.body.appendChild(a); a.click();
    a.remove(); URL.revokeObjectURL(url);
  }
}
