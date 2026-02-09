import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductReviewService } from '../../services/product-review.service';
import { ProductReviewDto } from '../../dto/product-review';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { ToastService } from '../../shared/toast.service';

type ConfirmAction = 'approve' | 'decline' | 'bulk-approve' | 'bulk-decline';
type StatusFilter = 'All' | 'Pending' | 'Approved' | 'Declined';

@Component({
  selector: 'app-manage-product-reviews',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmDialogComponent],
  templateUrl: './manage-product-reviews.component.html',
  styleUrls: ['./manage-product-reviews.component.scss']
})
export class ManageProductReviewsComponent implements OnInit {
  reviews: ProductReviewDto[] = [];
  displayed: ProductReviewDto[] = [];

  selectedIds = new Set<number>();
  loading = false;

  filterStatus: StatusFilter = 'All';
  page = 1;
  pageSize = 10;
  totalCount = 0;

  searchTerm = '';
  modalImage: string | null = null;

  confirming = false;
  confirmAction: ConfirmAction | null = null;
  targetReview: ProductReviewDto | null = null;

  // bottom sheet "Manage"
  manageFor: number | null = null;

  constructor(
    private reviewSvc: ProductReviewService,
    private toast: ToastService
  ) {}

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.reviewSvc.getAllReviewsPaged(this.page, this.pageSize).subscribe({
      next: res => {
        this.reviews = res.items;
        this.displayed = this.reviews; // server-paged for "All"
        this.totalCount = res.totalCount;
        this.loading = false;
        this.selectedIds.clear();
      },
      error: () => (this.loading = false)
    });
  }

  filter(status: StatusFilter) {
    this.filterStatus = status;
    this.selectedIds.clear();

    if (status === 'All') {
      this.page = 1;
      this.load();
      return;
    }

    this.loading = true;
    this.reviewSvc.getReviewsByStatus(status).subscribe({
      next: data => {
        this.loading = false;
        const q = this.searchTerm.trim().toLowerCase();
        this.reviews = q
          ? data.filter(r =>
              (r.userName || '').toLowerCase().includes(q) ||
              (r.reviewText || '').toLowerCase().includes(q) ||
              (r.reviewTitle || '').toLowerCase().includes(q) ||
              (r.productName || '').toLowerCase().includes(q)
            )
          : data;
        this.totalCount = this.reviews.length;
        this.page = 1;
        this.slicePage();
      },
      error: () => (this.loading = false)
    });
  }

  private slicePage() {
    if (this.filterStatus === 'All') { this.displayed = this.reviews; return; }
    const start = (this.page - 1) * this.pageSize;
    this.displayed = this.reviews.slice(start, start + this.pageSize);
  }

  get pageCount(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }
  get pageNumbers(): number[] {
    const max = this.pageCount, window = 5;
    let start = Math.max(1, this.page - Math.floor(window / 2));
    let end = Math.min(max, start + window - 1);
    start = Math.max(1, end - window + 1);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }
  goPage(n: number) { if (n>=1 && n<=this.pageCount && n!==this.page){ this.page = n; (this.filterStatus === 'All') ? this.load() : this.slicePage(); } }
  goPrev() { this.goPage(this.page - 1); }
  goNext() { this.goPage(this.page + 1); }

  search() {
    if (this.filterStatus === 'All') {
      const q = this.searchTerm.trim().toLowerCase();
      this.displayed = q
        ? this.reviews.filter(r =>
            (r.userName || '').toLowerCase().includes(q) ||
            (r.reviewText || '').toLowerCase().includes(q) ||
            (r.reviewTitle || '').toLowerCase().includes(q) ||
            (r.productName || '').toLowerCase().includes(q)
          )
        : this.reviews;
      return;
    }
    const q = this.searchTerm.trim().toLowerCase();
    this.reviews = this.reviews.filter(r =>
      (r.userName || '').toLowerCase().includes(q) ||
      (r.reviewText || '').toLowerCase().includes(q) ||
      (r.reviewTitle || '').toLowerCase().includes(q) ||
      (r.productName || '').toLowerCase().includes(q)
    );
    this.totalCount = this.reviews.length;
    this.page = 1;
    this.slicePage();
  }

  /** new: match “categories” toolbar with a Clear button */
  clearSearch() {
    if (!this.searchTerm) return;
    this.searchTerm = '';
    this.search();
  }

  approve(r: ProductReviewDto) { this.openConfirm('approve', r); }
  decline(r: ProductReviewDto) { this.openConfirm('decline', r); }
  bulkApprove() { if (this.selectedIds.size) this.openConfirm('bulk-approve'); }
  bulkDecline() { if (this.selectedIds.size) this.openConfirm('bulk-decline'); }

  openConfirm(action: ConfirmAction, r: ProductReviewDto | null = null) {
    this.confirmAction = action;
    this.targetReview = r;
    this.confirming = true;
    this.closeManage();
  }
  onConfirm() {
    const done = () => {
      this.confirming = false;
      this.targetReview = null;
      this.selectedIds.clear();
      (this.filterStatus === 'All') ? this.load() : this.filter(this.filterStatus);
    };

    switch (this.confirmAction) {
      case 'approve':
        if (this.targetReview)
          this.reviewSvc.approveReview(this.targetReview.reviewID).subscribe(() => {
            this.toast.show(`Approved review #${this.targetReview!.reviewID}`);
            done();
          });
        break;
      case 'decline':
        if (this.targetReview)
          this.reviewSvc.declineReview(this.targetReview.reviewID).subscribe(() => {
            this.toast.show(`Declined review #${this.targetReview!.reviewID}`);
            done();
          });
        break;
      case 'bulk-approve':
        this.reviewSvc.bulkApprove([...this.selectedIds]).subscribe(() => {
          this.toast.show(`Approved ${this.selectedIds.size} review(s).`);
          done();
        });
        break;
      case 'bulk-decline':
        this.reviewSvc.bulkDecline([...this.selectedIds]).subscribe(() => {
          this.toast.show(`Declined ${this.selectedIds.size} review(s).`);
          done();
        });
        break;
    }
  }
  onCancel() { this.confirming = false; this.targetReview = null; }

  toggleSelection(id: number) {
    this.selectedIds.has(id) ? this.selectedIds.delete(id) : this.selectedIds.add(id);
  }
  toggleAll() {
    if (this.selectedIds.size === this.displayed.length) this.selectedIds.clear();
    else this.displayed.forEach(r => this.selectedIds.add(r.reviewID));
  }

  /** ---------- IMAGE PREVIEW ---------- */
  openModal(photoUrl: string) {
    this.modalImage = photoUrl;
    try { document.body.classList.add('modal-open'); } catch {}
  }
  closeModal() {
    this.modalImage = null;
    try { document.body.classList.remove('modal-open'); } catch {}
  }
  @HostListener('document:keydown.escape') onEsc() {
    if (this.modalImage) this.closeModal();
  }
  /** ----------------------------------- */

  getFullPhotoUrl(url: string): string {
    return !url ? '' : (url.startsWith('http') ? url : 'https://localhost:7025' + url);
  }
  handleImgError(event: Event) {
    const el = event.target as HTMLImageElement | null;
    if (el) el.style.display = 'none';
  }

  /* Manage sheet helpers */
  openManage(id: number){ this.manageFor = id; }
  closeManage(){ this.manageFor = null; }
  onApproveFromSheet(): void {
    if (!this.manageFor) return;
    const r = this.displayed.find(it => it.reviewID === this.manageFor);
    if (r) this.approve(r);
  }
  onDeclineFromSheet(): void {
    if (!this.manageFor) return;
    const r = this.displayed.find(it => it.reviewID === this.manageFor);
    if (r) this.decline(r);
  }

  @HostListener('document:click') closeAnyOpen(){ /* placeholder */ }
}
