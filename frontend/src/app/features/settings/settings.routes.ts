import { Routes } from '@angular/router';

export default [
  { path: '', title: 'Settings · AutoJob', loadComponent: () => import('./pages/settings-page') },
] satisfies Routes;
