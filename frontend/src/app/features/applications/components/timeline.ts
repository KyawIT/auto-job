import { Component, computed, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { label } from '../../../shared/labels';
import { ApplicationsApi } from '../applications.api';
import { ApplicationDetails } from '../applications.models';

/** What happened to this application, newest first, plus a quick note field. */
@Component({
  selector: 'app-timeline',
  imports: [DatePipe],
  template: `
    <form class="note" (submit)="$event.preventDefault(); add()">
      <label class="visually-hidden" for="tl-note">Add a note</label>
      <input
        class="input"
        id="tl-note"
        placeholder="Add a note…"
        [value]="note()"
        (input)="note.set($any($event.target).value)"
      />
      <button type="submit" class="btn btn--small" [disabled]="!note().trim() || saving()">
        Add
      </button>
    </form>
    <ol class="events">
      @for (e of events(); track $index) {
        <li>
          <span class="dot" aria-hidden="true"></span>
          <div>
            <p class="type">{{ label(e.type) }}</p>
            @if (e.note) {
              <p class="text">{{ e.note }}</p>
            }
            <p class="caption">
              <time [attr.datetime]="e.occurredAt">{{ e.occurredAt | date: 'medium' }}</time>
            </p>
          </div>
        </li>
      }
    </ol>
  `,
  styles: `
    :host {
      display: grid;
      gap: 1rem;
    }
    .note {
      display: flex;
      gap: 0.5rem;
    }
    .events {
      display: grid;
      gap: 0.875rem;
      margin: 0;
      padding: 0;
      list-style: none;
    }
    li {
      display: flex;
      gap: 0.75rem;
    }
    .dot {
      flex: none;
      width: 0.5rem;
      height: 0.5rem;
      margin-top: 0.45rem;
      border-radius: 50%;
      background: var(--accent-fill);
    }
    .type {
      font-size: 0.875rem;
      font-weight: 600;
    }
    .text {
      font-size: 0.875rem;
      white-space: pre-line;
    }
  `,
})
export class Timeline {
  private readonly api = inject(ApplicationsApi);

  readonly application = input.required<ApplicationDetails>();
  readonly changed = output<ApplicationDetails>();

  protected readonly events = computed(() =>
    [...this.application().events].sort((a, b) => b.occurredAt.localeCompare(a.occurredAt)),
  );
  protected readonly note = signal('');
  protected readonly saving = signal(false);
  protected readonly label = label;

  protected async add(): Promise<void> {
    const note = this.note().trim();
    if (!note) return;
    this.saving.set(true);
    try {
      this.changed.emit(await this.api.addNote(this.application().id, note));
      this.note.set('');
    } catch {
      // toast
    } finally {
      this.saving.set(false);
    }
  }
}
