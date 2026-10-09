import { Component, inject, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { languageName } from '../../../shared/labels';
import { PageHeader } from '../../../shared/page-header/page-header';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { ApplicationsApi } from '../applications.api';
import { APPLICATION_STATUSES, ApplicationStatus } from '../applications.models';

@Component({
  selector: 'app-application-list-page',
  imports: [DatePipe, RouterLink, PageHeader, EmptyState, StatusBadge],
  template: `
    <app-page-header
      heading="Applications"
      subtitle="Every letter you started, from draft to offer."
    />

    <div class="toolbar">
      <nav class="segmented" aria-label="Filter by status">
        <a
          [routerLink]="[]"
          [queryParams]="{ status: null }"
          [attr.aria-current]="status() ? null : 'page'"
          >All</a
        >
        @for (s of statuses; track s) {
          <a
            [routerLink]="[]"
            [queryParams]="{ status: s }"
            [attr.aria-current]="status() === s ? 'page' : null"
            >{{ s }}</a
          >
        }
      </nav>
    </div>

    @if (applications.hasValue() && applications.value().length) {
      <ul class="list">
        @for (a of applications.value(); track a.id) {
          <li>
            <a class="list-row" [routerLink]="['/applications', a.id]">
              <div class="list-row__main">
                <p class="list-row__title">{{ a.jobTitle }}</p>
                <p class="list-row__meta">
                  {{ a.companyName }} · {{ languageName(a.language) }} ·
                  {{
                    a.sentAt
                      ? 'sent ' + (a.sentAt | date: 'mediumDate')
                      : 'updated ' + (a.updatedAt | date: 'mediumDate')
                  }}
                </p>
              </div>
              <app-status-badge [status]="a.status" />
            </a>
          </li>
        }
      </ul>
    } @else if (applications.hasValue()) {
      @if (status()) {
        <app-empty-state
          symbol="⌕"
          heading="Nothing here"
          [message]="'No applications with status ' + status() + '.'"
        />
      } @else {
        <app-empty-state
          symbol="✎"
          heading="No applications yet"
          message="Open a job and choose “Start application”, or select several jobs to generate letters in one go."
        >
          <a class="btn btn--primary" routerLink="/jobs">Go to jobs</a>
        </app-empty-state>
      }
    } @else if (applications.error()) {
      <app-empty-state symbol="!" heading="Applications not available">
        <button type="button" class="btn" (click)="applications.reload()">Try again</button>
      </app-empty-state>
    } @else {
      <div class="list" role="status" aria-label="Loading applications">
        @for (i of [1, 2, 3]; track i) {
          <div class="list-row"><span class="skeleton" style="width: 50%"></span></div>
        }
      </div>
    }
  `,
  styles: `
    .segmented {
      flex-wrap: wrap;
    }
  `,
})
export default class ApplicationListPage {
  readonly status = input<ApplicationStatus | null, string | undefined>(null, {
    transform: (v) =>
      APPLICATION_STATUSES.includes(v as ApplicationStatus) ? (v as ApplicationStatus) : null,
  });

  protected readonly statuses = APPLICATION_STATUSES;
  protected readonly languageName = languageName;
  protected readonly applications = inject(ApplicationsApi).applicationsResource(this.status);
}
