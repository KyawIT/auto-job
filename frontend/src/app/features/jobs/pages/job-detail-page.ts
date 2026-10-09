import {
  Component,
  computed,
  effect,
  inject,
  input,
  numberAttribute,
  signal,
  untracked,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { toApiError } from '../../../core/api-error';
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { label } from '../../../shared/labels';
import { PageHeader } from '../../../shared/page-header/page-header';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { ApplicationsApi } from '../../applications/applications.api';
import { JobsApi } from '../jobs.api';
import { Job, JobPostingState } from '../jobs.models';

@Component({
  selector: 'app-job-detail-page',
  imports: [DatePipe, DecimalPipe, RouterLink, PageHeader, EmptyState, StatusBadge],
  templateUrl: './job-detail-page.html',
  styleUrl: './job-detail-page.scss',
})
export default class JobDetailPage {
  private readonly api = inject(JobsApi);
  private readonly applications = inject(ApplicationsApi);
  private readonly router = inject(Router);
  private readonly confirm = inject(Confirm);

  readonly id = input.required({ transform: numberAttribute });

  protected readonly job = this.api.jobResource(this.id);
  protected readonly notFound = computed(
    () => !!this.job.error() && toApiError(this.job.error()).kind === 'not-found',
  );
  protected readonly starting = signal(false);
  protected readonly label = label;

  constructor() {
    // Opening a new posting marks it as seen.
    effect(() => {
      if (this.job.hasValue() && this.job.value().state === 'New') {
        const id = this.job.value().id;
        untracked(() => void this.api.setState(id, 'Seen').catch(() => undefined));
      }
    });
  }

  protected facts(job: Job): { term: string; value: string }[] {
    const facts = [
      { term: 'Location', value: job.location ?? '' },
      { term: 'Work mode', value: job.workMode !== 'Unknown' ? label(job.workMode) : '' },
      {
        term: 'Employment',
        value: job.employmentType !== 'Unknown' ? label(job.employmentType) : '',
      },
      { term: 'Source', value: label(job.source) },
      { term: 'Contact', value: [job.contactName, job.contactEmail].filter(Boolean).join(', ') },
    ];
    return facts.filter((f) => f.value);
  }

  protected async setState(state: JobPostingState): Promise<void> {
    await this.api.setState(this.id(), state);
    this.job.reload();
  }

  protected async startApplication(): Promise<void> {
    this.starting.set(true);
    try {
      const application = await this.applications.start(this.id());
      void this.router.navigate(['/applications', application.id]);
    } catch {
      this.starting.set(false);
    }
  }

  protected async remove(job: Job): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete job?',
      message: `"${job.title}" at ${job.companyName} is removed. A search may find it again; dismissing hides it for good.`,
      confirmLabel: 'Delete',
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.delete(job.id);
      void this.router.navigateByUrl('/jobs');
    } catch {
      // 409: it has an application (toast)
    }
  }
}
