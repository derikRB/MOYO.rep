import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from './toast.service';
import { ToastComponent } from './toast.component';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, ToastComponent],
  template: `
    <div class="toast-container">
      <ng-container *ngFor="let t of toastSvc.toasts$ | async">
        <app-toast [toast]="t"></app-toast>
      </ng-container>
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 72px;     /* right below your navbar */
      right: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.6rem;
      z-index: 2000;
    }
  `]
})
export class ToastContainerComponent {
  constructor(public toastSvc: ToastService) {}
}
