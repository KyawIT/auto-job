import { Component, inject, linkedSignal, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { form, FormRoot, min, required, validate } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { label, orNull } from '../../../shared/labels';
import { PageHeader } from '../../../shared/page-header/page-header';
import { Sheet } from '../../../shared/sheet/sheet';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { JobsApi } from '../../jobs/jobs.api';
import { JobSource, SearchJobsRequest, SearchResult } from '../../jobs/jobs.models';
import { SettingsApi } from '../../settings/settings.api';
import { SavedSearchDraft, SavedSearchForm } from '../components/saved-search-form';

@Component({
  selector: 'app-search-page',
  imports: [
    DatePipe,
    RouterLink,
    FormRoot,
    PageHeader,
    Sheet,
    StatusBadge,
    SavedSearchForm,
    ...FIELD_DIRECTIVES,
  ],
  templateUrl: './search-page.html',
  styleUrl: './search-page.scss',
})
export default class SearchPage {
  private readonly api = inject(JobsApi);
  private readonly settings = inject(SettingsApi).settingsResource();

  protected readonly label = label;
  protected readonly result = signal<SearchResult | null>(null);
  protected readonly lastRequest = signal<SearchJobsRequest | null>(null);
  protected readonly saveDraft = signal<SavedSearchDraft | null>(null);

  // Location and radius start from the defaults in Settings.
  protected readonly model = linkedSignal(() => {
    const s = this.settings.hasValue() ? this.settings.value() : null;
    return {
      keywords: '',
      location: s?.defaultSearchLocation ?? '',
      radiusKm: s?.defaultSearchRadiusKm ?? null,
      bundesagentur: true,
      adzuna: !!s?.adzunaConfigured,
    };
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.keywords);
      min(p.radiusKm, 0);
      validate(p.adzuna, ({ value, valueOf }) =>
        !value() && !valueOf(p.bundesagentur)
          ? { kind: 'sources', message: 'Select at least one source.' }
          : undefined,
      );
    },
    {
      submission: {
        action: async (f) => {
          const v = f().value();
          const request: SearchJobsRequest = {
            keywords: v.keywords.trim(),
            location: orNull(v.location),
            radiusKm: v.radiusKm,
            sources: [
              ...(v.bundesagentur ? ['BundesagenturFuerArbeit' as JobSource] : []),
              ...(v.adzuna ? ['Adzuna' as JobSource] : []),
            ],
          };
          try {
            this.result.set(await this.api.search(request));
            this.lastRequest.set(request);
            return undefined;
          } catch (error) {
            return serverErrors(f, error);
          }
        },
      },
    },
  );

  protected saveSearch(): void {
    const request = this.lastRequest();
    if (request) this.saveDraft.set({ ...request, name: request.keywords });
  }
}
