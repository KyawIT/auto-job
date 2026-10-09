import { Component, inject, input, linkedSignal, output } from '@angular/core';
import { form, FormRoot, min, required, validate } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { label, orNull } from '../../../shared/labels';
import { JobsApi } from '../../jobs/jobs.api';
import { SavedSearch, SaveSearchRequest } from '../../jobs/jobs.models';

export type SavedSearchDraft = Partial<SaveSearchRequest> & { id?: number };

const intervals = [
  { minutes: 60, label: 'Every hour' },
  { minutes: 180, label: 'Every 3 hours' },
  { minutes: 720, label: 'Twice a day' },
  { minutes: 1440, label: 'Once a day' },
];

/** Create or edit a saved search; it refreshes in the background and new jobs appear in the list. */
@Component({
  selector: 'app-saved-search-form',
  imports: [FormRoot, ...FIELD_DIRECTIVES],
  templateUrl: './saved-search-form.html',
})
export class SavedSearchForm {
  private readonly api = inject(JobsApi);

  readonly draft = input<SavedSearchDraft>({});
  readonly saved = output<SavedSearch>();
  readonly cancelled = output();

  protected readonly intervals = intervals;
  protected readonly label = label;

  protected readonly model = linkedSignal(() => {
    const d = this.draft();
    const sources = d.sources ?? ['BundesagenturFuerArbeit'];
    return {
      name: d.name ?? d.keywords ?? '',
      keywords: d.keywords ?? '',
      location: d.location ?? '',
      radiusKm: d.radiusKm ?? null,
      bundesagentur: sources.includes('BundesagenturFuerArbeit'),
      adzuna: sources.includes('Adzuna'),
      refreshIntervalMinutes: String(d.refreshIntervalMinutes ?? 180),
      isActive: d.isActive ?? true,
    };
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name);
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
          const request: SaveSearchRequest = {
            name: v.name.trim(),
            keywords: v.keywords.trim(),
            location: orNull(v.location),
            radiusKm: v.radiusKm,
            sources: [
              ...(v.bundesagentur ? ['BundesagenturFuerArbeit' as const] : []),
              ...(v.adzuna ? ['Adzuna' as const] : []),
            ],
            refreshIntervalMinutes: Number(v.refreshIntervalMinutes),
            isActive: v.isActive,
          };
          try {
            const id = this.draft().id;
            const saved = id
              ? await this.api.updateSavedSearch(id, request)
              : await this.api.createSavedSearch(request);
            this.saved.emit(saved);
            return undefined;
          } catch (error) {
            return serverErrors(f, error);
          }
        },
      },
    },
  );
}
