import { Component, Input } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { ReportBrandingHeaderComponent } from './report-branding-header.component';
import { jsPDF, autoTable, addBranding } from './report-utils';

@Component({
  selector: 'app-stock-list-report',
  standalone: true,
  template: `
    <app-report-branding-header [date]="date" [userName]="userName"></app-report-branding-header>
    <h2 style="font-weight:bold; margin-top:18px;">Stock Report</h2>

    <div class="report-toolbar">
      <div class="left"></div>
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
            <th class="report-th">Category</th>
            <th class="report-th" style="text-align:right;">Total Stock</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let s of stock || []">
            <td class="report-td">{{ s.categoryName }}</td>
            <td class="report-td" style="text-align:right;">{{ s.totalStock | number }}</td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <td class="report-td report-total">Grand Total</td>
            <td class="report-td report-total" style="text-align:right;">{{ getTotalStock() | number }}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  `,
  styles: [`
    .neat-table { width:100%;border-collapse:separate;border-spacing:0;min-width:400px;}
    .report-th { background:#FF5BAA; color:#fff; font-weight:700; padding:11px 18px; text-align:left; border-bottom:2.5px solid #F6A9D5; font-size:1.06rem;}
    .report-td {padding:10px 18px;font-size:1.01rem;border-bottom:1px solid #FFDFEF;vertical-align: middle;background:#fff;}
    .report-total {background:#FFDFEF;font-weight:800;font-size:1.08rem;color:#222;border-top:2.5px solid #F6A9D5;}
    tbody tr:last-child .report-td {border-bottom:none;}

    .report-toolbar { display:flex; align-items:center; justify-content:space-between; margin:12px 0 8px 0; }
    .brand-btn { background:#FF5BAA; color:#fff; border:none; padding:8px 14px; border-radius:12px; font-weight:700; cursor:pointer; box-shadow:0 1px 3px rgba(0,0,0,.08);}
    .brand-btn:hover { filter:brightness(0.95); }
    .report-meta { color:#666; margin:4px 0 12px 0; font-size:.95rem; }
    .report-meta b { color:#333; }
  `],
  imports: [CommonModule, DecimalPipe, ReportBrandingHeaderComponent]
})
export class StockListReportComponent {
  @Input() stock: any[] = [];
  @Input() userName: string = '';
  @Input() date: string = '';
  exportedDate = '';

  getTotalStock(): number {
    return (this.stock || []).reduce((sum, b) => sum + (b.totalStock || 0), 0);
  }
  exportPdf() {
    const doc = new jsPDF();
    addBranding(doc, 'Stock Report', this.date, this.userName);
    autoTable(doc, {
      head: [['Category', 'Total Stock']],
      body: (this.stock || []).map(s => [ s.categoryName, s.totalStock ]),
      foot: [ ['Grand Total', this.getTotalStock()] ],
      startY: 58,
      theme: 'striped',
      headStyles: { fillColor: [255, 91, 170], textColor: [255,255,255], fontStyle: 'bold' },
      footStyles: { fillColor: [255, 91, 170], textColor: [255,255,255], fontStyle: 'bold' }
    });
    doc.save(`Stock_Report_${new Date().toISOString()}.pdf`);
    this.exportedDate = new Date().toLocaleString();
  }
}
