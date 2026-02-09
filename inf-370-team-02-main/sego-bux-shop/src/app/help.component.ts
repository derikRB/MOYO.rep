import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface HelpSection {
  key: string;
  title: string;
  content: string;
}

@Component({
  selector: 'app-help',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './help.component.html',
  styleUrls: ['./help.component.scss']
})
export class HelpComponent {
  @Output() close = new EventEmitter<void>();   // <--- Output event

  searchTerm: string = '';
  showPDF = false;
  sections: HelpSection[] = [
    { key: 'home', title: 'Home Page', content: 'Use the sidebar to navigate. Click "Help" any time for support.' },
    { key: 'products', title: 'Managing Products', content: 'Add, update, delete, or import products on the Products page. Use tooltips for field-level guidance.' },
    { key: 'orders', title: 'Managing Orders', content: 'Track, update, and view details of orders.' },
    { key: 'import', title: 'Bulk Import (Excel)', content: 'Click "Import Products (Excel)" to upload a .xlsx file. Each column maps to a product property.' },
    { key: 'faq', title: 'FAQ', content: 'Find answers to common questions below.' }
  ];
  filteredSections = this.sections;
  searchHelp() {
    const t = this.searchTerm.trim().toLowerCase();
    if (!t) {
      this.filteredSections = this.sections;
    } else {
      this.filteredSections = this.sections.filter(
        s => s.title.toLowerCase().includes(t) || s.content.toLowerCase().includes(t)
      );
    }
  }
  openPDF() { this.showPDF = true; }
  closePDF() { this.showPDF = false; }

  // Add this method:
  onCloseClick() {
    this.close.emit();
  }
}
