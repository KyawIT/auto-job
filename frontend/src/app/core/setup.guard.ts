import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { toApiError } from './api-error';
import { silent } from './error.interceptor';

/** Remembers that a profile exists, so the guard asks the backend only until it does. */
@Service()
export class SetupStatus {
  hasProfile = false;
}

/** No profile yet → the setup wizard. Any other failure lets the user through (the shell shows backend status). */
export const setupGuard: CanActivateFn = async () => {
  const status = inject(SetupStatus);
  const http = inject(HttpClient);
  const router = inject(Router);
  if (status.hasProfile) return true;

  try {
    await firstValueFrom(http.get('/api/profile', { context: silent() }));
    status.hasProfile = true;
    return true;
  } catch (error) {
    return toApiError(error).kind === 'not-found' ? router.parseUrl('/setup') : true;
  }
};
