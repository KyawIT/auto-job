import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { http } from '../../testing/test-app';
import { utcDatesInterceptor } from './utc-dates.interceptor';

describe('utcDatesInterceptor', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([utcDatesInterceptor])),
        provideHttpClientTesting(),
      ],
    });
  });

  it('marks backend timestamps as UTC and leaves everything else alone', async () => {
    const pending = firstValueFrom(TestBed.inject(HttpClient).get('/api/batches/1'));
    http()
      .expectOne('/api/batches/1')
      .flush({
        createdAt: '2026-10-09T16:20:22.0225958',
        finishedAt: null,
        tasks: [{ at: '2026-10-09T16:20:22' }],
        dateOfBirth: '1990-01-31',
        alreadyZoned: '2026-10-09T16:20:22Z',
        title: 'Developer',
      });

    expect(await pending).toEqual({
      createdAt: '2026-10-09T16:20:22.0225958Z',
      finishedAt: null,
      tasks: [{ at: '2026-10-09T16:20:22Z' }],
      dateOfBirth: '1990-01-31',
      alreadyZoned: '2026-10-09T16:20:22Z',
      title: 'Developer',
    });
  });
});
