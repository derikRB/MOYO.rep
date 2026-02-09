import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Toast } from './toast.model';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private toasts: Toast[] = [];
  private subject = new BehaviorSubject<Toast[]>([]);
  readonly toasts$ = this.subject.asObservable();
  private counter = 0;

  show(message: string, imageUrl?: string, duration = 3000) {
    const id = ++this.counter;
    const t: Toast = { id, message, imageUrl };
    this.toasts.push(t);
    this.subject.next(this.toasts);
    setTimeout(() => this.remove(id), duration);
  }

  remove(id: number) {
    this.toasts = this.toasts.filter(t => t.id !== id);
    this.subject.next(this.toasts);
  }
}
