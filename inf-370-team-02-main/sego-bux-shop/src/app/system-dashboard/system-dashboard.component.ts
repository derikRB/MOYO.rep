import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule, ReactiveFormsModule, FormBuilder, Validators, FormGroup, AbstractControl
} from '@angular/forms';
import { HttpClient, HttpEvent, HttpEventType, HttpParams } from '@angular/common/http';
import { Subscription, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';

import { ConfigService, TimerPolicyDto } from '../services/config.service';
import { MaintenanceService } from '../services/maintenance.service';
import { MetricsChartsComponent } from '../admin/metrics-charts.component';

import { ToastService } from '../shared/toast.service';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';
import { TimeService } from '../services/time.service';

interface CurrentTimerStateDto {
  nowUtc: string;
  otpExpiresAtUtc?: string | null;
  sessionExpiresAtUtc?: string | null;
}

interface AuditRow {
  id: number;
  utcTimestamp: string;
  userDisplay?: string | null;
  userEmail?: string | null;
  action: string;
  entity: string;
  entityId?: string | null;
  criticalValue?: string | null;
}
interface Paged<T> { page: number; pageSize: number; total: number; items: T[]; }
type PendingAction = 'saveTimers' | 'backup' | 'restore' | null;

/** ---- Roles/Permissions UI shape ---- */
interface AccessRow {
  resource: string;                    // feature key: e.g. "reports", "stock"
  label: string;                       // display
  perms: Record<string, boolean>;      // role -> allowed
}

/** ---- Matches backend FeatureAccessDto ---- */
interface FeatureAccessDto {
  key: string;
  displayName: string;
  roles: string[];
}

@Component({
  selector: 'app-system-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MetricsChartsComponent, ConfirmDialogComponent],
  templateUrl: './system-dashboard.component.html',
  styleUrls: ['./system-dashboard.component.scss']
})
export class SystemDashboardComponent implements OnInit, OnDestroy {
  readonly Math = Math;

  // timers
  timersForm!: FormGroup;
  policy?: TimerPolicyDto;
  current?: CurrentTimerStateDto;
  timersLoading = false;
  timersSaving = false;

  // tick (server "now" via TimeService)
  nowTick = '';             // ISO in UTC
  nowSub?: Subscription;

  // audit
  auditEnabled = true;
  auditLoading = false;
  auditPage = 1;
  auditPageSize = 20;
  auditTotal = 0;
  auditItems: AuditRow[] = [];
  auditFilters = { from: '', to: '', user: '', action: '', entity: '' };
  auditError = '';

  // maintenance
  restoreProgress = 0;
  restoreMsg = '';
  restoring = false;
  backupMsg = '';

  // confirms
  confirming = false;
  confirmMessage = '';
  private pendingAction: PendingAction = null;
  private pendingRestoreFile?: File;

  // ---- Roles/Permissions UI ----
  accessLoading = false;
  accessSaving  = false;
  accessError   = '';
  roles: string[] = ['Admin', 'Manager', 'Employee', 'Customer']; // fallback/default
  accessRows: AccessRow[] = [];
  accessMsg = '';

  private api = environment.apiUrl;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private configSvc: ConfigService,
    private maintSvc: MaintenanceService,
    private toast: ToastService,
    private time: TimeService
  ) {
    this.timersForm = this.fb.group({
      otpExpiryMinutes: [10, [Validators.required, Validators.min(1), Validators.max(30)]],
      sessionTimeoutMinutes: [60, [Validators.required, Validators.min(5), Validators.max(240)]],
    });
  }

  ngOnInit(): void {
    this.nowTick = this.time.nowUtc;
    this.nowSub = this.time.nowIso$().subscribe((v: string) => this.nowTick = v);

    this.loadTimers();
    this.refreshCurrent();
    this.loadAudit();

    // load roles/access matrix
    this.loadAccess();
  }
  ngOnDestroy(): void { this.nowSub?.unsubscribe(); }

  // ---------- control getters (for template)
  get otpCtrl(): AbstractControl { return this.timersForm.get('otpExpiryMinutes')!; }
  get sessCtrl(): AbstractControl { return this.timersForm.get('sessionTimeoutMinutes')!; }

  // ---------- utilities
  private clampOtp(mins: number): number  { return Math.min(30, Math.max(1, mins)); }
  private clampSess(mins: number): number { return Math.min(240, Math.max(5, mins)); }

  private addMinutesUtc(baseIso: string, mins: number): string {
    const d = new Date(baseIso);
    d.setUTCMinutes(d.getUTCMinutes() + mins);
    return d.toISOString();
  }

  /** Format an ISO-in-UTC string to a C.A.T display string. */
  formatCat(iso?: string | null): string {
    return this.time.formatCat(iso ?? null);
  }

  /** e.g., "now", "in 12m", "in 2h" */
  formatRelativeShort(targetIso?: string | null): string {
    const baseIso = this.current?.nowUtc || this.time.nowUtc;
    return this.time.formatRelativeShort(targetIso ?? null, baseIso);
  }

  /** "≈ H h M m" helper for minutes */
  asHm(mins: any): string {
    const n = Number(mins ?? 0);
    const m = isFinite(n) ? Math.max(0, Math.round(n)) : 0;
    const h = Math.floor(m / 60);
    const r = m % 60;
    return `${h} h ${r} m`;
  }

  // ---------- previews (when API doesn't provide an active expiry)
  get otpPreview(): string {
    const mins = this.clampOtp(Number(this.otpCtrl.value ?? 0));
    const atIso = this.addMinutesUtc(this.current?.nowUtc || this.time.nowUtc, mins);
    return `${this.formatCat(atIso)} (${this.formatRelativeShort(atIso)}) (preview)`;
  }
  get sessionPreview(): string {
    const mins = this.clampSess(Number(this.sessCtrl.value ?? 0));
    const atIso = this.addMinutesUtc(this.current?.nowUtc || this.time.nowUtc, mins);
    return `${this.formatCat(atIso)} (${this.formatRelativeShort(atIso)}) (preview)`;
  }

  // ---------- C.A.T → UTC helpers for Audit filters ----------
  private catToUtcIso(input: string): string | null {
    const t = (input || '').trim();
    if (!t) return null;

    const m = t.match(
      /^(\d{4})-(\d{2})-(\d{2})(?:[ T](\d{2}):(\d{2})(?::(\d{2}))?)?$/
    );
    if (!m) return null;

    const Y  = +m[1], Mo = +m[2], D = +m[3];
    const hh = +(m[4] ?? '0'), mm = +(m[5] ?? '0'), ss = +(m[6] ?? '0');

    // C.A.T is UTC+02:00 (no DST) → convert to UTC by subtracting 2h
    const utc = new Date(Date.UTC(Y, Mo - 1, D, hh - 2, mm, ss, 0));
    return utc.toISOString();
  }

  // ---------- timers ----------
  loadTimers(): void {
    this.timersLoading = true;
    this.configSvc.getTimers()
      .pipe(finalize(() => (this.timersLoading = false)))
      .subscribe({
        next: (p) => {
          this.policy = p;
          this.timersForm.patchValue(
            { otpExpiryMinutes: p.otpExpiryMinutes, sessionTimeoutMinutes: p.sessionTimeoutMinutes },
            { emitEvent: false }
          );
        },
        error: (err) => {
          console.error('getTimers error', err);
          this.toast.show('Failed to load timers');
        }
      });
  }

  /** show confirm first */
  promptSaveTimers(): void {
    if (this.timersForm.invalid) return;
    const otpMins  = this.clampOtp(Number(this.otpCtrl.value ?? 0));
    const sessMins = this.clampSess(Number(this.sessCtrl.value ?? 0));
    this.confirmMessage = `Save timers?\n\nOTP: ${otpMins} min • Session: ${sessMins} min`;
    this.pendingAction = 'saveTimers';
    this.confirming = true;
  }

  private applySaveTimers(): void {
    this.timersSaving = true;
    const otpMins  = this.clampOtp(Number(this.otpCtrl.value ?? 0));
    const sessMins = this.clampSess(Number(this.sessCtrl.value ?? 0));
    const body = { otpExpiryMinutes: otpMins, sessionTimeoutMinutes: sessMins };

    this.configSvc.saveTimers(body)
      .pipe(finalize(() => (this.timersSaving = false)))
      .subscribe({
        next: () => {
          this.loadTimers();
          this.refreshCurrent();
          this.toast.show('Timers saved');
        },
        error: (err) => {
          console.error('updateTimers error', err);
          this.toast.show('Failed to save timers');
        }
      });
  }

  refreshCurrent(): void {
    this.http.get<CurrentTimerStateDto>(`${this.api}/api/timers/current`)
      .subscribe({
        next: (c) => (this.current = c),
        error: (err) => console.error('getCurrentTimerState error', err)
      });
  }

  // ---------- audit ----------
  loadAudit(page: number = this.auditPage): void {
    this.auditError = '';
    this.auditLoading = true;

    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(this.auditPageSize));

    const fromUtc = this.catToUtcIso(this.auditFilters.from);
    const toUtc   = this.catToUtcIso(this.auditFilters.to);
    if (fromUtc) params = params.set('fromUtc', fromUtc);
    if (toUtc)   params = params.set('toUtc', toUtc);

    if (this.auditFilters.user)   params = params.set('user', this.auditFilters.user);
    if (this.auditFilters.action) params = params.set('action', this.auditFilters.action);
    if (this.auditFilters.entity) params = params.set('entity', this.auditFilters.entity);

    this.http.get<Paged<AuditRow>>(`${this.api}/api/audit`, { params })
      .pipe(
        catchError(_ => {
          this.auditEnabled = false;
          this.auditError = 'Audit API not available yet.';
          this.toast.show('Audit API not available');
          return of({ page, pageSize: this.auditPageSize, total: 0, items: [] } as Paged<AuditRow>);
        }),
        finalize(() => (this.auditLoading = false))
      )
      .subscribe(res => {
        this.auditEnabled = true;
        this.auditPage = res.page;
        this.auditItems = res.items || [];
        this.auditTotal = res.total || 0;
      });
  }

  clearAuditFilters(): void {
    this.auditFilters = { from: '', to: '', user: '', action: '', entity: '' };
    this.loadAudit(1);
  }

  // ---------- maintenance ----------
  promptBackup(): void {
    this.confirmMessage = 'Create and download a backup now?';
    this.pendingAction = 'backup';
    this.confirming = true;
  }

  private applyBackup(): void {
    this.backupMsg = 'Preparing backup...';
    this.maintSvc.backup().subscribe({
      next: (blob) => {
        const ts = new Date().toISOString().replace(/[:.]/g, '-');
        const filename = `backup-${ts}.bak`;
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = filename; a.click();
        URL.revokeObjectURL(url);
        this.backupMsg = 'Backup downloaded.';
        this.toast.show('Backup downloaded');
      },
      error: (err) => {
        console.error('backup error', err);
        this.backupMsg = 'Backup failed.';
        this.toast.show('Backup failed');
      }
    });
  }

  onRestoreFileSelected(list: FileList | null): void {
    if (!list || list.length === 0) return;
    const file = list[0];
    if (!file.name.toLowerCase().endsWith('.bak')) {
      this.restoreMsg = 'Please select a .bak file.';
      this.toast.show('Select a .bak file');
      return;
    }
    this.pendingRestoreFile = file;
    this.confirmMessage = `Restore from "${file.name}" ? The app may restart.`;
    this.pendingAction = 'restore';
    this.confirming = true;
  }

  private applyRestore(): void {
    if (!this.pendingRestoreFile) return;
    const file = this.pendingRestoreFile;

    this.restoreMsg = 'Uploading...';
    this.restoring = true;
    this.restoreProgress = 0;

    this.maintSvc.restore(file).subscribe({
      next: (evt: HttpEvent<any>) => {
        if (evt.type === HttpEventType.UploadProgress && (evt as any).total) {
          const total = (evt as any).total as number;
          const loaded = (evt as any).loaded as number;
          this.restoreProgress = Math.round((loaded / total) * 100);
        } else if (evt.type === HttpEventType.Response) {
          this.restoreMsg = 'Restore scheduled. The app may restart.';
          this.restoring = false;
          this.toast.show('Restore scheduled');
        }
      },
      error: (err) => {
        console.error('restore error', err);
        this.restoreMsg = 'Restore failed.';
        this.restoring = false;
        this.toast.show('Restore failed');
      }
    });
  }

  // ---------- confirm dialog handlers ----------
  onConfirm(): void {
    this.confirming = false;
    switch (this.pendingAction) {
      case 'saveTimers': this.applySaveTimers(); break;
      case 'backup':     this.applyBackup();     break;
      case 'restore':    this.applyRestore();    break;
    }
    this.pendingAction = null;
  }
  onCancel(): void {
    this.confirming = false;
    this.pendingAction = null;
    this.pendingRestoreFile = undefined;
  }

  // ---------- Roles/Permissions ----------
  loadAccess(): void {
    this.accessLoading = true;
    this.accessError = '';

    this.http.get<FeatureAccessDto[]>(`${this.api}/api/admin/feature-access`)
      .pipe(
        catchError(err => {
          console.warn('feature-access GET failed; using defaults', err);

          if (err?.status === 403) {
            this.accessError = 'Forbidden (Admin only).';
            this.toast.show('Forbidden: only Admin can view feature access.');
          } else {
            this.accessError = 'Access API not available.';
            this.toast.show('Access API not available — using defaults.');
          }

          // Fallback local matrix so UI works even if API is not ready.
          if (!this.accessRows.length) {
            this.accessRows = [
              { resource: 'reports',       label: 'Reports',        perms: { Admin: true, Manager: true, Employee: true, Customer: false } },
              { resource: 'stock',         label: 'Stock Manager',  perms: { Admin: true, Manager: true, Employee: true, Customer: false } },
              { resource: 'products',      label: 'Products',       perms: { Admin: true, Manager: true, Employee: true, Customer: false } },
              { resource: 'categories',    label: 'Categories',     perms: { Admin: true, Manager: true, Employee: true, Customer: false } },
              { resource: 'orders',        label: 'Orders',         perms: { Admin: true, Manager: true, Employee: true, Customer: false } },
              { resource: 'templates',     label: 'Templates',      perms: { Admin: true, Manager: true, Employee: true, Customer: false } },
              { resource: 'vat',           label: 'VAT',            perms: { Admin: true, Manager: false, Employee: true, Customer: false } },
              { resource: 'faqs',          label: 'FAQs',           perms: { Admin: true, Manager: true, Employee: true, Customer: false } },
              { resource: 'chatbotConfig', label: 'Chatbot Config', perms: { Admin: true, Manager: false, Employee: false, Customer: false } },
              { resource: 'dashboard',     label: 'Dashboard',      perms: { Admin: true, Manager: true, Employee: true, Customer: false } },
            ];
          }
          return of([] as FeatureAccessDto[]);
        }),
        finalize(() => (this.accessLoading = false))
      )
      .subscribe(dtos => {
        if (!dtos?.length) return;

        // Ensure roles list
        if (!this.roles?.length) {
          this.roles = ['Admin', 'Manager', 'Employee', 'Customer'];
        }

        // Map backend DTOs to UI rows
        this.accessRows = dtos.map(dto => {
          const perms: Record<string, boolean> = {};
          this.roles.forEach(r => perms[r] = dto.roles?.includes(r) ?? false);
          return { resource: dto.key, label: dto.displayName, perms } as AccessRow;
        });

        this.toast.show('Access matrix loaded');
      });
  }

  onToggleRole(row: AccessRow, role: string, checked: boolean): void {
    row.perms = row.perms || {};
    row.perms[role] = checked;
  }

  saveAccess(): void {
    this.accessSaving = true;
    this.accessMsg = '';
    this.accessError = '';

    // tell user immediately
    this.toast.show('Saving permissions…');

    // Convert UI matrix -> FeatureAccessDto[]
    const body: FeatureAccessDto[] = this.accessRows.map(row => ({
      key: row.resource,
      displayName: row.label,
      roles: this.roles.filter(r => !!row.perms?.[r])
    }));

    this.http.put(`${this.api}/api/admin/feature-access`, body)
      .pipe(
        catchError(err => {
          console.error('feature-access PUT failed', err);
          this.accessMsg = err?.status === 403
            ? 'Only Admin can save.'
            : 'Failed to save permissions.';
          this.toast.show(this.accessMsg);
          return of(null);
        }),
        finalize(() => (this.accessSaving = false))
      )
      .subscribe(ok => {
        if (ok === null) return;
        this.accessMsg = 'Permissions saved.';
        this.toast.show('Permissions saved');
      });
  }

  resetAccessLocal(): void {
    this.loadAccess();
    this.toast.show('Permissions reloaded');
  }
}
