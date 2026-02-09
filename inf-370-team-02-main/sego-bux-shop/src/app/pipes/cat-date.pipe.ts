import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'catDate',
  standalone: true
})
export class CatDatePipe implements PipeTransform {
  private fmt = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Africa/Johannesburg',
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
    hour12: false
  });

  transform(value: unknown, withSeconds: boolean = true): string {
    const d = this.parseApiDate(value);
    if (!d || isNaN(d.getTime())) return '—';

    const parts = this.fmt.formatToParts(d);
    const get = (t: string) => parts.find(p => p.type === t)?.value ?? '';
    const hhmm = `${get('hour')}:${get('minute')}` + (withSeconds ? `:${get('second')}` : '');
    return `${get('year')}-${get('month')}-${get('day')} ${hhmm} C.A.T`;
  }

  /**
   * Robust parser for API dates.
   * Rules:
   *  - If there is an explicit offset (Z or +hh:mm / -hh:mm), use it.
   *  - If it’s plain ISO with 'T' and NO offset -> treat as UTC.
   *  - If it’s 'YYYY-MM-DD HH:mm[:ss]' -> treat as UTC.
   *  - Else fallback to browser parsing.
   */
  private parseApiDate(v: unknown): Date | null {
    if (!v && v !== 0) return null;
    if (v instanceof Date) return v;
    if (typeof v === 'number') return new Date(v);

    const s = String(v).trim();
    if (!s) return null;

    // Offset present (Z or ±hh:mm) -> safe to use directly
    if (/\dT\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+\-]\d{2}:?\d{2})$/.test(s)) {
      return new Date(s);
    }

    // Pure ISO with T but no offset -> assume UTC
    if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?$/.test(s)) {
      return new Date(s + 'Z');
    }

    // Space separated -> convert to ISO and assume UTC
    if (/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}(:\d{2})?$/.test(s)) {
      const iso = s.replace(' ', 'T') + (s.endsWith(':') ? '00' : '');
      return new Date(iso + 'Z');
    }

    // Fallback (browser local)
    return new Date(s);
  }
}
