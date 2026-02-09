import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'catRelative',
  standalone: true
})
export class CatRelativePipe implements PipeTransform {
  transform(value: unknown, nowUtcIso?: string): string {
    const target = this.parseApiDate(value);
    if (!target) return '';

    const now = nowUtcIso ? new Date(nowUtcIso) : new Date();
    const diffMs = target.getTime() - now.getTime();

    const abs = Math.abs(diffMs);
    if (abs < 15_000) return 'now';

    const mins = Math.round(abs / 60_000);
    if (mins < 60) return diffMs < 0 ? `${mins}m ago` : `in ${mins}m`;

    const hrs = Math.round(mins / 60);
    return diffMs < 0 ? `${hrs}h ago` : `in ${hrs}h`;
  }

  private parseApiDate(v: unknown): Date | null {
    if (!v && v !== 0) return null;
    if (v instanceof Date) return v;
    if (typeof v === 'number') return new Date(v);
    const s = String(v).trim();
    if (!s) return null;

    if (/\dT\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+\-]\d{2}:?\d{2})$/.test(s)) return new Date(s);
    if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?$/.test(s)) return new Date(s + 'Z');
    if (/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}(:\d{2})?$/.test(s)) return new Date(s.replace(' ', 'T') + 'Z');
    return new Date(s);
  }
}
