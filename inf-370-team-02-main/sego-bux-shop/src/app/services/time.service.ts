import { Injectable } from '@angular/core';
import { Observable, interval, map, shareReplay, startWith } from 'rxjs';

/**
 * Single source of truth for time.
 * - Tracks server/client offset (set by server-time.interceptor via Date header)
 * - Exposes server-based "now" as getter and as an Observable stream
 * - Provides C.A.T formatting + relative helpers used by components
 */
@Injectable({ providedIn: 'root' })
export class TimeService {
  /** serverNow - clientNow (ms) */
  private _offsetMs = 0;

  /** Johannesburg (UTC+02:00, no DST) formatter */
  private readonly catFmt = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Africa/Johannesburg',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false
  });

  /** Called by the interceptor whenever it sees a valid HTTP Date header */
  setServerDateHeader(dateHeader: string | null | undefined): void {
    if (!dateHeader) return;
    const serverNow = new Date(dateHeader).getTime();
    if (isNaN(serverNow)) return;
    this._offsetMs = serverNow - Date.now();
  }

  /** Server "now" as ISO UTC string (use for relative calcs) */
  get nowUtc(): string {
    return new Date(Date.now() + this._offsetMs).toISOString();
  }

  /**
   * Server "now" stream (ISO UTC). Emits immediately, then every second.
   * Useful for live ticking UIs.
   */
  nowIso$(): Observable<string> {
    return interval(1000).pipe(
      startWith(0),
      map(() => this.nowUtc),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  /** C.A.T formatted display: 'YYYY-MM-DD HH:mm[:ss] C.A.T' */
  formatCat(value: unknown, withSeconds: boolean = true): string {
    const d = this.parseApiDate(value);
    if (!d || isNaN(d.getTime())) return '—';
    const parts = this.catFmt.formatToParts(d);
    const get = (t: string) => parts.find(p => p.type === t)?.value ?? '';
    const hhmm = `${get('hour')}:${get('minute')}` + (withSeconds ? `:${get('second')}` : '');
    return `${get('year')}-${get('month')}-${get('day')} ${hhmm} C.A.T`;
  }

  /**
   * Relative short text like 'now', 'in 12m', '2h ago'.
   * baseIso defaults to server "now".
   */
  formatRelativeShort(value: unknown, baseIso?: string): string {
    const target = this.parseApiDate(value);
    if (!target) return '';
    const base = baseIso ? new Date(baseIso) : new Date(this.nowUtc);
    const diffMs = target.getTime() - base.getTime();

    const abs = Math.abs(diffMs);
    if (abs <= 15_000) return 'now';

    const mins = Math.round(abs / 60_000);
    if (mins < 60) return diffMs < 0 ? `${mins}m ago` : `in ${mins}m`;

    const hrs = Math.round(mins / 60);
    return diffMs < 0 ? `${hrs}h ago` : `in ${hrs}h`;
  }

  // ---------- parsing helpers (treat missing timezone as UTC) ----------

  private parseApiDate(v: unknown): Date | null {
    if (!v && v !== 0) return null;
    if (v instanceof Date) return v;
    if (typeof v === 'number') return new Date(v);

    const s = String(v).trim();
    if (!s) return null;

    // ISO with explicit offset (Z or ±hh:mm) -> trust it
    if (/\dT\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+\-]\d{2}:?\d{2})$/.test(s)) {
      return new Date(s);
    }

    // ISO with 'T' but no offset -> assume UTC
    if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?$/.test(s)) {
      return new Date(s + 'Z');
    }

    // Space separated 'YYYY-MM-DD HH:mm[:ss]' -> assume UTC
    if (/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}(:\d{2})?$/.test(s)) {
      const iso = s.replace(' ', 'T') + (s.match(/:\d{2}$/) ? '' : (s.endsWith(':') ? '00' : ''));
      return new Date(iso + 'Z');
    }

    // Fallback
    return new Date(s);
  }
}
