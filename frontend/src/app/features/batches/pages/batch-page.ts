import {
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  numberAttribute,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { toApiError } from '../../../core/api-error';
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { PageHeader } from '../../../shared/page-header/page-header';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { BatchesApi } from '../batches.api';
import { Batch } from '../batches.models';

const pollMs = 1500;

/** Live progress of a batch. Polls while running; the user can leave and come back any time. */
@Component({
  selector: 'app-batch-page',
  imports: [DatePipe, RouterLink, PageHeader, EmptyState, StatusBadge],
  templateUrl: './batch-page.html',
  styleUrl: './batch-page.scss',
})
export default class BatchPage {
  private readonly api = inject(BatchesApi);
  private readonly confirm = inject(Confirm);

  readonly id = input.required({ transform: numberAttribute });

  protected readonly batch = this.api.batchResource(this.id);
  protected readonly notFound = computed(
    () => !!this.batch.error() && toApiError(this.batch.error()).kind === 'not-found',
  );
  protected readonly busy = signal(false);
  /** Drives a one-time completion announcement for screen readers. */
  protected readonly finished = computed(
    () => this.batch.hasValue() && this.batch.value().status !== 'Running',
  );

  protected readonly progress = computed(() => {
    if (!this.batch.hasValue()) return null;
    const p = this.batch.value().progress;
    const finished = p.done + p.failed + p.cancelled;
    return { ...p, finished, fraction: p.total ? finished / p.total : 0 };
  });

  constructor() {
    const timer = setInterval(() => {
      if (
        this.batch.hasValue() &&
        this.batch.value().status === 'Running' &&
        !this.batch.isLoading()
      )
        this.batch.reload();
    }, pollMs);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected title(batch: Batch): string {
    if (batch.status === 'Running') return 'Writing letters…';
    if (batch.status === 'Cancelled') return 'Batch cancelled';
    return batch.progress.failed ? 'Finished with problems' : 'All letters written';
  }

  protected async cancel(): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Cancel batch?',
      message: 'Letters already written are kept. Queued ones are not generated.',
      confirmLabel: 'Cancel batch',
      danger: true,
    });
    if (ok) await this.run(() => this.api.cancel(this.id()));
  }

  protected retry(): Promise<void> {
    return this.run(() => this.api.retryFailed(this.id()));
  }

  private async run(command: () => Promise<Batch>): Promise<void> {
    this.busy.set(true);
    try {
      this.batch.value.set(await command());
    } catch {
      // toast
    } finally {
      this.busy.set(false);
    }
  }
}
