import { Routes } from '@angular/router';

export default [
  { path: '', title: 'Profile · AutoJob', loadComponent: () => import('./pages/profile-page') },
] satisfies Routes;
