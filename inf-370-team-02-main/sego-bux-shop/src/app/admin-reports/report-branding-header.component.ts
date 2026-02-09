import { Component, Input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';

@Component({
  selector: 'app-report-branding-header',
  standalone: true,
  imports: [CommonModule, DatePipe],
  template: `
   <div style="display:flex;align-items:center;margin-bottom:16px;">
        <img src="https://i.postimg.cc/J7JL3WKC/logo.jpg" alt="Logo" style="height:60px;"/>
      <div>
        <h3 style="margin:0;color:#FF5BAA;font-weight:700;">By Sego and Bux Reports</h3>
        <small>
          <span style="color:#8E2236">Generated:</span> 
          <span *ngIf="date">{{ date | date:'medium' }}</span>
          <br>
          <span style="color:#FF5BAA;font-weight:600;">User:</span>
          <span [style.background]="'#FFDFEF'" [style.color]="'#8E2236'" style="padding:2px 8px;border-radius:8px;font-weight:bold;">
            {{ userName || 'Unknown User' }}
          </span>
        </small>
      </div>
    </div>
    <hr>
  `
})
export class ReportBrandingHeaderComponent {
  @Input() date: string | Date = '';
  @Input() userName: string = '';
}
