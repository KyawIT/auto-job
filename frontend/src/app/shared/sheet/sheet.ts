import { Component, effect, ElementRef, input, model, viewChild } from '@angular/core';

let nextId = 0;

/**
 * Modal sheet on a native `<dialog>`: focus is trapped and restored by the browser,
 * Escape closes it, the background dims. Bind `[(open)]`.
 */
@Component({
  selector: 'app-sheet',
  template: `
    <dialog #dialog class="sheet" [attr.aria-labelledby]="headingId" (close)="open.set(false)">
      @if (open()) {
        <div class="sheet__body">
          <h2 class="sheet__title" [id]="headingId">{{ heading() }}</h2>
          <ng-content />
        </div>
      }
    </dialog>
  `,
})
export class Sheet {
  readonly heading = input.required<string>();
  readonly open = model(false);

  protected readonly headingId = `sheet-title-${nextId++}`;
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const dialog = this.dialog().nativeElement;
      if (this.open() && !dialog.open) dialog.showModal();
      if (!this.open() && dialog.open) dialog.close();
    });
  }
}
