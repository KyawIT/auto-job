import { Component, inject, input, linkedSignal, output, signal } from '@angular/core';
import { toApiError } from '../../../core/api-error';
import { FileDrop } from '../../../shared/file-drop/file-drop';
import { label } from '../../../shared/labels';
import { DocumentsApi } from '../documents.api';
import {
  ACCEPTED_FILES,
  AppDocument,
  DOCUMENT_TYPES,
  DocumentType,
  MAX_UPLOAD_MB,
} from '../documents.models';

/** Pick a type, drop files, they upload one after another. Upload problems are listed inline. */
@Component({
  selector: 'app-document-uploader',
  imports: [FileDrop],
  template: `
    @if (!fixedType()) {
      <div class="options">
        <div class="field">
          <label class="field-label" for="upload-type">Document type</label>
          <select
            class="select"
            id="upload-type"
            [value]="type()"
            (change)="type.set($any($event.target).value)"
          >
            @for (t of types; track t) {
              <option [value]="t">{{ label(t) }}</option>
            }
          </select>
        </div>
        <label class="check">
          <input
            type="checkbox"
            [checked]="attach()"
            (change)="attach.set($any($event.target).checked)"
          />
          Attach to new applications
        </label>
      </div>
    }
    <app-file-drop
      [accept]="accept"
      [multiple]="!fixedType()"
      [disabled]="!!uploading()"
      [hint]="hint"
      (files)="upload($event)"
    />
    <p class="visually-hidden" role="status">{{ uploading() ? 'Uploading ' + uploading() : '' }}</p>
    @if (uploading()) {
      <p class="caption" aria-hidden="true">Uploading {{ uploading() }}…</p>
    }
    @if (problems().length) {
      <ul class="notice notice--danger problems" role="alert">
        @for (p of problems(); track p) {
          <li>{{ p }}</li>
        }
      </ul>
    }
  `,
  styles: `
    :host {
      display: grid;
      gap: 0.75rem;
    }
    .options {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      gap: 1rem;
    }
    .options .field {
      min-width: 14rem;
    }
    .check {
      min-height: 2.375rem;
    }
    .problems {
      display: grid;
      margin: 0;
      padding-left: 2rem;
    }
  `,
})
export class DocumentUploader {
  private readonly api = inject(DocumentsApi);

  /** Set to upload only this type (the wizard's CV step). */
  readonly fixedType = input<DocumentType>();
  readonly uploaded = output<AppDocument[]>();

  protected readonly types = DOCUMENT_TYPES;
  protected readonly label = label;
  protected readonly accept = ACCEPTED_FILES;
  protected readonly hint = `PDF, Word (.docx) or text, up to ${MAX_UPLOAD_MB} MB.`;

  protected readonly type = linkedSignal<DocumentType>(() => this.fixedType() ?? 'Cv');
  protected readonly attach = signal(false);
  protected readonly uploading = signal<string | null>(null);
  protected readonly problems = signal<string[]>([]);

  protected async upload(files: File[]): Promise<void> {
    const done: AppDocument[] = [];
    const problems: string[] = [];
    for (const file of files) {
      if (file.size > MAX_UPLOAD_MB * 1024 * 1024) {
        problems.push(`${file.name}: larger than ${MAX_UPLOAD_MB} MB.`);
        continue;
      }
      this.uploading.set(file.name);
      try {
        done.push(await this.api.upload(file, this.type(), this.attach()));
      } catch (error) {
        const apiError = toApiError(error);
        const details = Object.values(apiError.fieldErrors).flat();
        problems.push(`${file.name}: ${details.length ? details.join(' ') : apiError.message}`);
      }
    }
    this.uploading.set(null);
    this.problems.set(problems);
    if (done.length) this.uploaded.emit(done);
  }
}
