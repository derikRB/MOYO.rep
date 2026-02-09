import { Component, Input, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportBrandingHeaderComponent } from './report-branding-header.component';
import { jsPDF, addBranding } from './report-utils';

@Component({
  selector: 'app-dashboard-report',
  standalone: true,
  imports: [CommonModule, ReportBrandingHeaderComponent],
  template: `
    <div>
      <app-report-branding-header [date]="date" [userName]="userName"></app-report-branding-header>
      <h2>Sales Dashboard</h2>

      <div class="report-toolbar">
        <div class="left"></div>
        <div class="right">
          <button class="brand-btn" (click)="exportPdf()">Export PDF</button>
        </div>
      </div>

      <div *ngIf="exportedDate" class="report-meta">
        <b>Report exported at:</b> {{ exportedDate }}
      </div>

      <canvas id="dashboardChart" width="600" height="250"></canvas>
    </div>
  `,
  styles: [`
    .report-toolbar { display:flex; align-items:center; justify-content:space-between; margin:12px 0 8px 0; }
    .brand-btn { background:#FF5BAA; color:#fff; border:none; padding:8px 14px; border-radius:12px; font-weight:700; cursor:pointer; box-shadow:0 1px 3px rgba(0,0,0,.08); }
    .brand-btn:hover { filter:brightness(0.95); }
    .report-meta { color:#666; margin:4px 0 12px 0; font-size:.95rem; }
    .report-meta b { color:#333; }
  `]
})
export class DashboardReportComponent implements AfterViewInit {
  @Input() sales: any[] = [];
  @Input() date: string = '';
  @Input() userName: string = '';
  exportedDate: string = '';
  @Input() financial: any = null;

  ngAfterViewInit() {
    import('chart.js/auto').then((Chart: any) => {
      new Chart.default('dashboardChart', {
        type: 'line',
        data: {
          labels: this.sales.map((d: any) => d.orderDate),
          datasets: [{ label: 'Sales', data: this.sales.map((d: any) => d.revenue), borderColor: '#FF5BAA', backgroundColor: 'rgba(255,91,170,0.15)' }]
        },
        options: { responsive: false }
      });
    });
  }

  exportPdf() {
    const doc = new jsPDF();
    addBranding(doc, 'Sales Dashboard', this.date, this.userName);
    const chartCanvas = document.getElementById('dashboardChart') as HTMLCanvasElement;
    if (chartCanvas) {
      const chartImg = chartCanvas.toDataURL('image/png', 1.0);
      doc.addImage(chartImg, 'PNG', 15, 45, 160, 60);
    }
    doc.save(`DashboardReport_${new Date().toISOString()}.pdf`);
    this.exportedDate = new Date().toLocaleString();
  }
}
