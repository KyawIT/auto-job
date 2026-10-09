import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Page title (the page's single h1), optional subtitle and back link, actions on the right. */
@Component({
  selector: 'app-page-header',
  imports: [RouterLink],
  template: `
    @if (back(); as b) {
      <a class="back" [routerLink]="b.link"><span aria-hidden="true">‹</span> {{ b.label }}</a>
    }
    <div class="row">
      <div class="text">
        <h1 class="title-1" tabindex="-1">{{ heading() }}</h1>
        @if (subtitle()) {
          <p class="muted">{{ subtitle() }}</p>
        }
      </div>
      <div class="btn-row"><ng-content /></div>
    </div>
  `,
  styles: `
    :host {
      display: block;
      margin-bottom: 1.5rem;
    }
    .row {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      justify-content: space-between;
      gap: 1rem;
    }
    .text {
      display: grid;
      gap: 0.25rem;
      min-width: 0;
    }
    h1:focus {
      outline: none;
    }
    .back {
      display: inline-block;
      margin-bottom: 0.5rem;
      font-size: 0.875rem;
    }
  `,
})
export class PageHeader {
  readonly heading = input.required<string>();
  readonly subtitle = input<string | null>();
  readonly back = input<{ label: string; link: string }>();
}
