import { Component, OnInit, HostListener } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AdminSidebarComponent } from '../admin-sidebar.component';
import { AlertService } from '../../services/admin/alert/alert.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, AdminSidebarComponent],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  sidebarOpen = true;
  sidebarCollapsed = false;
  windowWidth = window.innerWidth;
  lowStockCount = 0;

  constructor(private alertSvc: AlertService) {}

  ngOnInit(): void {
    this.onResize();

    // ✅ Subscribing to the live stream
    this.alertSvc.lowStockCount$.subscribe(count => {
      this.lowStockCount = count;
    });

    // ✅ Prime it on first load
    this.alertSvc.getLowStockAlerts().subscribe();
  }

  @HostListener('window:resize', [])
  onResize() {
    this.windowWidth = window.innerWidth;
    if (this.windowWidth > 900) this.sidebarOpen = true;
    if (this.windowWidth <= 900) this.sidebarCollapsed = false;
  }
}
