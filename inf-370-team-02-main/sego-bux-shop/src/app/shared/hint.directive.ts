import { Directive, ElementRef, Input, OnInit, Renderer2 } from '@angular/core';

/**
 * Adds field-level hints: a tooltip (title) and/or a placeholder on inputs.
 * Usage:
 *   <input appHint="Filter by order number or customer" hintPlaceholder="e.g., 100245 or Jane Doe" />
 */
@Directive({
  selector: '[appHint], [hintPlaceholder]',
  standalone: true
})
export class HintDirective implements OnInit {
  @Input('appHint') hintText?: string;
  @Input() hintPlaceholder?: string;

  constructor(private el: ElementRef<HTMLElement>, private r: Renderer2) {}

  ngOnInit(): void {
    if (this.hintText) {
      this.r.setAttribute(this.el.nativeElement, 'title', this.hintText);
      this.r.setAttribute(this.el.nativeElement, 'aria-label', this.hintText);
    }

    // Only set placeholder on input/textarea
    const tag = this.el.nativeElement.tagName.toLowerCase();
    if (this.hintPlaceholder && (tag === 'input' || tag === 'textarea')) {
      this.r.setAttribute(this.el.nativeElement, 'placeholder', this.hintPlaceholder);
    }
  }
}
