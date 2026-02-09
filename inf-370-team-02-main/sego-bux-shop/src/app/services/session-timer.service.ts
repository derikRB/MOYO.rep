import { Injectable, NgZone } from '@angular/core';
import { BehaviorSubject, interval } from 'rxjs';
import { ConfigService } from './config.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class SessionTimerService {
  private deadlineKey = 'session.deadlineUtc';
  private ticking = false;

  readonly secondsLeft$ = new BehaviorSubject<number>(-1);

  constructor(
    private cfg: ConfigService,
    private auth: AuthService,
    private zone: NgZone
  ) {}

  bootstrap(): void {
    const token = this.auth.getToken();
    const dl = localStorage.getItem(this.deadlineKey);
    if (token && dl) {
      const secs = Math.max(0, Math.floor((Date.parse(dl) - Date.now()) / 1000));
      if (secs > 0) this.tick(secs); else this.logout();
    }
  }

  start(): void {
    this.cfg.getTimers().subscribe(t => {
      const secs = (t.sessionTimeoutMinutes || 20) * 60;
      const deadline = new Date(Date.now() + secs * 1000).toISOString();
      localStorage.setItem(this.deadlineKey, deadline);
      this.tick(secs);
    });
  }

  extend(): void { this.start(); }

  logout(): void {
    localStorage.removeItem(this.deadlineKey);
    this.secondsLeft$.next(-1);
    this.ticking = false;
    this.auth.logout();
  }

  private tick(initialSeconds: number): void {
    this.secondsLeft$.next(initialSeconds);
    if (this.ticking) return;
    this.ticking = true;

    this.zone.runOutsideAngular(() => {
      interval(1000).subscribe(() => {
        const dl = localStorage.getItem(this.deadlineKey);
        if (!dl) { this.ticking = false; return; }
        const left = Math.floor((Date.parse(dl) - Date.now()) / 1000);
        this.zone.run(() => this.secondsLeft$.next(left));
        if (left <= 0) this.zone.run(() => this.logout());
      });
    });
  }
}
