import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportBrandingHeaderComponent } from './report-branding-header.component';
import { jsPDF, autoTable, addBranding } from './report-utils';

@Component({
  selector: 'app-custom-report',
  standalone: true,
  template: `
    <app-report-branding-header [date]="date" [userName]="userName"></app-report-branding-header>
    <h2 style="font-weight:bold; margin-top:18px;">Custom Sales Report</h2>

    <div class="report-toolbar">
      <div class="left" style="display:flex; gap:8px; align-items:center; flex-wrap:wrap;">
        <label class="filter-label">
          From: <input class="brand-select" type="date" [(ngModel)]="fromDate" (change)="applyDateFilter()" />
        </label>
        <label class="filter-label">
          To: <input class="brand-select" type="date" [(ngModel)]="toDate" (change)="applyDateFilter()" />
        </label>
        <!-- <button class="brand-btn secondary" (click)="applyDateFilter()"></button> -->
        <button class="brand-btn secondary" style="background:#666;" (click)="clearFilter()">Clear Filter</button>
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
    .brand-btn.secondary { background:#F6A9D5; color:#fff; }
    .brand-btn:hover { filter:brightness(0.95); }
    .brand-select { padding:6px 10px; border-radius:10px; border:1px solid #F6A9D5; outline:none; }
    .brand-select:focus { box-shadow:0 0 0 3px rgba(246,169,213,.35); }
    .filter-label { font-weight:600; color:#444; }

    .report-meta { color:#666; margin:4px 0 12px 0; font-size:.95rem; }
    .report-meta b { color:#333; }
  `],
  imports: [CommonModule, DatePipe, DecimalPipe, FormsModule, ReportBrandingHeaderComponent]
})
export class CustomReportComponent {
  @Input() sales: any[] = [];
  @Input() userName: string = '';
  @Input() date: string = '';
  exportedDate = '';

  fromDate: string = '';
  toDate: string = '';
  filteredSales: any[] = [];

  ngOnInit() {
    this.clearFilter(); // show all sales initially
  }

  /** Filter sales based on selected dates */
  applyDateFilter() {
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
    return (this.filteredSales || []).reduce((sum, b) => sum + (b.orders || 0), 0); 
  }

  getTotalRevenue(): number { 
    return (this.filteredSales || []).reduce((sum, b) => sum + (b.revenue || 0), 0); 
  }

  exportPdf() {
    const doc = new jsPDF();
    addBranding(doc, 'Custom Sales Report', this.date, this.userName);

    autoTable(doc, {
      head: [['Date', 'Orders', 'Revenue']],
      body: (this.filteredSales || []).map(s => [
        new Date(s.orderDate).toLocaleDateString(),
        s.orders,
        `R${s.revenue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
      ]),
      foot: [[
        'Grand Total',
        this.getTotalOrders(),
        `R${this.getTotalRevenue().toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
      ]],
      startY: 58,
      theme: 'striped',
      headStyles: { fillColor: [255, 91, 170], textColor: [255, 255, 255], fontStyle: 'bold' },
      footStyles: { fillColor: [255, 91, 170], textColor: [255, 255, 255], fontStyle: 'bold' }
    });

    doc.save(`Custom_Sales_Report_${new Date().toISOString()}.pdf`);
    this.exportedDate = new Date().toLocaleString();
  }
}
