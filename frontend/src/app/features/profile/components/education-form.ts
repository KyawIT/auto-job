import { Component, inject, input, linkedSignal, output } from '@angular/core';
import { form, FormRoot, required } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { orNull } from '../../../shared/labels';
import { ProfileApi } from '../profile.api';
import { Education } from '../profile.models';

@Component({
  selector: 'app-education-form',
  imports: [FormRoot, ...FIELD_DIRECTIVES],
  template: `
    <form [formRoot]="form" class="form-stack">
      <div class="form-grid">
        <div class="field">
          <label class="field-label" for="ed-degree">Degree</label>
          <input class="input" id="ed-degree" placeholder="e.g. B.Sc." [formField]="form.degree" />
          <app-field-errors [field]="form.degree" for="ed-degree" />
        </div>
        <div class="field">
          <label class="field-label" for="ed-institution">Institution</label>
          <input class="input" id="ed-institution" [formField]="form.institution" />
          <app-field-errors [field]="form.institution" for="ed-institution" />
        </div>
        <div class="field">
          <label class="field-label" for="ed-field"
            >Field of study <span class="muted">(optional)</span></label
          >
          <input class="input" id="ed-field" [formField]="form.fieldOfStudy" />
        </div>
        <div class="field">
          <label class="field-label" for="ed-grade"
            >Grade <span class="muted">(optional)</span></label
          >
          <input class="input" id="ed-grade" [formField]="form.grade" />
        </div>
        <div class="field">
          <label class="field-label" for="ed-start"
            >Start date <span class="muted">(optional)</span></label
          >
          <input class="input" id="ed-start" type="date" [formField]="form.startDate" />
        </div>
        <div class="field">
          <label class="field-label" for="ed-end"
            >End date <span class="muted">(optional)</span></label
          >
          <input class="input" id="ed-end" type="date" [formField]="form.endDate" />
          <app-field-errors [field]="form.endDate" for="ed-end" />
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
export class EducationForm {
  private readonly api = inject(ProfileApi);
  readonly entry = input<Education | null>(null);
  readonly saved = output();
  readonly cancelled = output();

  protected readonly model = linkedSignal(() => {
    const e = this.entry();
    return {
      institution: e?.institution ?? '',
      degree: e?.degree ?? '',
      fieldOfStudy: e?.fieldOfStudy ?? '',
      grade: e?.grade ?? '',
      startDate: e?.startDate ?? '',
      endDate: e?.endDate ?? '',
    };
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.institution);
      required(p.degree);
    },
    {
      submission: {
        action: async (f) => {
          const v = f().value();
          const request = {
            institution: v.institution,
            degree: v.degree,
            fieldOfStudy: orNull(v.fieldOfStudy),
            grade: orNull(v.grade),
            startDate: orNull(v.startDate),
            endDate: orNull(v.endDate),
          };
          try {
            const id = this.entry()?.id;
            await (id
              ? this.api.updateEntry('educations', id, request)
              : this.api.addEntry('educations', request));
            this.saved.emit();
            return undefined;
          } catch (error) {
            return serverErrors(f, error);
          }
        },
      },
    },
  );
}
