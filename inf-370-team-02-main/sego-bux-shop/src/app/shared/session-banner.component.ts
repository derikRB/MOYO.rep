import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SessionTimerService } from '../services/session-timer.service';
import { Subscription } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-session-banner',
  imports: [CommonModule],
  template: `
  <div *ngIf="secs>=0" class="banner" [class.warn]="secs<=120">
    <span>Session ends in {{mm}}:{{ss}}</span>
    <button (click)="extend()">Extend</button>
    <button class="ghost" (click)="logout()">Logout</button>
  </div>`,
  styles:[`
    .banner{position:fixed;right:16px;bottom:16px;background:#ffe6f1;color:#c2185b;
      border:1px solid #f8c4db;padding:.5rem .75rem;border-radius:999px;display:flex;
      gap:.5rem;align-items:center;box-shadow:0 3px 18px #e91e6333;z-index:9999}
    .banner.warn{background:#fff3cd;color:#8a6d3b;border-color:#ffe082}
    button{border:0;padding:.35rem .7rem;border-radius:999px;cursor:pointer;background:#e91e63;color:#fff}
    .ghost{background:transparent;color:inherit}
  `]
})
export class SessionBannerComponent implements OnInit, OnDestroy {
  secs = -1; mm='00'; ss='00';
  private sub?: Subscription;

  constructor(private st: SessionTimerService) {}
  ngOnInit(){ this.sub = this.st.secondsLeft$.subscribe(v=>{ this.secs=v; const m=Math.max(0,Math.floor(v/60)); const s=Math.max(0,v%60); this.mm=(''+m).padStart(2,'0'); this.ss=(''+s).padStart(2,'0'); }); this.st.bootstrap(); }
  extend(){ this.st.extend(); }
  logout(){ this.st.logout(); }
  ngOnDestroy(){ this.sub?.unsubscribe(); }
}
