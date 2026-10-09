import { Component, inject, input, output, signal } from '@angular/core';
import { toApiError } from '../../../core/api-error';
import { Notifications } from '../../../core/notifications';
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import { ProfileApi } from '../../profile/profile.api';
import { Profile } from '../../profile/profile.models';
import { AppDocument } from '../documents.models';

/** "Fill profile from CV". The CV text leaves the machine, so the user agrees to that first. */
@Component({
  selector: 'app-cv-import-button',
  template: `
    <button
      type="button"
      class="btn"
      [class]="primary() ? 'btn--primary' : 'btn--small'"
      [disabled]="busy()"
      (click)="run()"
    >
      {{ busy() ? 'Reading CV…' : 'Fill profile from CV' }}
    </button>
  `,
})
export class CvImportButton {
  private readonly profiles = inject(ProfileApi);
  private readonly confirm = inject(Confirm);
  private readonly notifications = inject(Notifications);

  readonly document = input.required<AppDocument>();
  readonly primary = input(false);
  readonly imported = output<Profile>();

  protected readonly busy = signal(false);

  protected async run(): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Fill profile from CV?',
      message:
        `The text of "${this.document().displayName}" is sent to NVIDIA's AI service to read it. ` +
        'Empty profile fields are filled and missing entries added; nothing you entered is overwritten.',
      confirmLabel: 'Send and fill',
    });
    if (!ok) return;

    this.busy.set(true);
    try {
      const profile = await this.profiles.importCv(this.document().id);
      this.notifications.show('Profile filled from your CV.', {
        tone: 'success',
        action: { label: 'Review', link: '/profile' },
      });
      this.imported.emit(profile);
    } catch (error) {
      // AI problems are already toasts; a 400 here means the document has no readable text.
      const apiError = toApiError(error);
      if (apiError.kind === 'validation') {
        this.notifications.error(
          Object.values(apiError.fieldErrors).flat().join(' ') || apiError.message,
        );
      }
    } finally {
      this.busy.set(false);
    }
  }
}
