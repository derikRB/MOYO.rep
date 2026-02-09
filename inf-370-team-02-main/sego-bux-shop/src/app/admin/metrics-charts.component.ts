import { Component, OnDestroy, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { interval, Subject, takeUntil } from 'rxjs';
import Chart from 'chart.js/auto';
import { MetricsService } from '../services/metrics.service';
import { MetricsSignalRService } from '../services/metrics-signalr.service';
import { EventBusService } from '../services/event-bus.service';
import { SharedStatusService } from '../services/shared-status.service';
import { OrderService } from '../services/order.service'; // Add this import

@Component({
  selector: 'app-metrics-charts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './metrics-charts.component.html',
  styleUrls: ['./metrics-charts.component.scss']
})
export class MetricsChartsComponent implements OnInit, OnDestroy {
  @ViewChild('statusCanvas') statusCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('salesCanvas')  salesCanvas!:  ElementRef<HTMLCanvasElement>;
  @ViewChild('stockCanvas')  stockCanvas!:  ElementRef<HTMLCanvasElement>;

  private destroy$ = new Subject<void>();
  private statusChart?: Chart;
  private salesChart?: Chart;
  private stockChart?: Chart;

  statusRange: 'today'|'7d' = '7d';
  loading = false;

  constructor(
    private metrics: MetricsService,
    private orderService: OrderService, // Add this
    private bus: EventBusService,
    private hub: MetricsSignalRService,
    private statusService: SharedStatusService
  ) {}

  ngOnInit(): void {
    this.hub.connect();
    interval(30000).pipe(takeUntil(this.destroy$)).subscribe(() => this.refresh());
    this.bus.on('ordersChanged').pipe(takeUntil(this.destroy$)).subscribe(() => this.refresh());
    this.bus.on('salesChanged').pipe(takeUntil(this.destroy$)).subscribe(() => this.refresh());
    this.bus.on('inventoryChanged').pipe(takeUntil(this.destroy$)).subscribe(() => this.refresh());
    setTimeout(() => this.refresh(), 0);
  }

  refresh(): void {
    if (!this.statusCanvas || !this.salesCanvas || !this.stockCanvas) return;
    this.loading = true;

    // Use OrderService to get full order data instead of MetricsService
    this.orderService.getAllOrders().subscribe(orders => {
      // Filter orders based on date range
      const filteredOrders = this.filterOrdersByRange(orders, this.statusRange);
      
      // Process the data using the shared status service
      const statusCounts = this.calculateEffectiveStatusCounts(filteredOrders);
      this.drawStatus(Object.keys(statusCounts), Object.values(statusCounts));
    });

    this.metrics.getSalesLast30d().subscribe(data => {
      const s = this.metrics.toLine(data);
      this.drawSales(s.labels, s.totals);
    });

    this.metrics.getLowStock(10).subscribe(data => {
      const s = this.metrics.toBarLowStock(data);
      this.drawLowStock(s.labels, s.qty, s.thr);
      this.loading = false;
    });
  }

  /**
   * Filter orders based on date range
   */
  private filterOrdersByRange(orders: any[], range: 'today'|'7d'): any[] {
    const now = new Date();
    const startDate = new Date();
    
    if (range === 'today') {
      startDate.setHours(0, 0, 0, 0);
    } else {
      startDate.setDate(now.getDate() - 7);
      startDate.setHours(0, 0, 0, 0);
    }
    
    return orders.filter(order => {
      const orderDate = new Date(order.orderDate);
      return orderDate >= startDate && orderDate <= now;
    });
  }

  /**
   * Calculate effective status counts from full order data
   */
  private calculateEffectiveStatusCounts(orders: any[]): {[key: string]: number} {
    const statusCounts: {[key: string]: number} = {};
    
    orders.forEach(order => {
      const effectiveStatus = this.statusService.getEffectiveStatus(order);
      statusCounts[effectiveStatus] = (statusCounts[effectiveStatus] || 0) + 1;
    });
    
    return statusCounts;
  }

  private drawStatus(labels: string[], values: number[]) {
    const ctx = this.statusCanvas.nativeElement.getContext('2d')!;
    this.statusChart?.destroy();
    this.statusChart = new Chart(ctx, {
      type: 'doughnut',
      data: { 
        labels, 
        datasets: [{ 
          data: values,
          backgroundColor: [
            '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40'
          ]
        }] 
      },
      options: { 
        responsive: true, 
        plugins: { 
          legend: { position: 'bottom' }, 
          title: { display: true, text: `Orders by Status (${this.statusRange})` } 
        } 
      }
    });
  }

  private drawSales(labels: string[], totals: number[]) {
    const ctx = this.salesCanvas.nativeElement.getContext('2d')!;
    this.salesChart?.destroy();
    this.salesChart = new Chart(ctx, {
      type: 'line',
      data: { 
        labels, 
        datasets: [{ 
          label: 'Sales (30d)', 
          data: totals, 
          tension: .35, 
          pointRadius: 2,
          borderColor: '#36A2EB',
          backgroundColor: 'rgba(54, 162, 235, 0.1)',
          fill: true
        }] 
      },
      options: { 
        responsive: true, 
        scales: { y: { beginAtZero: true } } 
      }
    });
  }

  private drawLowStock(labels: string[], qty: number[], thr: number[]) {
    const ctx = this.stockCanvas.nativeElement.getContext('2d')!;
    this.stockChart?.destroy();
    this.stockChart = new Chart(ctx, {
      type: 'bar',
      data: { 
        labels, 
        datasets: [
          { 
            label: 'Qty', 
            data: qty,
            backgroundColor: '#FF6384'
          }, 
          { 
            label: 'Threshold', 
            data: thr,
            backgroundColor: '#36A2EB'
          }
        ] 
      },
      options: { 
        indexAxis: 'y', 
        responsive: true, 
        scales: { x: { beginAtZero: true } },
        plugins: { title: { display: true, text: 'Low Stock vs Threshold' } } 
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next(); 
    this.destroy$.complete();
    this.statusChart?.destroy(); 
    this.salesChart?.destroy(); 
    this.stockChart?.destroy();
  }
}