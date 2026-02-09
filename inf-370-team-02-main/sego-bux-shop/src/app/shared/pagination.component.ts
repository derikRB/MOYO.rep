import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="pagination">
      <button [disabled]="page === 1" (click)="go(page-1)">Prev</button>
      <ng-container *ngFor="let p of pages">
        <button [class.active]="p === page" (click)="go(p)">{{p}}</button>
      </ng-container>
      <button [disabled]="page === totalPages" (click)="go(page+1)">Next</button>
    </div>
  `,
  styles: [`
    .pagination {
      display: flex; gap: 0.4rem; margin: 2rem 0 1rem 0; justify-content: center;
    }
    .pagination button {
      background: #fff;
      border: 1.2px solid #e91e63;
      color: #e91e63;
      border-radius: 5px;
      padding: .35rem 1rem;
      font-weight: bold;
      cursor: pointer;
      transition: background 0.18s;
    }
    .pagination button.active, .pagination button:hover {
      background: #e91e63;
      color: #fff;
    }
    .pagination button:disabled { opacity: 0.5; cursor: not-allowed; }
  `]
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() totalPages = 1;
  @Output() pageChange = new EventEmitter<number>();
  get pages() {
    return Array.from({length: this.totalPages}, (_, i) => i+1);
  }
  go(p: number) { if (p >= 1 && p <= this.totalPages) this.pageChange.emit(p); }
}
