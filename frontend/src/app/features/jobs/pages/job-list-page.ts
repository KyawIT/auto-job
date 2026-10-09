import { Component, computed, effect, inject, input, linkedSignal, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { label } from '../../../shared/labels';
import { PageHeader } from '../../../shared/page-header/page-header';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { BatchStartSheet } from '../components/batch-start-sheet';
import { JobsApi } from '../jobs.api';
import { JobFilter, JobPostingState, JobSummary } from '../jobs.models';

const filters: { value: JobFilter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'new', label: 'New' },
  { value: 'bookmarked', label: 'Bookmarked' },
  { value: 'dismissed', label: 'Dismissed' },
];

@Component({
  selector: 'app-job-list-page',
  imports: [DatePipe, RouterLink, PageHeader, EmptyState, StatusBadge, BatchStartSheet],
  templateUrl: './job-list-page.html',
  styleUrl: './job-list-page.scss',
})
export default class JobListPage {
  private readonly api = inject(JobsApi);
  private readonly router = inject(Router);

  /** Query params (`?filter=bookmarked&q=angular`), so the list state survives navigation. */
  readonly filter = input<JobFilter, string | undefined>('all', {
    transform: (v) => (filters.some((f) => f.value === v) ? (v as JobFilter) : 'all'),
  });
  readonly q = input<string, string | undefined>('', { transform: (v) => v ?? '' });

  protected readonly filters = filters;
  protected readonly label = label;

  /** What's typed, shown instantly; the query follows a beat later. */
  protected readonly term = linkedSignal(() => this.q());
  protected readonly jobs = this.api.jobsResource(this.filter, this.q);
  protected readonly selected = signal<ReadonlySet<number>>(new Set());
  protected readonly batchOpen = signal(false);

  protected readonly visibleIds = computed(() =>
    this.jobs.hasValue() ? this.jobs.value().map((j) => j.id) : [],
  );
  protected readonly allSelected = computed(
    () => this.visibleIds().length > 0 && this.visibleIds().every((id) => this.selected().has(id)),
  );
  protected readonly selectedIds = computed(() =>
    this.visibleIds().filter((id) => this.selected().has(id)),
  );

  constructor() {
    effect((onCleanup) => {
      const term = this.term();
      if (term === this.q()) return;
      const timer = setTimeout(() => this.setQuery({ q: term || null }), 250);
      onCleanup(() => clearTimeout(timer));
    });
  }

  protected setQuery(params: { filter?: JobFilter | null; q?: string | null }): void {
    void this.router.navigate([], {
      queryParams: params,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  protected toggle(id: number): void {
    this.selected.update((set) => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  protected clearSelection(): void {
    this.selected.set(new Set());
  }

  protected toggleAll(): void {
    this.selected.set(this.allSelected() ? new Set() : new Set(this.visibleIds()));
  }

  protected async setState(job: JobSummary, state: JobPostingState): Promise<void> {
    await this.api.setState(job.id, state);
    this.jobs.reload();
  }

  protected meta(job: JobSummary): string {
    return [
      job.companyName,
      job.location,
      job.workMode !== 'Unknown' ? label(job.workMode) : null,
      label(job.source),
    ]
      .filter(Boolean)
      .join(' · ');
  }
}
