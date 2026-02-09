// src/app/admin-reports/management-report.component.ts
import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

import { addBranding } from './report-utils';
import { ReportBrandingHeaderComponent } from './report-branding-header.component';
import { AdminReportsService, PeriodNode } from '../services/admin-reports.service';

@Component({
  selector: 'app-management-report',
  standalone: true,
  imports: [ CommonModule, FormsModule, ReportBrandingHeaderComponent ],
  template: `
    <app-report-branding-header [date]="date" [userName]="userName"></app-report-branding-header>

    <h2 style="font-weight:bold; margin-top:18px;">Management Report</h2>

    <div class="report-toolbar">
      <div class="left" style="display:flex; gap:8px; align-items:center;">
        <!-- Guard once, then use '.' access (no optional chaining) -->
        <ng-container *ngIf="tree as t">
          <select [(ngModel)]="selectedPeriod" (change)="onPeriodChange()" class="brand-select">
            <option [value]="t.period">{{ t.period }}</option>
            <optgroup *ngFor="let m of t.children" [label]="m.period">
              <option [value]="m.period">&nbsp;&nbsp;{{ m.period }}</option>
              <option *ngFor="let w of m.children" [value]="w.period">&nbsp;&nbsp;&nbsp;{{ w.period }}</option>
            </optgroup>
          </select>
          <span class="chip" *ngIf="selectedPeriod">{{ selectedPeriod }}</span>
        </ng-container>
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
            <th class="report-th">Period</th>
            <th class="report-th" style="text-align:right">Total Revenue</th>
            <th class="report-th" style="text-align:right">Net Revenue</th>
            <th class="report-th" style="text-align:right">VAT</th>
          </tr>
        </thead>
        <tbody *ngIf="selectedNode">
          <tr>
            <td class="report-td">{{ selectedNode.period }}</td>
            <td class="report-td" style="text-align:right;">R{{ selectedNode.totalRevenue | number:'1.2-2' }}</td>
            <td class="report-td" style="text-align:right;">R{{ selectedNode.netRevenue    | number:'1.2-2' }}</td>
            <td class="report-td" style="text-align:right;">R{{ selectedNode.totalVAT       | number:'1.2-2' }}</td>
          </tr>
        </tbody>
        <tfoot *ngIf="selectedNode">
          <tr>
            <td class="report-td report-total">Total</td>
            <td class="report-td report-total" style="text-align:right;">R{{ selectedNode.totalRevenue | number:'1.2-2' }}</td>
            <td class="report-td report-total" style="text-align:right;">R{{ selectedNode.netRevenue    | number:'1.2-2' }}</td>
            <td class="report-td report-total" style="text-align:right;">R{{ selectedNode.totalVAT       | number:'1.2-2' }}</td>
          </tr>
        </tfoot>
      </table>
    </div>

    <canvas id="mgmtChart" width="600" height="250" style="margin:18px 0;"></canvas>
  `,
  styles: [`
    .neat-table { width:100%; border-collapse:separate; border-spacing:0; min-width:540px; }
    .report-th { background:#FF5BAA; color:#fff; font-weight:700; padding:11px 18px; text-align:left; border-bottom:2.5px solid #F6A9D5; font-size:1.06rem; }
    .report-td { padding:10px 18px; font-size:1.01rem; border-bottom:1px solid #FFDFEF; vertical-align: middle; background:#fff; }
    .report-total { background:#FFDFEF; font-weight:800; font-size:1.08rem; color:#222; border-top:2.5px solid #F6A9D5; }
    tbody tr:last-child .report-td { border-bottom:none; }

    .report-toolbar { display:flex; align-items:center; justify-content:space-between; margin:12px 0 8px 0; }
    .brand-btn { background:#FF5BAA; color:#fff; border:none; padding:8px 14px; border-radius:12px; font-weight:700; cursor:pointer; box-shadow:0 1px 3px rgba(0,0,0,.08); }
    .brand-btn:hover { filter:brightness(0.95); }

    .chip { background:#FFE3F1; color:#d4145a; padding:6px 10px; border-radius:14px; font-weight:700; border:1px solid #F6A9D5; }
    .brand-select { padding:8px 12px; border-radius:12px; border:1px solid #F6A9D5; outline:none; }
    .brand-select:focus { box-shadow:0 0 0 3px rgba(246,169,213,.35); }

    .report-meta { color:#666; margin:4px 0 12px 0; font-size:.95rem; }
    .report-meta b { color:#333; }
  `]
})
export class ManagementReportComponent implements OnInit {
  @Input() date:     string = '';
  @Input() userName: string = '';

  exportedDate = '';
  tree!: PeriodNode;             // keep definite-assign; guarded in template
  flat: PeriodNode[] = [];
  selectedPeriod = '';
  selectedNode!: PeriodNode;
  chart: any;

  constructor(private svc: AdminReportsService) {}

  ngOnInit() {
    this.svc.getFinancialHierarchy().subscribe(root => {
      this.tree = root;
      this.flat = this.flatten(root);
      this.selectedPeriod = root.period;
      this.onPeriodChange();
    });
  }

  private flatten(root: PeriodNode): PeriodNode[] {
    const out: PeriodNode[] = [root];
    root.children.forEach(m => {
      out.push(m);
      m.children.forEach(w => out.push(w));
    });
    return out;
  }

  onPeriodChange() {
    this.selectedNode = this.flat.find(n => n.period === this.selectedPeriod)!;
    this.renderChart();
  }

  renderChart() {
    if (!this.selectedNode) return;

    const dataNodes = [this.selectedNode, ...this.selectedNode.children];
    const labels = dataNodes.map(n => n.period);
    const totals = dataNodes.map(n => n.totalRevenue);
    const nets   = dataNodes.map(n => n.netRevenue);

    import('chart.js/auto').then(Chart => {
      if (this.chart) this.chart.destroy();
      this.chart = new Chart.default('mgmtChart', {
        type: 'bar',
        data: {
          labels,
          datasets: [
            { label: 'Total Revenue', data: totals, backgroundColor: 'rgba(135,206,250,0.6)' },
            { label: 'Net Revenue',   data: nets,   backgroundColor: 'rgba(255,182,193,0.6)' }
          ]
        },
        options: {
          responsive: false,
          plugins: {
            legend: { position: 'top' },
            tooltip: { callbacks: { label(ctx) {
              const v = ctx.parsed.y;
              return ctx.dataset.label + ': R' + v.toLocaleString(undefined,{ minimumFractionDigits:2, maximumFractionDigits:2 });
            }}}
          },
          scales: {
            y: { ticks: { callback(value) {
              return 'R' + Number(value).toLocaleString(undefined,{ minimumFractionDigits:2, maximumFractionDigits:2 });
            }}}
          }
        }
      });
    });
  }

  exportPdf() {
    if (!this.selectedNode) return;
    const doc = new jsPDF();
    addBranding(doc, 'Management Report', this.date, this.userName);

    autoTable(doc, {
      head: [['Period','Total','Net','VAT']],
      body: [[
        this.selectedNode.period,
        `R${this.selectedNode.totalRevenue.toLocaleString(undefined,{ minimumFractionDigits:2, maximumFractionDigits:2 })}`,
        `R${this.selectedNode.netRevenue.toLocaleString(undefined,{ minimumFractionDigits:2, maximumFractionDigits:2 })}`,
        `R${this.selectedNode.totalVAT.toLocaleString(undefined,{ minimumFractionDigits:2, maximumFractionDigits:2 })}`
      ]],
      startY: 58,
      theme: 'striped',
      headStyles: { fillColor: [255, 91, 170], textColor: [255,255,255], fontStyle: 'bold' },
      footStyles: { fillColor: [255, 91, 170], textColor: [255,255,255], fontStyle: 'bold' }
    });

    const canvas = document.getElementById('mgmtChart') as HTMLCanvasElement;
    if (canvas) {
      const img = canvas.toDataURL('image/png', 1.0);
      doc.addImage(img, 'PNG', 15, 130, 160, 60);
    }

    doc.save(`Management_Report_${new Date().toISOString()}.pdf`);
    this.exportedDate = new Date().toLocaleString();
  }
}
