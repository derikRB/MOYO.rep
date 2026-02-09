import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import {
  AdminReportsService,
  SalesReport,
  OrderReport,
  FinancialReport,
  StockReport,
  OrdersByCustomer,
  StockTransaction
} from '../services/admin-reports.service';
import { AuthService } from '../services/auth.service';
import { CommonModule } from '@angular/common';

import { DashboardReportComponent } from './dashboard-report.component';
import { SalesListReportComponent } from './sales-list-report.component';
import { OrdersListReportComponent } from './orders-list-report.component';
import { OrdersByCustomerReportComponent } from './orders-by-customer-report.component';
import { StockListReportComponent } from './stock-list-report.component';
import { StockTransactionsReportComponent } from './stock-transactions-report.component';
import { ManagementReportComponent } from './management-report.component';
import { CustomReportComponent } from './custom-report.component';
import { StockForecastReportComponent } from './stock-forecast-report.component';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  templateUrl: './admin-reports.component.html',
  styleUrls: ['./admin-reports.component.scss'],
  providers: [DatePipe],
  imports: [
    CommonModule,
    DashboardReportComponent,
    SalesListReportComponent,
    OrdersListReportComponent,
    OrdersByCustomerReportComponent,
    StockListReportComponent,
    StockTransactionsReportComponent,
    ManagementReportComponent,
    CustomReportComponent,
    StockForecastReportComponent
  ]
})
export class AdminReportsComponent implements OnInit {
  activeTab: string = 'dashboard';
  userName: string = '';
  today: string = '';

  sales: SalesReport[] = [];
  orders: OrderReport[] = [];
  financial: FinancialReport | null = null;
  stock: StockReport[] = [];
  byCust: any[] = [];
  stockTx: any[] = [];
  stockForecast: any[] = [];
  fromDate: string = '';
  toDate: string = '';

  constructor(
    private reports: AdminReportsService,
    private auth: AuthService,
    private datePipe: DatePipe
  ) {}

  ngOnInit() {
    const name = this.auth.getUserName?.() || '';
    this.userName = name || "Employee User";
    this.today = this.datePipe.transform(new Date(), 'medium') || '';
    const start = new Date();
    start.setDate(start.getDate() - 29);
    this.fromDate = start.toISOString().substring(0, 10);
    this.toDate = new Date().toISOString().substring(0, 10);
    this.loadAll();
  }

  setTab(tab: string) { this.activeTab = tab; }

  loadAll() {
    this.reports.getSalesReport(this.fromDate, this.toDate).subscribe(r => this.sales = r ?? []);
    this.reports.getOrderReport().subscribe(r => this.orders = r ?? []);
    this.reports.getFinancialReport().subscribe(r => this.financial = r ?? null);
    this.reports.getStockReport().subscribe(r => this.stock = r ?? []);
    this.reports.getStockForecast().subscribe(r => this.stockForecast = r ?? []);
    this.reports.getOrdersByCustomer().subscribe(r => {
      this.byCust = this.groupOrdersByCustomer(r ?? []);
    });
    this.reports.getStockTransactions().subscribe(r => {
      this.stockTx = this.normalizeStockTransactions(r ?? []);
    });
  }

  applyFilter(dates: {from: string, to: string}) {
    this.fromDate = dates.from;
    this.toDate = dates.to;
    this.reports.getSalesReport(this.fromDate, this.toDate).subscribe(r => this.sales = r ?? []);
  }

  private groupOrdersByCustomer(raw: OrdersByCustomer[]): any[] {
    const grouped: { [key: string]: any[] } = {};
    raw.forEach(o => {
      if (!grouped[o.customerName]) grouped[o.customerName] = [];
      grouped[o.customerName].push({
        orderID: o.orderID,
        orderDate: o.orderDate,
        orderStatusName: o.status,
        totalPrice: o.total,
      });
    });
    return Object.keys(grouped).map(name => ({
      customerName: name,
      orders: grouped[name]
    }));
  }

  private normalizeStockTransactions(raw: StockTransaction[]): any[] {
    return (raw ?? []).map((tx, idx) => ({
      transactionID: idx + 1,
      date: tx.tranDate,
      productName: tx.productName,
      quantity: tx.received ?? tx.adjusted ?? 0,
      type: tx.received ? 'Received' : (tx.adjusted ? 'Adjusted' : 'Unknown')
    }));
  }
}
