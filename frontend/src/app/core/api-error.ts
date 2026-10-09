import { HttpErrorResponse } from '@angular/common/http';
import type { FieldTree, ValidationError } from '@angular/forms/signals';

export type ApiErrorKind =
  | 'validation'
  | 'not-found'
  | 'conflict'
  | 'ai-not-configured'
  | 'ai-rate-limited'
  | 'ai-failed'
  | 'network'
  | 'server';

/** A backend ProblemDetails answer (or a network failure), reduced to what the UI reacts to. */
export class ApiError extends Error {
  constructor(
    readonly kind: ApiErrorKind,
    message: string,
    readonly status: number,
    /** Field errors of a 400, keyed by camelCase field name (`firstName`, not `FirstName`). */
    readonly fieldErrors: Readonly<Record<string, readonly string[]>> = {},
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

interface ProblemDetails {
  type?: string;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

const aiTypes: readonly ApiErrorKind[] = ['ai-not-configured', 'ai-rate-limited', 'ai-failed'];

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  if (!(error instanceof HttpErrorResponse)) {
    return new ApiError('server', error instanceof Error ? error.message : 'Unexpected error', 0);
  }
  if (error.status === 0) {
    return new ApiError('network', 'The backend is not reachable.', 0);
  }

  const problem = (isObject(error.error) ? error.error : {}) as ProblemDetails;
  const message = problem.detail ?? problem.title ?? error.message;
  const type = problem.type as ApiErrorKind | undefined;

  if (type && aiTypes.includes(type)) return new ApiError(type, message, error.status);
  if (error.status === 400 && problem.errors) {
    return new ApiError(
      'validation',
      problem.title ?? 'Please check the highlighted fields.',
      400,
      normalizeKeys(problem.errors),
    );
  }
  if (error.status === 404) return new ApiError('not-found', message, 404);
  if (error.status === 409) return new ApiError('conflict', message, 409);
  return new ApiError('server', message, error.status);
}

/**
 * Turns a 400's field errors into Signal Forms submission errors on the matching fields.
 * Keys without a matching field land on the form root so nothing is silently dropped.
 * Other errors yield nothing: the error interceptor has already shown them as a toast.
 */
export function serverErrors<T>(
  form: FieldTree<T>,
  error: unknown,
): ValidationError.WithOptionalFieldTree[] {
  const apiError = toApiError(error);
  if (apiError.kind !== 'validation') return [];

  const children = form as unknown as Record<string, FieldTree<unknown> | undefined>;
  return Object.entries(apiError.fieldErrors).flatMap(([key, messages]) =>
    messages.map((message) => ({
      kind: 'server',
      message,
      fieldTree: (children[key] ?? form) as FieldTree<unknown>,
    })),
  );
}

// "FirstName" / "$.firstName" / "request.FirstName" → "firstName"
function normalizeKeys(errors: Record<string, string[]>): Record<string, string[]> {
  const result: Record<string, string[]> = {};
  for (const [key, messages] of Object.entries(errors)) {
    const last =
      key
        .replace(/^\$\.?/, '')
        .split('.')
        .pop() ?? key;
    const name = last ? last[0].toLowerCase() + last.slice(1) : '';
    result[name] = [...(result[name] ?? []), ...messages];
  }
  return result;
}

function isObject(value: unknown): value is object {
  return typeof value === 'object' && value !== null;
}
