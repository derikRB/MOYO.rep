import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from './shared/navbar/navbar.component';
import { FooterComponent } from './shared/footer/footer.component';
import { RouterOutlet } from '@angular/router';
import { ChatbotWidgetComponent } from './chatbot-widget/chatbot-widget.component';
import { ToastContainerComponent } from './shared/toast-container.component';
import { HelpComponent } from './help.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    FooterComponent,
    RouterOutlet,
    ChatbotWidgetComponent,
    ToastContainerComponent,
    HelpComponent
  ],
  template: `
    <app-navbar></app-navbar>
    <app-toast-container></app-toast-container>
   
    <app-help *ngIf="showHelp"></app-help>
    <router-outlet></router-outlet>
     <button
      (click)="showHelp = !showHelp"
      class="help-btn"
      title="Open system help"
      style="position:fixed; top:74px; right:24px; z-index:1200; background:#e91e63; color:white; border:none; border-radius:22px; width:44px; height:44px; box-shadow:0 2px 8px #0002; font-size:22px; display:flex; align-items:center; justify-content:center;"
    >?</button>
    <app-chatbot-widget></app-chatbot-widget>
    <app-footer></app-footer>
  `
})
export class AppComponent {
  showHelp = false;
}
