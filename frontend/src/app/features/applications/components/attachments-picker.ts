import { Component, computed, inject, input, linkedSignal, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { label } from '../../../shared/labels';
import { DocumentsApi } from '../../documents/documents.api';
import { ApplicationsApi } from '../applications.api';
import { ApplicationDetails } from '../applications.models';

/**
 * Which documents go with the email, and in which order. Reordering uses buttons
 * (works by keyboard and screen reader) rather than drag and drop.
 */
@Component({
  selector: 'app-attachments-picker',
  imports: [RouterLink],
  templateUrl: './attachments-picker.html',
  styleUrl: './attachments-picker.scss',
})
export class AttachmentsPicker {
  private readonly api = inject(ApplicationsApi);

  readonly application = input.required<ApplicationDetails>();
  readonly saved = output<ApplicationDetails>();

  protected readonly documents = inject(DocumentsApi).documentsResource();
  protected readonly label = label;
  protected readonly saving = signal(false);

  /** Ordered ids of attached documents. */
  protected readonly order = linkedSignal(() =>
    this.application().attachments.map((a) => a.documentId),
  );
  protected readonly dirty = computed(
    () =>
      this.order().join() !==
      this.application()
        .attachments.map((a) => a.documentId)
        .join(),
  );

  protected readonly attached = computed(() => {
    const docs = this.documents.hasValue() ? this.documents.value() : [];
    return this.order()
      .map((id) => docs.find((d) => d.id === id))
      .filter((d) => !!d);
  });
  protected readonly available = computed(() =>
    (this.documents.hasValue() ? this.documents.value() : []).filter(
      (d) => !this.order().includes(d.id),
    ),
  );

  protected addFromSelect(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const id = Number(select.value);
    select.value = '';
    if (id) this.order.update((ids) => [...ids, id]);
  }

  protected removeAt(index: number): void {
    this.order.update((ids) => ids.filter((_, i) => i !== index));
  }

  protected move(index: number, by: -1 | 1): void {
    this.order.update((ids) => {
      const next = [...ids];
      [next[index], next[index + by]] = [next[index + by], next[index]];
      return next;
    });
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      this.saved.emit(await this.api.setAttachments(this.application().id, this.order()));
    } catch {
      // toast
    } finally {
      this.saving.set(false);
    }
  }
}
