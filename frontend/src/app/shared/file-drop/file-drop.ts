import { Component, input, output, signal } from '@angular/core';

/** Drop zone plus a regular file picker button (the keyboard path). Emits the chosen files. */
@Component({
  selector: 'app-file-drop',
  template: `
    <div class="symbol" aria-hidden="true">⇪</div>
    <p><strong>Drop files here</strong> <span class="muted">or</span></p>
    <label class="btn btn--small">
      Choose files…
      <input
        class="visually-hidden"
        type="file"
        [accept]="accept()"
        [multiple]="multiple()"
        [disabled]="disabled()"
        (change)="picked($event)"
      />
    </label>
    @if (hint()) {
      <p class="caption">{{ hint() }}</p>
    }
  `,
  host: {
    '[class.over]': 'over()',
    '(dragenter)': 'enter($event)',
    '(dragover)': 'enter($event)',
    '(dragleave)': 'over.set(false)',
    '(drop)': 'drop($event)',
  },
  styles: `
    :host {
      display: grid;
      justify-items: center;
      gap: 0.5rem;
      padding: 1.75rem 1rem;
      border: 1.5px dashed var(--border-strong);
      border-radius: var(--radius-lg);
      background: var(--surface);
      text-align: center;
      transition:
        transform var(--spring-duration) var(--spring),
        border-color 150ms ease,
        background-color 150ms ease;
    }
    :host(.over) {
      border-color: var(--accent);
      background: var(--accent-soft);
      transform: scale(1.01);
    }
    .symbol {
      font-size: 1.5rem;
      color: var(--accent);
    }
    label:focus-within {
      box-shadow: var(--focus-ring);
    }
    @media (prefers-reduced-motion: reduce) {
      :host(.over) {
        transform: none;
      }
    }
  `,
})
export class FileDrop {
  readonly accept = input('');
  readonly multiple = input(true);
  readonly disabled = input(false);
  readonly hint = input<string>();
  readonly files = output<File[]>();

  protected readonly over = signal(false);

  protected enter(event: DragEvent): void {
    if (this.disabled()) return;
    event.preventDefault();
    this.over.set(true);
  }

  protected drop(event: DragEvent): void {
    event.preventDefault();
    this.over.set(false);
    if (this.disabled()) return;
    const files = Array.from(event.dataTransfer?.files ?? []);
    if (files.length) this.files.emit(this.multiple() ? files : files.slice(0, 1));
  }

  protected picked(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (files.length) this.files.emit(files);
  }
}
