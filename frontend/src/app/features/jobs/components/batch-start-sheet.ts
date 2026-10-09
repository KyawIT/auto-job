import { Component, inject, input, model, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { LETTER_LANGUAGES } from '../../../shared/labels';
import { Sheet } from '../../../shared/sheet/sheet';
import { BatchesApi } from '../../batches/batches.api';
import { SettingsApi } from '../../settings/settings.api';

/** Confirms a batch: language choice and an honest "this makes N AI requests". */
@Component({
  selector: 'app-batch-start-sheet',
  imports: [Sheet, RouterLink],
  template: `
    <app-sheet heading="Generate cover letters" [(open)]="open">
      <p>
        A cover letter, email subject and email body are written for
        <strong>{{ jobIds().length }}</strong> {{ jobIds().length === 1 ? 'job' : 'jobs' }}. This
        runs in the background while you keep working.
      </p>
      <div class="field">
        <label class="field-label" for="batch-language">Letter language</label>
        <select
          class="select"
          id="batch-language"
          [value]="language()"
          (change)="language.set($any($event.target).value)"
        >
          <option value="">Language of each posting</option>
          @for (l of languages; track l.code) {
            <option [value]="l.code">{{ l.name }}</option>
          }
        </select>
      </div>
      <p class="notice">
        <span aria-hidden="true">ⓘ</span>
        <span>
          This makes
          <strong
            >{{ jobIds().length }} AI {{ jobIds().length === 1 ? 'request' : 'requests' }}</strong
          >
          to NVIDIA. Your profile and each job posting are sent to the AI service.
        </span>
      </p>
      @if (settings.hasValue() && !settings.value().nimApiKeyConfigured) {
        <p class="notice notice--warning" role="alert">
          <span aria-hidden="true">!</span>
          <span
            >No NVIDIA API key is set yet.
            <a routerLink="/settings" (click)="open.set(false)">Add it in Settings</a> first.</span
          >
        </p>
      }
      <div class="form-actions">
        <button type="button" class="btn" (click)="open.set(false)">Cancel</button>
        <button
          type="button"
          class="btn btn--primary"
          [disabled]="starting() || !jobIds().length"
          (click)="start()"
        >
          {{
            starting()
              ? 'Starting…'
              : 'Start ' + jobIds().length + ' ' + (jobIds().length === 1 ? 'letter' : 'letters')
          }}
        </button>
      </div>
    </app-sheet>
  `,
})
export class BatchStartSheet {
  private readonly batches = inject(BatchesApi);
  private readonly router = inject(Router);

  readonly open = model(false);
  readonly jobIds = input.required<number[]>();

  protected readonly settings = inject(SettingsApi).settingsResource();
  protected readonly languages = LETTER_LANGUAGES;
  protected readonly language = signal('');
  protected readonly starting = signal(false);

  protected async start(): Promise<void> {
    this.starting.set(true);
    try {
      const batch = await this.batches.start(this.jobIds(), this.language() || null);
      this.open.set(false);
      void this.router.navigate(['/batches', batch.id]);
    } catch {
      // shown as a toast
    } finally {
      this.starting.set(false);
    }
  }
}
