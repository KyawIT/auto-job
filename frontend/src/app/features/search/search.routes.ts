import { Routes } from '@angular/router';

export default [
  { path: '', title: 'Search · AutoJob', loadComponent: () => import('./pages/search-page') },
  {
    path: 'saved',
    title: 'Saved searches · AutoJob',
    loadComponent: () => import('./pages/saved-searches-page'),
  },
] satisfies Routes;
