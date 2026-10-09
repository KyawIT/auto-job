import { Component, inject, input, linkedSignal, output, signal } from '@angular/core';
import { ApplicationsApi } from '../applications.api';
import {
  APPLICATION_STATUSES,
  ApplicationDetails,
  ApplicationStatus,
} from '../applications.models';

/** Moves the application along (sent, interview, offer…), optionally with a note for the timeline. */
@Component({
  selector: 'app-status-changer',
  template: `
    <div class="field">
      <label class="field-label" for="st-status">Status</label>
      <select
        class="select"
        id="st-status"
        [value]="status()"
        (change)="status.set($any($event.target).value)"
      >
        @for (s of statuses; track s) {
          <option [value]="s">{{ s }}</option>
        }
      </select>
    </div>
    @if (status() !== application().status) {
      <div class="field">
        <label class="field-label" for="st-note">Note <span class="muted">(optional)</span></label>
        <input
          class="input"
          id="st-note"
          placeholder="e.g. Interview on Monday"
          [value]="note()"
          (input)="note.set($any($event.target).value)"
        />
      </div>
      <div class="form-actions">
        <button type="button" class="btn btn--small" (click)="status.set(application().status)">
          Cancel
        </button>
        <button
          type="button"
          class="btn btn--small btn--primary"
          [disabled]="saving()"
          (click)="save()"
        >
          Change status
        </button>
      </div>
    }
  `,
  styles: `
    :host {
      display: grid;
      gap: 0.75rem;
    }
  `,
})
export class StatusChanger {
  private readonly api = inject(ApplicationsApi);

  readonly application = input.required<ApplicationDetails>();
  readonly changed = output<ApplicationDetails>();

  protected readonly statuses = APPLICATION_STATUSES;
  protected readonly status = linkedSignal<ApplicationStatus>(() => this.application().status);
  protected readonly note = signal('');
  protected readonly saving = signal(false);

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      const details = await this.api.changeStatus(
        this.application().id,
        this.status(),
        this.note().trim() || null,
      );
      this.note.set('');
      this.changed.emit(details);
    } catch {
      // toast
    } finally {
      this.saving.set(false);
    }
  }
}
