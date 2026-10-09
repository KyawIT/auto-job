import { Routes } from '@angular/router';

export default [
  {
    path: '',
    title: 'Applications · AutoJob',
    loadComponent: () => import('./pages/application-list-page'),
  },
  {
    path: ':id',
    title: 'Application · AutoJob',
    loadComponent: () => import('./pages/application-detail-page'),
  },
] satisfies Routes;
