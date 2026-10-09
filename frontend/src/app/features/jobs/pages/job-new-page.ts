import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { email, form, FormRoot, required } from '@angular/forms/signals';
import { serverErrors } from '../../../core/api-error';
import { FIELD_DIRECTIVES } from '../../../shared/field-errors/field-errors';
import { label, orNull } from '../../../shared/labels';
import { PageHeader } from '../../../shared/page-header/page-header';
import { JobsApi } from '../jobs.api';
import { EMPLOYMENT_TYPES, EmploymentType, WORK_MODES, WorkMode } from '../jobs.models';

@Component({
  selector: 'app-job-new-page',
  imports: [FormRoot, RouterLink, PageHeader, ...FIELD_DIRECTIVES],
  templateUrl: './job-new-page.html',
})
export default class JobNewPage {
  private readonly api = inject(JobsApi);
  private readonly router = inject(Router);

  protected readonly workModes = WORK_MODES;
  protected readonly employmentTypes = EMPLOYMENT_TYPES;
  protected readonly label = label;

  protected readonly model = signal({
    title: '',
    companyName: '',
    description: '',
    location: '',
    url: '',
    contactEmail: '',
    contactName: '',
    employmentType: 'Unknown' as EmploymentType,
    workMode: 'Unknown' as WorkMode,
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.title);
      required(p.companyName);
      required(p.description);
      email(p.contactEmail);
    },
    {
      submission: {
        action: async (f) => {
          const v = f().value();
          try {
            const job = await this.api.create({
              ...v,
              location: orNull(v.location),
              url: orNull(v.url),
              contactEmail: orNull(v.contactEmail),
              contactName: orNull(v.contactName),
            });
            void this.router.navigate(['/jobs', job.id], { replaceUrl: true });
            return undefined;
          } catch (error) {
            return serverErrors(f, error);
          }
        },
      },
    },
  );
}
