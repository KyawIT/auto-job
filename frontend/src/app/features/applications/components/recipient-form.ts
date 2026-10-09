import { Component, computed, inject, input, linkedSignal, output } from '@angular/core';
import { email, form, FormRoot, required } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { LETTER_LANGUAGES, orNull } from '../../../shared/labels';
import { ApplicationsApi } from '../applications.api';
import { ApplicationDetails } from '../applications.models';

/** Who the letter goes to and in which language. */
@Component({
  selector: 'app-recipient-form',
  imports: [FormRoot, ...FIELD_DIRECTIVES],
  template: `
    <form [formRoot]="form" class="form-stack">
      <div class="field">
        <label class="field-label" for="ap-language">Letter language</label>
        <select class="select" id="ap-language" [formField]="form.language">
          @for (l of languages(); track l.code) {
            <option [value]="l.code">{{ l.name }}</option>
          }
        </select>
        <app-field-errors [field]="form.language" for="ap-language" />
      </div>
      <div class="field">
        <label class="field-label" for="ap-recipient-name">Contact person</label>
        <input
          class="input"
          id="ap-recipient-name"
          placeholder="Hiring team"
          [formField]="form.recipientName"
        />
      </div>
      <div class="field">
        <label class="field-label" for="ap-recipient-email">Send to (email)</label>
        <input
          class="input"
          id="ap-recipient-email"
          type="email"
          [formField]="form.recipientEmail"
        />
        <app-field-errors [field]="form.recipientEmail" for="ap-recipient-email" />
      </div>
      <div class="field">
        <label class="field-label" for="ap-notes">Private notes</label>
        <textarea class="textarea" id="ap-notes" rows="3" [formField]="form.notes"></textarea>
      </div>
      <div class="form-actions">
        <button
          type="submit"
          class="btn btn--small btn--primary"
          [disabled]="form().submitting() || !form().dirty()"
        >
          Save
        </button>
      </div>
    </form>
  `,
})
export class RecipientForm {
  private readonly api = inject(ApplicationsApi);

  readonly application = input.required<ApplicationDetails>();
  readonly saved = output<ApplicationDetails>();

  /** The posting's detected language may not be one of ours; keep it selectable. */
  protected readonly languages = computed(() => {
    const code = this.application().language;
    const known = LETTER_LANGUAGES.some((l) => l.code === code);
    return known ? LETTER_LANGUAGES : [...LETTER_LANGUAGES, { code, name: code.toUpperCase() }];
  });

  protected readonly model = linkedSignal(() => {
    const a = this.application();
    return {
      language: a.language,
      recipientName: a.recipientName ?? '',
      recipientEmail: a.recipientEmail ?? '',
      notes: a.notes ?? '',
    };
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.language);
      email(p.recipientEmail);
    },
    {
      submission: {
        action: async (f) => {
          const v = f().value();
          try {
            const details = await this.api.update(this.application().id, {
              language: v.language,
              recipientName: orNull(v.recipientName),
              recipientEmail: orNull(v.recipientEmail),
              notes: orNull(v.notes),
            });
            this.saved.emit(details);
            return undefined;
          } catch (error) {
            return serverErrors(f, error);
          }
        },
      },
    },
  );
}
