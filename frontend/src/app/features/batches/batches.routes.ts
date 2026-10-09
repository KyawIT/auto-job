import { Routes } from '@angular/router';

export default [
  { path: '', pathMatch: 'full', redirectTo: '/applications' },
  { path: ':id', title: 'Batch · AutoJob', loadComponent: () => import('./pages/batch-page') },
] satisfies Routes;
