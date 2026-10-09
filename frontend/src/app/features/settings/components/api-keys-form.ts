import { Component, inject, input, output, signal } from '@angular/core';
import { form, FormRoot } from '@angular/forms/signals';
import { ApiKeys } from '../../../core/desktop';
import { Notifications } from '../../../core/notifications';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { SettingsApi } from '../settings.api';

const empty = { nimApiKey: '', adzunaAppId: '', adzunaAppKey: '' };

/**
 * Write-only key inputs: a saved key is never shown again, only "Configured".
 * Leaving a field empty keeps the stored key; "Remove" deletes it.
 */
@Component({
  selector: 'app-api-keys-form',
  imports: [FormRoot, StatusBadge, ...FIELD_DIRECTIVES],
  templateUrl: './api-keys-form.html',
  styles: `
    .key-label {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.5rem;
    }
  `,
})
export class ApiKeysForm {
  private readonly api = inject(SettingsApi);
  private readonly notifications = inject(Notifications);

  readonly nimConfigured = input(false);
  readonly adzunaConfigured = input(false);
  /** The wizard only asks for the NVIDIA key. */
  readonly showAdzuna = input(true);
  readonly submitLabel = input('Save keys');
  readonly saved = output();

  protected readonly removing = signal(false);
  protected readonly model = signal({ ...empty });
  protected readonly form = form(this.model, {
    submission: {
      action: async (f) => {
        const v = f().value();
        const keys: ApiKeys = {};
        if (v.nimApiKey.trim()) keys.nimApiKey = v.nimApiKey.trim();
        if (v.adzunaAppId.trim()) keys.adzunaAppId = v.adzunaAppId.trim();
        if (v.adzunaAppKey.trim()) keys.adzunaAppKey = v.adzunaAppKey.trim();
        if (Object.keys(keys).length) {
          try {
            await this.api.setApiKeys(keys);
          } catch {
            return []; // already shown as a toast
          }
          this.notifications.success('Keys saved.');
        }
        f().reset({ ...empty });
        this.saved.emit();
        return undefined;
      },
    },
  });

  protected async remove(keys: ApiKeys): Promise<void> {
    this.removing.set(true);
    try {
      await this.api.setApiKeys(keys);
      this.notifications.success('Key removed.');
      this.saved.emit();
    } catch {
      // already shown as a toast
    } finally {
      this.removing.set(false);
    }
  }
}
