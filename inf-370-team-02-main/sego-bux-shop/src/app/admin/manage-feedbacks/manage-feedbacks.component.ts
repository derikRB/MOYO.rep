import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FeedbackService } from '../../services/feedback.service';
import { FeedbackDto } from '../../dto/feedback.dto';
import { ToastService } from '../../shared/toast.service';
import * as XLSX from 'xlsx';

type RatingFilter = 'All' | '5' | '4' | '3' | '2' | '1';
type RecommendFilter = 'All' | 'Yes' | 'No';

@Component({
  selector: 'app-manage-feedbacks',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-feedbacks.component.html',
  styleUrls: ['./manage-feedbacks.component.scss']
})
export class ManageFeedbacksComponent implements OnInit {
  all: FeedbackDto[] = [];
  rows: FeedbackDto[] = [];
  pageRows: FeedbackDto[] = [];
  loading = false;

  searchTerm = '';
  ratingFilter: RatingFilter = 'All';
  recommendFilter: RecommendFilter = 'All';

  page = 1;
  pageSize = 10;
  totalCount = 0;

  constructor(
    private feedbackSvc: FeedbackService,
    private toast: ToastService
  ) {}

  ngOnInit(): void { this.load(); }

  load() {
    this.loading = true;
    this.feedbackSvc.getAll().subscribe({
      next: data => {
        this.all = [...data].sort((a, b) =>
          new Date(b.submittedDate).getTime() - new Date(a.submittedDate).getTime()
        );
        this.applyFilters();
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  displayUser(r: FeedbackDto): string {
    return (r.userName || 'pleasework again').trim();
  }

  search() { this.applyFilters(); }
  setRatingFilter(v: RatingFilter) { this.ratingFilter = v; this.applyFilters(); }
  setRecommendFilter(v: RecommendFilter) { this.recommendFilter = v; this.applyFilters(); }

  private applyFilters() {
    const q = this.searchTerm.trim().toLowerCase();
    let filtered = this.all;

    if (q) {
      filtered = filtered.filter(r =>
        (r.comments || '').toLowerCase().includes(q) ||
        (r.userName || `#${r.userID}`).toLowerCase().includes(q) ||
        String(r.orderID).includes(q)
      );
    }
    if (this.ratingFilter !== 'All') filtered = filtered.filter(x => x.rating === Number(this.ratingFilter));
    if (this.recommendFilter !== 'All') filtered = filtered.filter(x => x.recommend === (this.recommendFilter === 'Yes'));

    this.rows = filtered;
    this.totalCount = filtered.length;
    this.page = 1;
    this.slicePage();
  }

  private slicePage() {
    const start = (this.page - 1) * this.pageSize;
    this.pageRows = this.rows.slice(start, start + this.pageSize);
  }

  get pageCount(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }
  get pageNumbers(): number[] {
    const max = this.pageCount, window = 5;
    let start = Math.max(1, this.page - Math.floor(window / 2));
    let end = Math.min(max, start + window - 1);
    start = Math.max(1, end - window + 1);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }
  goPage(n: number) { if (n>=1 && n<=this.pageCount && n!==this.page){ this.page = n; this.slicePage(); } }
  goPrev() { this.goPage(this.page - 1); }
  goNext() { this.goPage(this.page + 1); }

  stars(n: number) { return Array.from({ length: 5 }, (_, i) => i < n); }

  exportExcel() {
    const exportRows = this.rows.map(r => ({
      FeedbackID: r.feedbackID,
      User: this.displayUser(r) + ` (#${r.userID})`,
      OrderID: r.orderID,
      Rating: r.rating,
      Recommend: r.recommend ? 'Yes' : 'No',
      SubmittedDate: new Date(r.submittedDate).toLocaleString(),
      Comments: r.comments || ''
    }));

    const ws = XLSX.utils.json_to_sheet(exportRows);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Feedback');
    XLSX.writeFile(wb, 'customer-feedback.xlsx');
    this.toast.show('Feedback exported.');
  }
}
