import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { http, provideTestApp, toasts } from '../../testing/test-app';
import { ApiError } from './api-error';
import { Notifications } from './notifications';

describe('errorInterceptor', () => {
  let client: HttpClient;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideTestApp() });
    client = TestBed.inject(HttpClient);
  });

  async function fail(status: number, body: object | null): Promise<ApiError> {
    const pending = firstValueFrom(client.get('/api/x'));
    http().expectOne('/api/x').flush(body, { status, statusText: 'Error' });
    return pending.then(
      () => {
        throw new Error('expected an error');
      },
      (error: ApiError) => error,
    );
  }

  it('turns a 500 into a "Backend error" toast', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const error = await fail(500, { title: 'boom' });
    expect(error.kind).toBe('server');
    expect(toasts()).toEqual(['Backend error. Details are in the console.']);
  });

  it('leaves validation errors to the form, with camelCase field names', async () => {
    const error = await fail(400, { title: 'Invalid', errors: { FirstName: ['Required.'] } });
    expect(error.kind).toBe('validation');
    expect(error.fieldErrors).toEqual({ firstName: ['Required.'] });
    expect(toasts()).toEqual([]);
  });

  it('points to Settings when the NVIDIA key is missing', async () => {
    await fail(400, { type: 'ai-not-configured', detail: 'No key' });
    const [toast] = TestBed.inject(Notifications).toasts();
    expect(toast.message).toContain('NVIDIA API key missing');
    expect(toast.action).toEqual({ label: 'Open Settings', link: '/settings' });
  });

  it('shows rate limits and conflicts with their message', async () => {
    await fail(429, { type: 'ai-rate-limited', detail: 'slow down' });
    await fail(409, { detail: 'Document is still attached.' });
    expect(toasts()).toEqual([
      'The AI is rate limited. Try again in a moment.',
      'Document is still attached.',
    ]);
  });

  it('does not toast a 404 (pages show their own not-found state)', async () => {
    const error = await fail(404, { detail: 'Job 1 not found.' });
    expect(error.kind).toBe('not-found');
    expect(toasts()).toEqual([]);
  });
});
