import { Component, computed, inject, Service, signal } from '@angular/core';
import { Sheet } from '../sheet/sheet';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  /** Destructive actions get a red button. */
  danger?: boolean;
}

interface PendingConfirm extends ConfirmOptions {
  resolve: (confirmed: boolean) => void;
}

/** `await confirm.ask({...})` → true if confirmed. Only for destructive or costly actions. */
@Service()
export class Confirm {
  readonly pending = signal<PendingConfirm | null>(null);

  ask(options: ConfirmOptions): Promise<boolean> {
    this.pending()?.resolve(false);
    return new Promise((resolve) => this.pending.set({ ...options, resolve }));
  }

  answer(confirmed: boolean): void {
    this.pending()?.resolve(confirmed);
    this.pending.set(null);
  }
}

/** Rendered once in the shell. */
@Component({
  selector: 'app-confirm-dialog',
  imports: [Sheet],
  template: `
    <app-sheet
      [heading]="request()?.title ?? ''"
      [open]="!!request()"
      (openChange)="!$event && confirm.answer(false)"
    >
      @if (request(); as r) {
        <p>{{ r.message }}</p>
        <div class="form-actions">
          <button type="button" class="btn" (click)="confirm.answer(false)">Cancel</button>
          <button
            type="button"
            class="btn"
            [class.btn--danger]="r.danger"
            [class.btn--primary]="!r.danger"
            (click)="confirm.answer(true)"
          >
            {{ r.confirmLabel ?? 'Confirm' }}
          </button>
        </div>
      }
    </app-sheet>
  `,
})
export class ConfirmDialog {
  protected readonly confirm = inject(Confirm);
  protected readonly request = computed(() => this.confirm.pending());
}
