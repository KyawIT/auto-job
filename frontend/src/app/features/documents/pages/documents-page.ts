import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Desktop } from '../../../core/desktop';
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { formatBytes, label } from '../../../shared/labels';
import { PageHeader } from '../../../shared/page-header/page-header';
import { Sheet } from '../../../shared/sheet/sheet';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { CvImportButton } from '../components/cv-import-button';
import { DocumentEditForm } from '../components/document-edit-form';
import { DocumentUploader } from '../components/document-uploader';
import { DocumentsApi } from '../documents.api';
import { AppDocument } from '../documents.models';

@Component({
  selector: 'app-documents-page',
  imports: [
    DatePipe,
    PageHeader,
    EmptyState,
    Sheet,
    StatusBadge,
    DocumentUploader,
    DocumentEditForm,
    CvImportButton,
  ],
  templateUrl: './documents-page.html',
  styles: `
    .doc-icon {
      display: grid;
      flex: none;
      place-items: center;
      width: 2.25rem;
      height: 2.75rem;
      border-radius: 0.375rem;
      background: var(--accent-soft);
      color: var(--accent);
      font-size: 0.625rem;
      font-weight: 700;
      letter-spacing: 0.04em;
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      justify-content: flex-end;
      gap: 0.25rem;
    }
  `,
})
export default class DocumentsPage {
  private readonly api = inject(DocumentsApi);
  private readonly desktop = inject(Desktop);
  private readonly confirm = inject(Confirm);

  protected readonly documents = this.api.documentsResource();
  protected readonly editing = signal<AppDocument | null>(null);
  protected readonly label = label;
  protected readonly formatBytes = formatBytes;

  protected extension(document: AppDocument): string {
    return document.originalFileName.split('.').pop()?.toUpperCase() ?? '';
  }

  protected async download(document: AppDocument): Promise<void> {
    const blob = await this.api.download(document.id);
    await this.desktop.saveFile(document.originalFileName, blob);
  }

  protected async remove(document: AppDocument): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete document?',
      message: `"${document.displayName}" is deleted from this computer. Applications that attach it must drop it first.`,
      confirmLabel: 'Delete',
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.delete(document.id);
    } catch {
      // 409 "still attached" is shown as a toast
    }
    this.documents.reload();
  }

  protected saved(): void {
    this.editing.set(null);
    this.documents.reload();
  }
}
