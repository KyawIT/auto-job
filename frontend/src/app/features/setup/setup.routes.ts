import { Routes } from '@angular/router';

export default [{ path: '', loadComponent: () => import('./pages/setup-page') }] satisfies Routes;
