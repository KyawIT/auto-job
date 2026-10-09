import { Component, inject, input, linkedSignal, output } from '@angular/core';
import { form, FormRoot, required } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { orNull } from '../../../shared/labels';
import { ProfileApi } from '../profile.api';
import { WorkExperience } from '../profile.models';

@Component({
  selector: 'app-work-experience-form',
  imports: [FormRoot, ...FIELD_DIRECTIVES],
  template: `
    <form [formRoot]="form" class="form-stack">
      <div class="form-grid">
        <div class="field">
          <label class="field-label" for="we-role">Role</label>
          <input class="input" id="we-role" [formField]="form.role" />
          <app-field-errors [field]="form.role" for="we-role" />
        </div>
        <div class="field">
          <label class="field-label" for="we-company">Company</label>
          <input
            class="input"
            id="we-company"
            autocomplete="organization"
            [formField]="form.company"
          />
          <app-field-errors [field]="form.company" for="we-company" />
        </div>
        <div class="field span-all">
          <label class="field-label" for="we-location"
            >Location <span class="muted">(optional)</span></label
          >
          <input class="input" id="we-location" [formField]="form.location" />
        </div>
        <div class="field">
          <label class="field-label" for="we-start">Start date</label>
          <input class="input" id="we-start" type="date" [formField]="form.startDate" />
          <app-field-errors [field]="form.startDate" for="we-start" />
        </div>
        <div class="field">
          <label class="field-label" for="we-end"
            >End date <span class="muted">(empty = current)</span></label
          >
          <input class="input" id="we-end" type="date" [formField]="form.endDate" />
          <app-field-errors [field]="form.endDate" for="we-end" />
        </div>
        <div class="field span-all">
          <label class="field-label" for="we-description"
            >What you did <span class="muted">(optional)</span></label
          >
          <textarea class="textarea" id="we-description" [formField]="form.description"></textarea>
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
export class WorkExperienceForm {
  private readonly api = inject(ProfileApi);
  readonly entry = input<WorkExperience | null>(null);
  readonly saved = output();
  readonly cancelled = output();

  protected readonly model = linkedSignal(() => {
    const e = this.entry();
    return {
      company: e?.company ?? '',
      role: e?.role ?? '',
      location: e?.location ?? '',
      startDate: e?.startDate ?? '',
      endDate: e?.endDate ?? '',
      description: e?.description ?? '',
    };
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.company);
      required(p.role);
      required(p.startDate);
    },
    {
      submission: {
        action: async (f) => {
          const v = f().value();
          const request = {
            ...v,
            location: orNull(v.location),
            endDate: orNull(v.endDate),
            description: orNull(v.description),
          };
          try {
            const id = this.entry()?.id;
            await (id
              ? this.api.updateEntry('work-experiences', id, request)
              : this.api.addEntry('work-experiences', request));
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
