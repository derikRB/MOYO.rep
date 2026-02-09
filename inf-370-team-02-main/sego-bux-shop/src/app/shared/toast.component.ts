import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Toast } from './toast.model';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast">
      <button class="close" (click)="close()">×</button>
      <img *ngIf="toast.imageUrl" [src]="toast.imageUrl" class="thumb" />
      <div class="message">{{ toast.message }}</div>
    </div>
  `,
  styles: [`
    .toast {
      display: flex;
      align-items: center;
      background: #e91e63;
      color: #fff;
      padding: 0.6rem 1rem;
      border-radius: 0.5rem;
      box-shadow: 0 2px 8px rgba(0,0,0,0.15);
    }
    .close {
      background: none;
      border: none;
      color: #fff;
      font-size: 1.2rem;
      margin-right: 0.5rem;
      cursor: pointer;
    }
    .thumb {
      width: 36px;
      height: 36px;
      object-fit: cover;
      border-radius: 4px;
      margin-right: 0.6rem;
    }
    .message {
      flex: 1;
    }
  `]
})
export class ToastComponent {
  @Input() toast!: Toast;
  constructor(private toastSvc: ToastService) {}
  close() { this.toastSvc.remove(this.toast.id); }
}
