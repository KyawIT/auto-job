import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmptyState } from '../shared/empty-state/empty-state';
import { PageHeader } from '../shared/page-header/page-header';

@Component({
  selector: 'app-not-found',
  imports: [EmptyState, PageHeader, RouterLink],
  template: `
    <app-page-header heading="Page not found" />
    <app-empty-state symbol="?" heading="Nothing here" message="This page doesn't exist (anymore).">
      <a class="btn btn--primary" routerLink="/jobs">Go to jobs</a>
    </app-empty-state>
  `,
})
export default class NotFound {}
