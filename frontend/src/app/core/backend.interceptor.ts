import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Backend } from './backend';

/**
 * Turns relative requests like `http.get('/api/jobs')` into calls to the sidecar
 * and attaches the per-launch token. Absolute URLs pass through untouched.
 */
export const backendInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/')) {
    return next(req);
  }

  const backend = inject(Backend);
  return next(
    req.clone({
      url: backend.url + req.url,
      setHeaders: backend.token ? { 'X-AutoJob-Token': backend.token } : {},
    }),
  );
};
