import { computed, Directive, inject, input, Component } from '@angular/core';
import { FieldTree, FormField, ValidationError } from '@angular/forms/signals';

const defaultMessages: Record<string, string> = {
  required: 'This field is required.',
  email: 'Enter a valid email address.',
  min: 'The value is too small.',
  max: 'The value is too large.',
  minLength: 'This is too short.',
  maxLength: 'This is too long.',
  pattern: 'The format is not valid.',
  parse: 'Enter a valid value.',
};

function messageOf(error: ValidationError): string {
  return error.message ?? defaultMessages[error.kind] ?? 'This value is not valid.';
}

/** Shows when a field has been touched (or submitted) and is invalid. */
function showErrors(state: { touched(): boolean; invalid(): boolean }): boolean {
  return state.touched() && state.invalid();
}

/**
 * Client and server errors of one Signal Forms field. Give it the input's id as `for`;
 * the input then points at it through `aria-describedby` (see `FieldA11y`).
 */
@Component({
  selector: 'app-field-errors',
  template: `
    @if (visible()) {
      <p class="field-error" [id]="for() + '-errors'">
        @for (message of messages(); track message) {
          <span>{{ message }}</span>
        }
      </p>
    }
  `,
  styles: `
    .field-error {
      display: grid;
      font-size: 0.8125rem;
      color: var(--danger);
    }
  `,
})
export class FieldErrors {
  readonly field = input.required<FieldTree<unknown>>();
  readonly for = input.required<string>();

  protected readonly visible = computed(() => showErrors(this.field()()));
  protected readonly messages = computed(() => [
    ...new Set(this.field()().errors().map(messageOf)),
  ]);
}

/**
 * Adds `aria-invalid` and `aria-describedby` to every `[formField]` control that has an id,
 * matching what `<app-field-errors for="id">` renders.
 */
@Directive({
  selector: '[formField][id]',
  host: {
    '[attr.aria-invalid]': 'invalid() ? "true" : null',
    '[attr.aria-describedby]': 'invalid() ? id() + "-errors" : null',
  },
})
export class FieldA11y {
  private readonly formField = inject(FormField);
  readonly id = input.required<string>();
  protected readonly invalid = computed(() => showErrors(this.formField.state()));
}

/** Everything a form template needs: `[formField]`, accessible error wiring, `<app-field-errors>`. */
export const FIELD_DIRECTIVES = [FormField, FieldA11y, FieldErrors] as const;
