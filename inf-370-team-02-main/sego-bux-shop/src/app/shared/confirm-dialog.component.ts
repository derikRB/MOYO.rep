import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule }                         from '@angular/common';

@Component({
  selector:    'confirm-dialog',
  standalone:  true,
  imports:     [ CommonModule ],
  template: `
    <div class="overlay">
      <div class="dialog">
        <p>{{ message }}</p>
        <div class="actions">
          <button class="btn-cancel" (click)="cancel.emit()">Cancel</button>
          <button class="btn-ok"     (click)="confirm.emit()">OK</button>
        </div>
      </div>
    </div>
  `,
  styleUrls: [ './confirm-dialog.component.scss' ]
})
export class ConfirmDialogComponent {
  @Input()  message = 'Are you sure?';
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel  = new EventEmitter<void>();
}
