import { Component, input } from '@angular/core';

/** Friendly placeholder for empty lists and not-found pages; put the next step in the content. */
@Component({
  selector: 'app-empty-state',
  template: `
    <div class="symbol" aria-hidden="true">{{ symbol() }}</div>
    <h2 class="title-2">{{ heading() }}</h2>
    @if (message()) {
      <p class="muted">{{ message() }}</p>
    }
    <div class="btn-row"><ng-content /></div>
  `,
  styles: `
    :host {
      display: grid;
      justify-items: center;
      gap: 0.5rem;
      padding: 3rem 1.5rem;
      text-align: center;
      border: 1px dashed var(--border-strong);
      border-radius: var(--radius-lg);
    }
    .symbol {
      display: grid;
      place-items: center;
      width: 3rem;
      height: 3rem;
      margin-bottom: 0.25rem;
      border-radius: 50%;
      background: var(--accent-soft);
      color: var(--accent);
      font-size: 1.375rem;
    }
    p {
      max-width: 28rem;
    }
    .btn-row {
      margin-top: 0.5rem;
      justify-content: center;
    }
  `,
})
export class EmptyState {
  readonly heading = input.required<string>();
  readonly message = input<string>();
  readonly symbol = input('✦');
}
