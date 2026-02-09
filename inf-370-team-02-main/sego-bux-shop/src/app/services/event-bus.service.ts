import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { filter, map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class EventBusService {
  private bus = new Subject<{ type: string; payload?: any }>();
  emit(type: string, payload?: any) { this.bus.next({ type, payload }); }
  on<T = any>(type: string): Observable<T> {
    return this.bus.asObservable().pipe(filter(e => e.type === type), map(e => e.payload as T));
  }
}
