import { Component, Input, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { ReportBrandingHeaderComponent } from './report-branding-header.component';
import { jsPDF, autoTable, addBranding } from './report-utils';

@Component({
  selector: 'app-stock-forecast-report',
  standalone: true,
  template: `
    <div>
      <app-report-branding-header [date]="date" [userName]="userName"></app-report-branding-header>
      <h2 style="font-weight:bold; margin-top:18px;">Stock Forecast Report</h2>

      <div class="report-toolbar">
        <div class="left"></div>
        <div class="right">
          <button class="brand-btn" (click)="exportPdf()">Export PDF</button>
        </div>
      </div>

      <div *ngIf="exportedDate" class="report-meta">
        <b>Report exported at:</b> {{ exportedDate }}
      </div>

      <div style="margin-bottom:18px;">
        <canvas id="forecastChart" height="220" style="width:100%;max-width:1000px;"></canvas>
      </div>

      <div style="overflow-x:auto;">
        <table class="neat-table">
          <thead>
            <tr>
              <th class="report-th">Product</th>
              <th class="report-th" style="text-align:right;">Avg Daily Qty</th>
              <th class="report-th" style="text-align:right;">Predicted (7 Days)</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of stockForecast || []">
              <td class="report-td">{{ item.productName }}</td>
              <td class="report-td" style="text-align:right;">{{ item.avgDailyQty | number:'1.0-0' }}</td>
              <td class="report-td" style="text-align:right;">{{ item.predicted7DayQty | number:'1.0-0' }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr>
              <td class="report-td report-total">Totals</td>
              <td class="report-td report-total" style="text-align:right;">{{ getTotalAvg() | number:'1.0-0' }}</td>
              <td class="report-td report-total" style="text-align:right;">{{ getTotalPredicted() | number:'1.0-0' }}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  `,
  styles: [`
    .neat-table { width:100%; border-collapse:separate; border-spacing:0; min-width:540px;}
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
export class StockForecastReportComponent implements AfterViewInit, OnDestroy {
  @Input() stockForecast: any[] = [];
  @Input() date: string = '';
  @Input() userName: string = '';
  chart: any = null;
  exportedDate = '';

  ngAfterViewInit() { this.renderChart(); }
  ngOnDestroy() { if (this.chart) { this.chart.destroy(); } }

  renderChart() {
    import('chart.js/auto').then((Chart: any) => {
      if (this.chart) { this.chart.destroy(); }
      this.chart = new Chart.default('forecastChart', {
        type: 'line',
        data: {
          labels: (this.stockForecast || []).map(x => x.productName),
          datasets: [
            {
              label: 'Avg Daily Qty',
              data: (this.stockForecast || []).map(x => x.avgDailyQty),
              borderColor: '#FF5BAA',
              backgroundColor: 'rgba(255,91,170,0.15)',
              fill: false,
              pointBackgroundColor: '#FF5BAA',
              tension: 0.45
            },
            {
              label: 'Predicted (7 Days)',
              data: (this.stockForecast || []).map(x => x.predicted7DayQty),
              borderColor: '#F6A9D5',
              backgroundColor: 'rgba(246,169,213,0.15)',
              fill: false,
              pointBackgroundColor: '#F6A9D5',
              tension: 0.45
            }
          ]
        },
        options: { responsive: true, plugins: { legend: { position: 'top' }, title: { display: false } }, scales: { y: { beginAtZero: true } } }
      });
    });
  }

  getTotalAvg(): number {
    return (this.stockForecast || []).reduce((sum, item) => sum + (Number(item.avgDailyQty) || 0), 0);
  }
  getTotalPredicted(): number {
    return (this.stockForecast || []).reduce((sum, item) => sum + (Number(item.predicted7DayQty) || 0), 0);
  }

  exportPdf() {
    const doc = new jsPDF();
    addBranding(doc, 'Stock Forecast Report', this.date, this.userName);

    autoTable(doc, {
      head: [['Product', 'Avg Daily Qty', 'Predicted (7 Days)']],
      body: (this.stockForecast || []).map(item => [
        item.productName,
        item.avgDailyQty,
        item.predicted7DayQty
      ]),
      foot: [[
        { content: 'Totals', styles: { fontStyle: 'bold', fillColor: [255, 223, 239], textColor: [255,91,170] } },
        { content: this.getTotalAvg(), styles: { fontStyle: 'bold', fillColor: [255, 91, 170], textColor: [255,255,255] } },
        { content: this.getTotalPredicted(), styles: { fontStyle: 'bold', fillColor: [255, 91, 170], textColor: [255,255,255] } }
      ]],
      startY: 58,
      theme: 'striped',
      headStyles: { fillColor: [255, 91, 170], textColor: [255,255,255], fontStyle: 'bold' },
      footStyles: { fillColor: [255, 91, 170], textColor: [255,255,255], fontStyle: 'bold' },
      styles: { font: 'helvetica', fontSize: 11 }
    });

    const chartCanvas = document.getElementById('forecastChart') as HTMLCanvasElement;
    if (chartCanvas) {
      const chartImg = chartCanvas.toDataURL('image/png', 1.0);
      // @ts-ignore
      doc.addImage(chartImg, 'PNG', 15, doc.lastAutoTable.finalY + 10, 160, 60);
    }
    doc.save(`Stock_Forecast_Report_${new Date().toISOString()}.pdf`);
    this.exportedDate = new Date().toLocaleString();
  }
}
