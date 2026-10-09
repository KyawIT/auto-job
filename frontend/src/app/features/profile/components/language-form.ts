import { Component, inject, input, linkedSignal, output } from '@angular/core';
import { form, FormRoot, required } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { ProfileApi } from '../profile.api';
import { Language, LANGUAGE_LEVELS, LanguageLevel } from '../profile.models';

@Component({
  selector: 'app-language-form',
  imports: [FormRoot, ...FIELD_DIRECTIVES],
  template: `
    <form [formRoot]="form" class="form-stack">
      <div class="form-grid">
        <div class="field">
          <label class="field-label" for="la-name">Language</label>
          <input class="input" id="la-name" placeholder="e.g. German" [formField]="form.name" />
          <app-field-errors [field]="form.name" for="la-name" />
        </div>
        <div class="field">
          <label class="field-label" for="la-level">Level</label>
          <select class="select" id="la-level" [formField]="form.level">
            @for (level of levels; track level) {
              <option [value]="level">{{ level }}</option>
            }
          </select>
        </div>
      </div>
      <div class="form-actions">
        <button type="button" class="btn" (click)="cancelled.emit()">Cancel</button>
        <button type="submit" class="btn btn--primary" [disabled]="form().submitting()">
          Save
        </button>
      </div>
    </form>
  `,
})
export class LanguageForm {
  private readonly api = inject(ProfileApi);
  readonly entry = input<Language | null>(null);
  readonly saved = output();
  readonly cancelled = output();
  protected readonly levels = LANGUAGE_LEVELS;

  protected readonly model = linkedSignal(() => ({
    name: this.entry()?.name ?? '',
    level: (this.entry()?.level ?? 'B2') as LanguageLevel,
  }));

  protected readonly form = form(this.model, (p) => required(p.name), {
    submission: {
      action: async (f) => {
        const request = f().value();
        try {
          const id = this.entry()?.id;
          await (id
            ? this.api.updateEntry('languages', id, request)
            : this.api.addEntry('languages', request));
          this.saved.emit();
          return undefined;
        } catch (error) {
          return serverErrors(f, error);
        }
      },
    },
  });
}
