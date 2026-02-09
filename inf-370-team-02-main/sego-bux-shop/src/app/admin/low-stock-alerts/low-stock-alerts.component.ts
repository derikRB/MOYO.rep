import { Component, OnInit }                   from '@angular/core';
import { CommonModule }                        from '@angular/common';
import {
  AlertService,
  LowStockAlert
} from '../../services/admin/alert/alert.service';
import { ToastService }                        from '../../shared/toast.service';
import { ConfirmDialogComponent }              from '../../shared/confirm-dialog.component';

@Component({
  selector:    'app-low-stock-alerts',
  standalone:  true,
  imports:     [
    CommonModule,
    ConfirmDialogComponent     // ← make sure it's here
  ],
  templateUrl: './low-stock-alerts.component.html',
  styleUrls:   ['./low-stock-alerts.component.scss']
})
export class LowStockAlertsComponent implements OnInit {
  alerts: LowStockAlert[] = [];

  // confirm‑dialog state
  confirmingResolve = false;
  pendingAlertId!: number;
  confirmMessage   = '';

  constructor(
    private alertSvc: AlertService,
    private toast:    ToastService
  ) {}

  ngOnInit(): void {
    this.loadAlerts();
  }

  private loadAlerts(): void {
    this.alertSvc.getLowStockAlerts().subscribe({
      next: alerts => this.alerts = alerts,
      error: e     => this.toast.show('Failed to load alerts: ' + e.message)
    });
  }

  /** trigger our custom dialog instead of window.confirm */
  promptResolve(id: number): void {
    this.pendingAlertId    = id;
    this.confirmMessage    = `Mark alert #${id} as resolved?`;
    this.confirmingResolve = true;
  }

  onResolveConfirmed(): void {
    this.confirmingResolve = false;
    this.alertSvc.resolveAlert(this.pendingAlertId).subscribe({
      next: alerts => {
        this.alerts = alerts;
        this.toast.show(`Alert #${this.pendingAlertId} resolved`);
      },
      error: e => this.toast.show('Failed to resolve alert: ' + e.message)
    });
  }

  onResolveCanceled(): void {
    this.confirmingResolve = false;
  }

  /** Manual “Check Now” trigger */
  refresh(): void {
    this.alertSvc.checkLowStock().subscribe({
      next: alerts => {
        this.alerts = alerts;
        this.toast.show('Low‑stock check completed');
      },
      error: e => this.toast.show('Failed to refresh alerts: ' + e.message)
    });
  }
}
