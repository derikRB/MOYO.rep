import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import type { OrderResponseDto } from '../dto/order-response.dto';

@Component({
  selector: 'app-delivery-calendar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './delivery-calendar.component.html',
  styleUrls: ['./delivery-calendar.component.scss']
})
export class DeliveryCalendarComponent implements OnInit {
  @Input() orders: OrderResponseDto[] = [];

  // static labels
  days   = ['SUN','MON','TUE','WED','THU','FRI','SAT'];
  months = [
    'January','February','March','April','May','June',
    'July','August','September','October','November','December'
  ];

  today = new Date();
  currentMonth = this.today.getMonth();
  currentYear  = this.today.getFullYear();

  years: number[] = [];
  weeks: (Date|null)[][] = [];
  selectedOrder: OrderResponseDto | null = null;

  ngOnInit() {
    const base = this.today.getFullYear();
    for (let y = base - 50; y <= base + 50; y++) this.years.push(y);
    this.generateCalendar(this.currentYear, this.currentMonth);
  }

  generateCalendar(year: number, month: number) {
    const first = new Date(year, month, 1);
    const last  = new Date(year, month + 1, 0);
    this.weeks = [];

    // leading blanks
    let week: (Date|null)[] = [];
    for (let i = 0; i < first.getDay(); i++) week.push(null);

    // days
    for (let d = 1; d <= last.getDate(); d++) {
      week.push(new Date(year, month, d));
      if (week.length === 7) { this.weeks.push(week); week = []; }
    }

    // trailing blanks
    if (week.length > 0) while (week.length < 7) week.push(null);
    if (week.length > 0) this.weeks.push(week);
  }

  get monthLabel(): string {
    return `${this.months[this.currentMonth]} ${this.currentYear}`;
  }

  prevMonth() {
    if (this.currentMonth === 0) { this.currentMonth = 11; this.currentYear--; }
    else this.currentMonth--;
    this.generateCalendar(this.currentYear, this.currentMonth);
  }
  nextMonth() {
    if (this.currentMonth === 11) { this.currentMonth = 0; this.currentYear++; }
    else this.currentMonth++;
    this.generateCalendar(this.currentYear, this.currentMonth);
  }

  onMonthChange(idx: string | number) {
    this.currentMonth = Number(idx);
    this.generateCalendar(this.currentYear, this.currentMonth);
  }
  onYearChange(y: string | number) {
    this.currentYear = Number(y);
    this.generateCalendar(this.currentYear, this.currentMonth);
  }
  goToday() {
    this.currentMonth = this.today.getMonth();
    this.currentYear  = this.today.getFullYear();
    this.generateCalendar(this.currentYear, this.currentMonth);
  }

  /** Normalize any date-ish value to 'YYYY-MM-DD' or null. */
  private toKey(value: unknown): string | null {
    if (!value) return null;
    if (typeof value === 'string') {
      const s = value.trim();
      if (s.length >= 10) return s.slice(0, 10);
      const d = new Date(s);
      if (isNaN(d.getTime())) return null;
      return this.localDate(d);
    }
    if (value instanceof Date) return this.localDate(value);
    const d = new Date(String(value));
    return isNaN(d.getTime()) ? null : this.localDate(d);
  }

  /**
   * Calendar day for an order:
   * Prefer ExpectedDeliveryDate; fall back to OrderDate.
   */
  private orderDayKey(o: OrderResponseDto): string | null {
    return this.toKey((o as any).expectedDeliveryDate ?? (o as any).orderDate);
  }

  /** ——— THE KEY CHANGE: calendar badges & colours driven by DELIVERY STATUS ——— */
  getOrdersForDay(day: Date|null) {
    if (!day) return [];
    const dstr = this.localDate(day);
    return this.orders.filter(o => this.orderDayKey(o) === dstr);
  }

  /** Class used for the coloured badge — map delivery status to existing styles. */
  getDeliveryStatusClass(o: OrderResponseDto) {
    const s = (o.deliveryStatus || '').toLowerCase();
    // Reuse your existing colours: map Dispatched -> 'shipped' style.
    if (s === 'pending')     return 'calendar-order pending';
    if (s === 'dispatched' || s === 'in transit' || s === 'shipped')
                              return 'calendar-order shipped';
    if (s === 'delivered')   return 'calendar-order delivered';
    return 'calendar-order';
  }

  getCellClass(day: Date|null) {
    if (!day) return 'calendar-cell empty';
    if (day.toDateString() === this.today.toDateString()) return 'calendar-cell today';
    return 'calendar-cell';
  }

  /** Cell click = default to first order on that day. */
  selectOrder(day: Date|null) {
    if (!day) return;
    const orders = this.getOrdersForDay(day);
    if (orders.length) this.selectedOrder = orders[0];
  }

  /** Badge click = open the exact order you clicked. */
  openOrder(order: OrderResponseDto, evt?: Event) {
    evt?.stopPropagation();
    this.selectedOrder = order;
  }

  closePopup() { this.selectedOrder = null; }

  private localDate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth()+1).padStart(2,'0');
    const day = String(d.getDate()).padStart(2,'0');
    return `${y}-${m}-${day}`;
  }
}
