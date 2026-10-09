import { Component, inject, input, linkedSignal, output } from '@angular/core';
import { form, FormRoot, required } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { label } from '../../../shared/labels';
import { DocumentsApi } from '../documents.api';
import { AppDocument, DOCUMENT_TYPES } from '../documents.models';

@Component({
  selector: 'app-document-edit-form',
  imports: [FormRoot, ...FIELD_DIRECTIVES],
  template: `
    <form [formRoot]="form" class="form-stack">
      <div class="field">
        <label class="field-label" for="doc-name">Name</label>
        <input class="input" id="doc-name" [formField]="form.displayName" />
        <app-field-errors [field]="form.displayName" for="doc-name" />
      </div>
      <div class="field">
        <label class="field-label" for="doc-type">Type</label>
        <select class="select" id="doc-type" [formField]="form.type">
          @for (t of types; track t) {
            <option [value]="t">{{ label(t) }}</option>
          }
        </select>
      </div>
      <label class="check">
        <input type="checkbox" [formField]="form.attachByDefault" />
        Attach to new applications
      </label>
      <div class="form-actions">
        <button type="button" class="btn" (click)="cancelled.emit()">Cancel</button>
        <button type="submit" class="btn btn--primary" [disabled]="form().submitting()">
          Save
        </button>
      </div>
    </form>
  `,
})
export class DocumentEditForm {
  private readonly api = inject(DocumentsApi);
  readonly document = input.required<AppDocument>();
  readonly saved = output();
  readonly cancelled = output();

  protected readonly types = DOCUMENT_TYPES;
  protected readonly label = label;

  protected readonly model = linkedSignal(() => {
    const d = this.document();
    return { displayName: d.displayName, type: d.type, attachByDefault: d.attachByDefault };
  });

  protected readonly form = form(this.model, (p) => required(p.displayName), {
    submission: {
      action: async (f) => {
        try {
          await this.api.update(this.document().id, f().value());
          this.saved.emit();
          return undefined;
        } catch (error) {
          return serverErrors(f, error);
        }
      },
    },
  });
}
