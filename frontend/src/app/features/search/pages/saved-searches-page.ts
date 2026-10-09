import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { label } from '../../../shared/labels';
import { PageHeader } from '../../../shared/page-header/page-header';
import { Sheet } from '../../../shared/sheet/sheet';
import { JobsApi } from '../../jobs/jobs.api';
import { SavedSearch } from '../../jobs/jobs.models';
import { SavedSearchDraft, SavedSearchForm } from '../components/saved-search-form';

@Component({
  selector: 'app-saved-searches-page',
  imports: [DatePipe, PageHeader, EmptyState, Sheet, SavedSearchForm],
  template: `
    <app-page-header
      heading="Saved searches"
      subtitle="Checked in the background while the app is open. New jobs show up in your list."
      [back]="{ label: 'Search', link: '/search' }"
    >
      <button type="button" class="btn btn--primary" (click)="editing.set({})">
        New saved search
      </button>
    </app-page-header>

    @if (searches.hasValue() && searches.value().length) {
      <ul class="list">
        @for (s of searches.value(); track s.id) {
          <li class="list-row">
            <div class="list-row__main">
              <p class="list-row__title">
                {{ s.name }}
                @if (!s.isActive) {
                  <span class="muted"> · paused</span>
                }
              </p>
              <p class="list-row__meta">
                “{{ s.keywords }}”
                @if (s.location) {
                  in {{ s.location }}
                }
                @if (s.radiusKm) {
                  (+{{ s.radiusKm }} km)
                }
                · {{ sources(s) }} ·
                {{
                  s.lastRunAt ? 'last checked ' + (s.lastRunAt | date: 'short') : 'not checked yet'
                }}
              </p>
            </div>
            <button
              type="button"
              class="btn btn--small btn--plain"
              (click)="toggle(s)"
              [attr.aria-label]="(s.isActive ? 'Pause ' : 'Resume ') + s.name"
            >
              {{ s.isActive ? 'Pause' : 'Resume' }}
            </button>
            <button
              type="button"
              class="btn btn--small btn--plain"
              (click)="editing.set(s)"
              [attr.aria-label]="'Edit ' + s.name"
            >
              Edit
            </button>
            <button
              type="button"
              class="btn btn--small btn--danger-plain"
              (click)="remove(s)"
              [attr.aria-label]="'Delete ' + s.name"
            >
              Delete
            </button>
          </li>
        }
      </ul>
    } @else if (searches.hasValue()) {
      <app-empty-state
        symbol="↻"
        heading="No saved searches"
        message="Save a search to check it for new postings automatically."
      >
        <button type="button" class="btn btn--primary" (click)="editing.set({})">
          New saved search
        </button>
      </app-empty-state>
    } @else if (searches.error()) {
      <app-empty-state symbol="!" heading="Saved searches not available">
        <button type="button" class="btn" (click)="searches.reload()">Try again</button>
      </app-empty-state>
    } @else {
      <div class="card" role="status" aria-label="Loading saved searches">
        <span class="skeleton"></span>
      </div>
    }

    <app-sheet
      [heading]="editing()?.id ? 'Edit saved search' : 'New saved search'"
      [open]="!!editing()"
      (openChange)="!$event && editing.set(null)"
    >
      @if (editing(); as d) {
        <app-saved-search-form [draft]="d" (saved)="saved()" (cancelled)="editing.set(null)" />
      }
    </app-sheet>
  `,
})
export default class SavedSearchesPage {
  private readonly api = inject(JobsApi);
  private readonly confirm = inject(Confirm);

  protected readonly searches = this.api.savedSearchesResource();
  protected readonly editing = signal<SavedSearchDraft | null>(null);

  protected sources(search: SavedSearch): string {
    return search.sources.map(label).join(', ');
  }

  protected async toggle(search: SavedSearch): Promise<void> {
    const { name, keywords, location, radiusKm, sources, refreshIntervalMinutes } = search;
    await this.api.updateSavedSearch(search.id, {
      name,
      keywords,
      location,
      radiusKm,
      sources,
      refreshIntervalMinutes,
      isActive: !search.isActive,
    });
    this.searches.reload();
  }

  protected async remove(search: SavedSearch): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete saved search?',
      message: `"${search.name}" stops being checked. Jobs it already found stay in your list.`,
      confirmLabel: 'Delete',
      danger: true,
    });
    if (!ok) return;
    await this.api.deleteSavedSearch(search.id);
    this.searches.reload();
  }

  protected saved(): void {
    this.editing.set(null);
    this.searches.reload();
  }
}
