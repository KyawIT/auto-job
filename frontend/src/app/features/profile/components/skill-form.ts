import { Component, inject, input, linkedSignal, output } from '@angular/core';
import { form, FormRoot, required } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { ProfileApi } from '../profile.api';
import { Skill, SKILL_LEVELS, SkillLevel } from '../profile.models';

@Component({
  selector: 'app-skill-form',
  imports: [FormRoot, ...FIELD_DIRECTIVES],
  template: `
    <form [formRoot]="form" class="form-stack">
      <div class="form-grid">
        <div class="field">
          <label class="field-label" for="sk-name">Skill</label>
          <input class="input" id="sk-name" placeholder="e.g. TypeScript" [formField]="form.name" />
          <app-field-errors [field]="form.name" for="sk-name" />
        </div>
        <div class="field">
          <label class="field-label" for="sk-level"
            >Level <span class="muted">(optional)</span></label
          >
          <select class="select" id="sk-level" [formField]="form.level">
            <option value="">Not specified</option>
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
export class SkillForm {
  private readonly api = inject(ProfileApi);
  readonly entry = input<Skill | null>(null);
  readonly saved = output();
  readonly cancelled = output();
  protected readonly levels = SKILL_LEVELS;

  protected readonly model = linkedSignal(() => ({
    name: this.entry()?.name ?? '',
    level: this.entry()?.level ?? '',
  }));

  protected readonly form = form(this.model, (p) => required(p.name), {
    submission: {
      action: async (f) => {
        const v = f().value();
        const request = { name: v.name, level: (v.level || null) as SkillLevel | null };
        try {
          const id = this.entry()?.id;
          await (id
            ? this.api.updateEntry('skills', id, request)
            : this.api.addEntry('skills', request));
          this.saved.emit();
          return undefined;
        } catch (error) {
          return serverErrors(f, error);
        }
      },
    },
  });
}
