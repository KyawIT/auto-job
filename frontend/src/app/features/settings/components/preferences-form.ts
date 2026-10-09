import { Component, inject, input, linkedSignal } from '@angular/core';
import { form, FormRoot, max, min, required } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { Notifications } from '../../../core/notifications';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { label, LETTER_LANGUAGES, orNull } from '../../../shared/labels';
import { SettingsApi } from '../settings.api';
import { LETTER_LAYOUTS, MAX_PARALLEL_GENERATIONS, Settings } from '../settings.models';

@Component({
  selector: 'app-preferences-form',
  imports: [FormRoot, ...FIELD_DIRECTIVES],
  templateUrl: './preferences-form.html',
})
export class PreferencesForm {
  private readonly api = inject(SettingsApi);
  private readonly notifications = inject(Notifications);

  readonly settings = input.required<Settings>();

  protected readonly languages = LETTER_LANGUAGES;
  protected readonly layouts = LETTER_LAYOUTS;
  protected readonly label = label;
  protected readonly maxParallel = MAX_PARALLEL_GENERATIONS;

  protected readonly model = linkedSignal(() => {
    const s = this.settings();
    return {
      defaultLetterLanguage: s.defaultLetterLanguage,
      nimModel: s.nimModel ?? '',
      letterLayout: s.letterLayout,
      maxParallelGenerations: s.maxParallelGenerations as number | null,
      defaultSearchLocation: s.defaultSearchLocation ?? '',
      defaultSearchRadiusKm: s.defaultSearchRadiusKm,
    };
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.defaultLetterLanguage);
      required(p.maxParallelGenerations);
      min(p.maxParallelGenerations, 1);
      max(p.maxParallelGenerations, MAX_PARALLEL_GENERATIONS);
      min(p.defaultSearchRadiusKm, 0);
    },
    {
      submission: {
        action: async (f) => {
          const v = f().value();
          try {
            await this.api.update({
              defaultLetterLanguage: v.defaultLetterLanguage,
              nimModel: orNull(v.nimModel),
              letterLayout: v.letterLayout,
              maxParallelGenerations: v.maxParallelGenerations ?? 1,
              defaultSearchLocation: orNull(v.defaultSearchLocation),
              defaultSearchRadiusKm: v.defaultSearchRadiusKm,
            });
            f().reset();
            this.notifications.success('Settings saved.');
            return undefined;
          } catch (error) {
            return serverErrors(f, error);
          }
        },
      },
    },
  );
}
