import { Directive, HostListener, Input } from '@angular/core';
import { HelpService } from './help.service';

/**
 * Context help directive:
 * - NEVER opens the help panel on click/touch.
 * - Sets the current help key on hover/focus.
 * - Opens the help panel ONLY on F1 while this element is focused.
 *
 * Usage:
 *   <div appContextHelp="admin/orders"> ... </div>
 */
@Directive({
  selector: '[appContextHelp]',
  standalone: true
})
export class ContextHelpDirective {
  /** Help key for this section/control (e.g., 'admin/orders') */
  @Input('appContextHelp') key = '';

  constructor(private help: HelpService) {}

  /** Just set context (do not open) when user hovers/focuses */
  @HostListener('mouseenter')
  onEnter() {
    if (this.key) this.help.setKey(this.key);
  }

  @HostListener('focusin')
  onFocus() {
    if (this.key) this.help.setKey(this.key);
  }

  /** True context-sensitive behavior: F1 on this element opens its help */
  @HostListener('keydown', ['$event'])
  onKeydown(e: KeyboardEvent) {
    if (e.key === 'F1') {
      e.preventDefault();
      if (this.key) this.help.setKey(this.key);
      this.help.open();
    }
  }
}
