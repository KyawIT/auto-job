import { Routes } from '@angular/router';
import { setupGuard } from './core/setup.guard';
import { Shell } from './layout/shell/shell';

export const routes: Routes = [
  {
    path: 'setup',
    title: 'Welcome · AutoJob',
    loadChildren: () => import('./features/setup/setup.routes'),
  },
  {
    path: '',
    component: Shell,
    canActivate: [setupGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'jobs' },
      { path: 'jobs', loadChildren: () => import('./features/jobs/jobs.routes') },
      { path: 'search', loadChildren: () => import('./features/search/search.routes') },
      {
        path: 'applications',
        loadChildren: () => import('./features/applications/applications.routes'),
      },
      { path: 'batches', loadChildren: () => import('./features/batches/batches.routes') },
      { path: 'profile', loadChildren: () => import('./features/profile/profile.routes') },
      { path: 'documents', loadChildren: () => import('./features/documents/documents.routes') },
      { path: 'settings', loadChildren: () => import('./features/settings/settings.routes') },
      {
        path: '**',
        title: 'Not found · AutoJob',
        loadComponent: () => import('./layout/not-found'),
      },
    ],
  },
];
