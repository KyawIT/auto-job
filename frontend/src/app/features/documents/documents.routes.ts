import { Routes } from '@angular/router';

export default [
  { path: '', title: 'Documents · AutoJob', loadComponent: () => import('./pages/documents-page') },
] satisfies Routes;
