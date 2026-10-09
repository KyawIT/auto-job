import { Component, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { toApiError } from '../../../core/api-error';
import { Desktop } from '../../../core/desktop';
import { Notifications } from '../../../core/notifications';
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { languageName } from '../../../shared/labels';
import { PageHeader } from '../../../shared/page-header/page-header';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { ApplicationsApi } from '../applications.api';
import {
  ApplicationDetails,
  ExportFormat,
  GeneratedText,
  GeneratedTextKind,
} from '../applications.models';
import { AttachmentsPicker } from '../components/attachments-picker';
import { RecipientForm } from '../components/recipient-form';
import { StatusChanger } from '../components/status-changer';
import { TextEditor } from '../components/text-editor';
import { Timeline } from '../components/timeline';
import { VersionsSheet } from '../components/versions-sheet';

@Component({
  selector: 'app-application-detail-page',
  imports: [
    RouterLink,
    PageHeader,
    EmptyState,
    StatusBadge,
    TextEditor,
    VersionsSheet,
    RecipientForm,
    AttachmentsPicker,
    StatusChanger,
    Timeline,
  ],
  templateUrl: './application-detail-page.html',
  styleUrl: './application-detail-page.scss',
})
export default class ApplicationDetailPage {
  private readonly api = inject(ApplicationsApi);
  private readonly desktop = inject(Desktop);
  private readonly notifications = inject(Notifications);
  private readonly confirm = inject(Confirm);
  private readonly router = inject(Router);

  readonly id = input.required({ transform: numberAttribute });

  protected readonly application = this.api.applicationResource(this.id);
  protected readonly notFound = computed(
    () => !!this.application.error() && toApiError(this.application.error()).kind === 'not-found',
  );
  protected readonly generating = signal(false);
  protected readonly exporting = signal<ExportFormat | 'email' | null>(null);
  protected readonly versionsOf = signal<GeneratedTextKind | null>(null);
  protected readonly languageName = languageName;

  protected readonly letter = computed(() => this.current('CoverLetter'));
  protected readonly subject = computed(() => this.current('EmailSubject'));
  protected readonly body = computed(() => this.current('EmailBody'));

  private current(kind: GeneratedTextKind): GeneratedText | null {
    return this.application.hasValue()
      ? (this.application.value().texts.find((t) => t.kind === kind) ?? null)
      : null;
  }

  /** Commands answer with the full application, so the page updates without another request. */
  protected update(details: ApplicationDetails): void {
    this.application.value.set(details);
  }

  protected async generate(): Promise<void> {
    const edited =
      this.application.hasValue() && this.application.value().texts.some((t) => t.isEdited);
    if (edited) {
      const ok = await this.confirm.ask({
        title: 'Write a new version?',
        message:
          'The AI writes a new letter and email. Your edited texts stay available under Versions.',
        confirmLabel: 'Generate',
      });
      if (!ok) return;
    }
    this.generating.set(true);
    try {
      this.update(await this.api.generate(this.id(), () => void this.generate()));
      this.notifications.success('Your letter is ready.');
    } catch {
      // toast (missing key, rate limit, AI failure with retry)
    } finally {
      this.generating.set(false);
    }
  }

  protected async export(format: ExportFormat): Promise<void> {
    if (!this.application.hasValue()) return;
    const company = this.application.value().job.companyName.replace(/[\\/:*?"<>|]/g, '');
    this.exporting.set(format);
    try {
      const file = await this.api.export(this.id(), format);
      const saved = await this.desktop.saveFile(
        `Cover letter - ${company}.${format.toLowerCase()}`,
        file,
      );
      if (saved) this.application.reload(); // adds the "Exported" event
    } catch (error) {
      this.showValidation(error);
    } finally {
      this.exporting.set(null);
    }
  }

  protected async prepareEmail(): Promise<void> {
    this.exporting.set('email');
    try {
      const draft = await this.api.createEmailDraft(this.id());
      const opened = await this.desktop.openFile(draft.fullPath);
      this.notifications.show(
        opened
          ? 'Email draft opened in your mail app.'
          : `Email draft saved to ${draft.fullPath}. Open it with your mail app.`,
        { tone: 'success' },
      );
      this.application.reload();
    } catch (error) {
      this.showValidation(error);
    } finally {
      this.exporting.set(null);
    }
  }

  protected async remove(): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete application?',
      message:
        'The letter, its versions and the timeline are deleted. The job and your documents stay.',
      confirmLabel: 'Delete',
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.delete(this.id());
      void this.router.navigateByUrl('/applications');
    } catch {
      // toast
    }
  }

  /** E.g. "Generate or write the cover letter first." — not tied to a form field, so it becomes a toast. */
  private showValidation(error: unknown): void {
    const apiError = toApiError(error);
    if (apiError.kind === 'validation') {
      this.notifications.error(
        Object.values(apiError.fieldErrors).flat().join(' ') || apiError.message,
      );
    }
  }
}
