import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { map } from 'rxjs';

// "2026-10-09T16:20:22.0225958" — a timestamp without offset.
const unzonedTimestamp = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?$/;

/**
 * The backend stores UTC but SQLite drops the "UTC" marker, so timestamps arrive without
 * an offset and the browser would read them as local time. Marks them as UTC ("…Z").
 */
export const utcDatesInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    map((event) =>
      event instanceof HttpResponse && event.body !== null && typeof event.body === 'object'
        ? event.clone({ body: markUtc(event.body) })
        : event,
    ),
  );

function markUtc(value: unknown): unknown {
  if (typeof value === 'string') return unzonedTimestamp.test(value) ? `${value}Z` : value;
  if (Array.isArray(value)) return value.map(markUtc);
  if (value !== null && typeof value === 'object' && !(value instanceof Blob)) {
    return Object.fromEntries(Object.entries(value).map(([key, v]) => [key, markUtc(v)]));
  }
  return value;
}
