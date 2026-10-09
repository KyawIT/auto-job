import { Routes } from '@angular/router';

export default [
  { path: '', title: 'Jobs · AutoJob', loadComponent: () => import('./pages/job-list-page') },
  { path: 'new', title: 'Add job · AutoJob', loadComponent: () => import('./pages/job-new-page') },
  { path: ':id', title: 'Job · AutoJob', loadComponent: () => import('./pages/job-detail-page') },
] satisfies Routes;
