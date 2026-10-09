import { HttpContext, HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiError, toApiError } from './api-error';
import { Notifications } from './notifications';

/** Set to skip the toast, e.g. when the caller shows the error itself. */
export const SILENT_ERRORS = new HttpContextToken<boolean>(() => false);

/** Offered as "Retry" on the toast when the AI request fails (502 ai-failed). */
export const RETRY_ACTION = new HttpContextToken<(() => void) | null>(() => null);

export function silent(): HttpContext {
  return new HttpContext().set(SILENT_ERRORS, true);
}

export function withRetry(retry: () => void): HttpContext {
  return new HttpContext().set(RETRY_ACTION, retry);
}

/**
 * Every failed request is rethrown as a typed `ApiError`. Errors the user can't fix in a form
 * (AI problems, conflicts, a dead backend) also become a toast here, so pages only handle
 * validation (shown on fields) and not-found (shown as a page state).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(Notifications);

  return next(req).pipe(
    catchError((raw: unknown) => {
      const error = toApiError(raw);
      if (!req.context.get(SILENT_ERRORS))
        notify(notifications, error, req.context.get(RETRY_ACTION));
      return throwError(() => error);
    }),
  );
};

function notify(notifications: Notifications, error: ApiError, retry: (() => void) | null): void {
  switch (error.kind) {
    case 'validation':
    case 'not-found':
      return;
    case 'ai-not-configured':
      notifications.error('NVIDIA API key missing. Add it in Settings to use the AI.', {
        label: 'Open Settings',
        link: '/settings',
      });
      return;
    case 'ai-rate-limited':
      notifications.error('The AI is rate limited. Try again in a moment.');
      return;
    case 'ai-failed':
      notifications.error(error.message, retry ? { label: 'Retry', run: retry } : undefined);
      return;
    case 'conflict':
      notifications.error(error.message);
      return;
    case 'network':
    case 'server':
      console.error('Backend error', error);
      notifications.error(
        error.kind === 'network'
          ? 'The backend is not reachable.'
          : 'Backend error. Details are in the console.',
      );
      return;
  }
}
