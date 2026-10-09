import { afterNextRender, Component, inject, Injector, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SetupStatus } from '../../../core/setup.guard';
import { CvImportButton } from '../../documents/components/cv-import-button';
import { DocumentUploader } from '../../documents/components/document-uploader';
import { AppDocument } from '../../documents/documents.models';
import { PersonalDataForm } from '../../profile/components/personal-data-form';
import { ProfileApi } from '../../profile/profile.api';
import { ApiKeysForm } from '../../settings/components/api-keys-form';
import { SettingsApi } from '../../settings/settings.api';

type Step = 1 | 2 | 3;

/** First-run wizard: personal data → NVIDIA key (skippable) → CV import (skippable). */
@Component({
  selector: 'app-setup-page',
  imports: [PersonalDataForm, ApiKeysForm, DocumentUploader, CvImportButton],
  templateUrl: './setup-page.html',
  styleUrl: './setup-page.scss',
})
export default class SetupPage {
  private readonly router = inject(Router);
  private readonly status = inject(SetupStatus);
  private readonly injector = inject(Injector);

  protected readonly profile = inject(ProfileApi).profileResource();
  protected readonly settings = inject(SettingsApi).settingsResource();
  protected readonly step = signal<Step>(1);
  /** Direction of the last step change, so going back slides the other way. */
  protected readonly direction = signal<'forward' | 'back'>('forward');
  protected readonly cv = signal<AppDocument | null>(null);

  protected readonly steps = [
    { n: 1, label: 'About you' },
    { n: 2, label: 'AI key' },
    { n: 3, label: 'Your CV' },
  ] as const;

  protected go(step: Step): void {
    this.direction.set(step > this.step() ? 'forward' : 'back');
    this.step.set(step);
    // The button that was pressed is gone; continue from the new step's heading.
    afterNextRender(() => document.getElementById(`step${step}`)?.focus(), {
      injector: this.injector,
    });
  }

  protected profileSaved(): void {
    this.status.hasProfile = true;
    this.go(2);
  }

  protected cvUploaded(documents: AppDocument[]): void {
    this.cv.set(documents[0] ?? null);
  }

  protected finish(): void {
    void this.router.navigateByUrl('/jobs');
  }
}
