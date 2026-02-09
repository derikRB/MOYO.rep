import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { OrderService } from '../services/order.service';
import type { OrderResponseDto } from '../dto/order-response.dto';
import { AuthService } from '../services/auth.service';
import { CatDatePipe } from '../pipes/cat-date.pipe';
import { CatRelativePipe } from '../pipes/cat-relative.pipe';

@Component({
  selector: 'app-customer-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, CatDatePipe, CatRelativePipe],
  templateUrl: './customer-orders.component.html',
  styleUrls: ['./customer-orders.component.scss']
})
export class CustomerOrdersComponent implements OnInit {
  public orders: OrderResponseDto[] = [];
  public filteredOrders: OrderResponseDto[] = [];
  public filterStatus = '';
  public filterDate = '';
  public isLoading = true;
  private customerId = 0;

  modalImageUrl: string | null = null;

  reviewedProducts = new Set<string>();

  page = 1;
  pageSize = 5;
  get totalPages() {
    return Math.ceil(this.filteredOrders.length / this.pageSize) || 1;
  }
  pagedOrders() {
    const start = (this.page - 1) * this.pageSize;
    return this.filteredOrders.slice(start, start + this.pageSize);
  }
  nextPage() { if (this.page < this.totalPages) this.page++; }
  prevPage() { if (this.page > 1) this.page--; }
  goToPage(pageNum: number) { if (pageNum >= 1 && pageNum <= this.totalPages) this.page = pageNum; }

  readonly externalSteps = ['Paid', 'Processing', 'Shipped', 'Delivered'] as const;

  constructor(
    private orderService: OrderService,
    private auth: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.customerId = this.auth.getUserId()!;
    this.load();
  }

  private load() {
    this.isLoading = true;
    this.orderService.getOrdersByCustomer(this.customerId).subscribe({
      next: data => {
        this.orders = data;
        this.applyFilters();
        this.isLoading = false;

        // Mark reviewed products if backend supplies a flag
        for (const order of data) {
          for (const line of order.orderLines) {
            if ((line as any).reviewed) {
              this.reviewedProducts.add(`${order.orderID}-${line.productID}`);
            }
          }
        }
      },
      error: () => {
        alert('Failed to load orders');
        this.isLoading = false;
      }
    });
  }

  applyFilters() {
    this.filteredOrders = this.orders.filter(o => {
      const display = this.getDisplayStatus(o);
      const byStatus = !this.filterStatus || display === this.filterStatus;
      const byDate = !this.filterDate || o.orderDate.startsWith(this.filterDate);
      return byStatus && byDate;
    });
    this.page = 1;
  }

  getDisplayStatus(o: OrderResponseDto): string {
    const ds = (o.deliveryStatus || '').trim();
    const os = (o.orderStatusName || '').trim();
    if (ds && ds.toLowerCase() !== 'pending') return ds;
    if (os) return os;
    return ds || 'Pending';
  }

  getOrderStep(o: OrderResponseDto): number {
    let idx = 0;
    const os = (o.orderStatusName || '').toLowerCase();
    if (os === 'processing') idx = 1;
    if (os === 'shipped') idx = 2;
    const ds = (o.deliveryStatus || '').toLowerCase();
    if (ds === 'delivered') idx = 3;
    if (ds === 'dispatched') idx = Math.max(idx, 2);
    return idx;
  }

  progressPercent(o: OrderResponseDto): string {
    const idx = this.getOrderStep(o);
    const last = this.externalSteps.length - 1;
    return `${(idx / last) * 100}%`;
  }

  isStepActive(o: OrderResponseDto, i: number): boolean {
    return i <= this.getOrderStep(o);
  }

  openImageModal(url: string) { this.modalImageUrl = url; }
  closeImageModal() { this.modalImageUrl = null; }

  getFullImageUrl(path?: string | null): string {
    if (!path) return 'assets/no-image.png';
    if (path.startsWith('http')) return path;
    const trimmedApi = environment.apiUrl.replace(/\/$/, '');
    const normalized = path.startsWith('/') ? path : '/' + path;
    return `${trimmedApi}${normalized}`;
  }

  hasCustomization(line: OrderResponseDto['orderLines'][number]): boolean {
    return !!(
      line.customText ||
      line.uploadedImagePath ||
      line.snapshotPath ||
      this.hasNonDefaultStyle(line)
    );
  }

  hasNonDefaultStyle(line: OrderResponseDto['orderLines'][number]): boolean {
    const defaultFont = 'Arial';
    const defaultSize = 16;
    const defaultColor = '#000000';
    const fontChanged = !!line.font && line.font.trim().toLowerCase() !== defaultFont.toLowerCase();
    const sizeChanged = typeof line.fontSize === 'number' && line.fontSize > 0 && line.fontSize !== defaultSize;
    const colorChanged = !!line.color && line.color.trim().toLowerCase() !== defaultColor;
    return fontChanged || sizeChanged || colorChanged;
  }

  isReviewed(orderId: number, productId: number): boolean {
    return this.reviewedProducts.has(`${orderId}-${productId}`);
  }

  goToFeedback(orderId: number) {
    this.router.navigate(['/feedback', orderId]);
  }

  goToReview(orderId: number, productId: number) {
    this.router.navigate(['/product-review', orderId], { queryParams: { productId } });
  }
}
