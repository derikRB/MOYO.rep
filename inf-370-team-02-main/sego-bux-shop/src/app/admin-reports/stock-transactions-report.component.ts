import { Component, Input } from '@angular/core'; 
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportBrandingHeaderComponent } from './report-branding-header.component';
import { jsPDF, autoTable, addBranding } from './report-utils';

@Component({
  selector: 'app-stock-transactions-report',
  standalone: true,
  template: `
    <app-report-branding-header [date]="date" [userName]="userName"></app-report-branding-header>
    <h2 style="font-weight:bold; margin-top:18px;">Stock Transactions Report</h2>

    <div class="report-toolbar">
      <div class="left" style="display:flex; gap:8px; align-items:center; flex-wrap:wrap;">
        <label class="filter-label">
          From: <input class="brand-select" type="date" [(ngModel)]="fromDate" (change)="applyFilter()" />
        </label>
        <label class="filter-label">
          To: <input class="brand-select" type="date" [(ngModel)]="toDate" (change)="applyFilter()" />
        </label>
        <!-- <button class="brand-btn secondary" (click)="applyFilter()">Apply Filter</button> -->
        <button class="brand-btn secondary" style="background:#666;" (click)="clearFilter()">Clear Filter</button>
      </div>
      <div class="right">
        <button class="brand-btn" (click)="exportPdf()">Export PDF</button>
      </div>
    </div>

    <div *ngIf="exportedDate" class="report-meta">
      <b>Report exported at:</b> {{ exportedDate }}
    </div>

    <ng-container *ngFor="let txGroup of groupByProduct(filteredTx)">
      <div style="font-weight:700; margin:18px 0 4px 0; color:#d4145a;">{{ txGroup.productName }}</div>
      <div style="overflow-x:auto;">
        <table class="neat-table">
          <thead>
            <tr>
              <th class="report-th">TransactionID</th>
              <th class="report-th">Date</th>
              <th class="report-th" style="text-align:right;">Quantity</th>
              <th class="report-th">Type</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let t of txGroup.transactions">
              <td class="report-td">{{ t.transactionID }}</td>
              <td class="report-td">{{ t.date | date:'medium' }}</td>
              <td class="report-td" style="text-align:right;">{{ t.quantity }}</td>
              <td class="report-td">{{ t.type }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr>
              <td class="report-td report-total" colspan="2">Subtotal</td>
              <td class="report-td report-total" style="text-align:right;">{{ getSubtotal(txGroup.transactions) }}</td>
              <td class="report-td report-total"></td>
            </tr>
          </tfoot>
        </table>
      </div>
    </ng-container>

    <div style="margin: 18px 0 0 0; font-weight: 900; font-size: 1.15rem; color:#d4145a; text-align:right;">
      Grand Total: {{ getGrandTotal() | number:'1.2-2' }}
    </div>
  `,
  styles: [`
    .neat-table { width:100%;border-collapse:separate;border-spacing:0;min-width:540px;}
    .report-th { background:#FF5BAA; color:#fff; font-weight:700; padding:11px 18px; text-align:left; border-bottom:2.5px solid #F6A9D5; font-size:1.06rem;}
    .report-td {padding:10px 18px;font-size:1.01rem;border-bottom:1px solid #FFDFEF;vertical-align: middle;background:#fff;}
    .report-total {background:#FFDFEF;font-weight:800;font-size:1.08rem;color:#222;border-top:2.5px solid #F6A9D5;}
    tbody tr:last-child .report-td {border-bottom:none;}

    .report-toolbar { display:flex; align-items:center; justify-content:space-between; margin:12px 0 8px 0; flex-wrap:wrap; gap:8px; }
    .brand-btn { background:#FF5BAA; color:#fff; border:none; padding:8px 14px; border-radius:12px; font-weight:700; cursor:pointer; box-shadow:0 1px 3px rgba(0,0,0,.08);}
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
export class StockTransactionsReportComponent {
  @Input() stockTx: any[] = [];
  @Input() userName: string = '';
  @Input() date: string = '';
  fromDate: string = '';
  toDate: string = '';
  exportedDate = '';

  filteredTx: any[] = [];

  ngOnInit() {
    this.clearFilter(); // show all transactions initially
  }

  /** Filter transactions based on selected dates */
  applyFilter() {
    const from = this.fromDate ? new Date(this.fromDate) : null;
    const to = this.toDate ? new Date(this.toDate) : null;

    this.filteredTx = (this.stockTx || []).filter(t => {
      const d = new Date(t.date);
      return (!from || d >= from) && (!to || d <= to);
    });
  }

  /** Clear date filters and show all transactions */
  clearFilter() {
    this.fromDate = '';
    this.toDate = '';
    this.filteredTx = [...this.stockTx];
  }

  groupByProduct(txs: any[]): { productName: string, transactions: any[] }[] {
    const map: { [key: string]: any[] } = {};
    for (const t of txs) {
      if (!map[t.productName]) map[t.productName] = [];
      map[t.productName].push(t);
    }
    return Object.keys(map).map(name => ({
      productName: name,
      transactions: map[name]
    }));
  }

  getSubtotal(txs: any[]): number {
    return (txs || []).reduce((sum, t) => sum + (Number(t.quantity) || 0), 0);
  }

  getGrandTotal(): number {
    return (this.filteredTx || []).reduce((sum, t) => sum + (Number(t.quantity) || 0), 0);
  }

  exportPdf() {
    const doc = new jsPDF();
    addBranding(doc, 'Stock Transactions Report', this.date, this.userName);

    this.groupByProduct(this.filteredTx).forEach((group, idx) => {
      if (idx !== 0) doc.addPage();
      doc.setFontSize(14);
      doc.text(group.productName, 15, 65);
      autoTable(doc, {
        head: [['TransactionID', 'Date', 'Quantity', 'Type']],
        body: group.transactions.map(t => [
          t.transactionID,
          new Date(t.date).toLocaleString(),
          t.quantity,
          t.type
        ]),
        foot: [[
          { content: 'Subtotal', colSpan: 2, styles: { fontStyle: 'bold', fillColor: [255, 223, 239] } },
          { content: this.getSubtotal(group.transactions).toString(), styles: { fontStyle: 'bold', fillColor: [255, 91, 170], textColor: [255,255,255] } },
          { content: '' }
        ]],
        startY: 70,
        theme: 'striped',
        headStyles: { fillColor: [255, 91, 170], textColor: [255,255,255], fontStyle: 'bold' },
        footStyles: { fillColor: [255, 91, 170], textColor: [255,255,255], fontStyle: 'bold' }
      });
    });

    // Grand total page
    doc.addPage();
    doc.setFontSize(16);
    doc.text('Grand Total', 15, 65);
    autoTable(doc, {
      body: [[
        { content: 'Grand Total', colSpan: 2, styles: { fontStyle: 'bold', fillColor: [255, 223, 239] } },
        { content: this.getGrandTotal().toString(), styles: { fontStyle: 'bold', fillColor: [255, 91, 170], textColor: [255,255,255] } },
        { content: '' }
      ]],
      startY: 70,
      theme: 'striped',
      footStyles: { fillColor: [255, 91, 170], textColor: [255,255,255], fontStyle: 'bold' }
    });

    doc.save(`StockTransactions_Report_${new Date().toISOString()}.pdf`);
    this.exportedDate = new Date().toLocaleString();
  }
}
