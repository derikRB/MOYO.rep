import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConfigService, TimerPolicyDto } from '../services/config.service';
import { AuthService } from '../services/auth.service';

@Component({
  standalone: true,
  selector: 'app-system-timers',
  imports: [CommonModule, FormsModule],
  template: `
  <ng-container *ngIf="isStaff()">
    <div class="card">
      <div class="row aic jcsb">
        <h3 class="title">Configurable Timers</h3>
        <small>Updated: {{ timers()?.updatedAtUtc | date:'short' }}</small>
      </div>

      <div class="grid">
        <label>OTP Expiry (minutes)
          <input type="number" [(ngModel)]="otp" min="1" max="30"/>
        </label>

        <label>Session Timeout (minutes)
          <input type="number" [(ngModel)]="sess" min="5" max="240"/>
        </label>
      </div>

      <div class="row gap">
        <button (click)="save()" [disabled]="saving">{{ saving ? 'Saving…' : 'Save' }}</button>
        <span class="muted">Only Admin / Manager / Employee can view & change this.</span>
      </div>
    </div>
  </ng-container>
  `,
  styles: [`
    .card{background:#fff;padding:16px;border-radius:12px;box-shadow:0 4px 24px #00000010;margin-bottom:16px}
    .title{margin:0;color:#e91e63}
    .row{display:flex;align-items:center}
    .aic{align-items:center}.jcsb{justify-content:space-between}
    .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:12px;margin:12px 0}
    label{display:flex;flex-direction:column;font-weight:600}
    input{margin-top:6px;padding:8px;border:1px solid #eee;border-radius:8px}
    button{background:#e91e63;color:#fff;border:0;border-radius:8px;padding:8px 14px;cursor:pointer}
    .muted{color:#777;margin-left:12px}
    .gap{gap:12px}
  `]
})
export class SystemTimersComponent implements OnInit {
  timers = signal<TimerPolicyDto|undefined>(undefined);
  otp = 10;
  sess = 60;
  saving = false;

  constructor(private cfg: ConfigService, private auth: AuthService) {}

  isStaff(): boolean {
    const r = this.auth.getRoleFromToken();
    return r === 'Admin' || r === 'Manager' || r === 'Employee';
  }

  ngOnInit(): void {
    this.cfg.getTimers().subscribe(t => {
      this.timers.set(t);
      this.otp = t.otpExpiryMinutes;
      this.sess = t.sessionTimeoutMinutes;
    });
  }

  save(): void {
    this.saving = true;
    this.cfg.saveTimers({ otpExpiryMinutes: this.otp, sessionTimeoutMinutes: this.sess })
      .subscribe({
        next: () => { this.saving = false; alert('Timers saved.'); },
        error: e => { this.saving = false; alert(e.error?.message || 'Save failed'); }
      });
  }
}
