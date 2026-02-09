// import { Component, OnDestroy, OnInit, effect, inject, Signal } from '@angular/core';
// import { CommonModule } from '@angular/common';
// import { HelpService, HelpSection, HelpRole } from './help.service';
// import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
// import { FormsModule } from '@angular/forms';
// import { Subscription } from 'rxjs';

// @Component({
//   selector: 'app-help',
//   standalone: true,
//   imports: [CommonModule, FormsModule],
//   templateUrl: './help.component.html',
//   styleUrls: ['./help.component.scss']
// })
// export class HelpComponent implements OnInit, OnDestroy {
//   private help = inject(HelpService);
//   private sanitizer = inject(DomSanitizer);

//   isOpen = false;
//   query = '';
//   activeKey = 'home';
//   role: HelpRole = 'Unknown';

//   sections: HelpSection[] = [];
//   filtered: HelpSection[] = [];

//   private sub = new Subscription();

//   pdfUrl = 'assets/help/YourFullHelp.pdf'; // place your PDF under assets/help/

//   ngOnInit(): void {
//     this.sub.add(this.help.isOpen$.subscribe(v => this.isOpen = v));
//     this.sub.add(this.help.activeKey$.subscribe(k => this.activeKey = k));
//     this.sub.add(this.help.sections$.subscribe(s => {
//       this.sections = s;
//       this.applyFilter();
//       // ensure active key exists
//       if (!this.sections.find(x => x.key === this.activeKey)) {
//         this.activeKey = this.sections[0]?.key ?? 'home';
//       }
//       queueMicrotask(() => this.scrollToActive());
//     }));
//     this.sub.add(this.help.filteredSections$.subscribe(fs => {
//       this.filtered = fs;
//     }));

//     // initialize route activator
//     this.help.initRouteActivation();
//   }

//   ngOnDestroy(): void {
//     this.sub.unsubscribe();
//   }

//   close() { this.help.close(); }

//   onSearch(q: string) {
//     this.query = q;
//     this.help.setQuery(q);
//   }

//   openSection(key: string) {
//     this.help.setContext(key);
//     this.applyFilter();
//     queueMicrotask(() => this.scrollToActive());
//   }

//   asHtml(html: string): SafeHtml {
//     return this.sanitizer.bypassSecurityTrustHtml(html);
//   }

//   private applyFilter() {
//     const q = (this.query || '').trim().toLowerCase();
//     if (!q) {
//       this.filtered = this.sections;
//       return;
//     }
//     this.filtered = this.sections.filter(s => {
//       const hay = (s.title + ' ' + s.content + ' ' + (s.keywords || []).join(' ')).toLowerCase();
//       return hay.includes(q);
//     });
//   }

//   private scrollToActive() {
//     const el = document.querySelector<HTMLElement>(`[data-help-key="${this.activeKey}"]`);
//     if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
//   }

//   openPdfInNewTab() {
//     window.open(this.pdfUrl, '_blank');
//   }
// }
