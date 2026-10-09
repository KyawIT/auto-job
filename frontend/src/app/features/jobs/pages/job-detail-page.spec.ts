import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { aJob, anApplication } from '../../../../testing/fixtures';
import {
  byText,
  click,
  expectRequest,
  nextTask,
  provideTestApp,
  settle,
  text,
  toasts,
} from '../../../../testing/test-app';
import JobDetailPage from './job-detail-page';

@Component({ template: '' })
class ApplicationStub {}

describe('JobDetailPage', () => {
  let harness: RouterTestingHarness;
  let root: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: provideTestApp([
        { path: 'jobs/:id', component: JobDetailPage },
        { path: 'applications/:id', component: ApplicationStub },
      ]),
    });
    harness = await RouterTestingHarness.create('/jobs/11');
    root = harness.routeNativeElement!;
    await settle();
  });

  it('shows the posting and starts an application', async () => {
    expectRequest('GET', '/api/jobs/11').flush(aJob());
    await harness.fixture.whenStable();
    expect(text(root.querySelector('h1'))).toBe('Frontend Developer');
    expect(text(root)).toContain('Build great UIs.');
    expect(text(root)).toContain('Full-time');

    click(byText(root, 'Start application'));
    await nextTask();
    const request = expectRequest('POST', '/api/applications');
    expect(request.request.body).toEqual({ jobPostingId: 11, language: null });
    request.flush(anApplication());
    await nextTask();
    expect(TestBed.inject(Router).url).toBe('/applications/21');
  });

  it('marks a new posting as seen when it is opened', async () => {
    expectRequest('GET', '/api/jobs/11').flush(aJob({ state: 'New' }));
    await nextTask();
    expect(expectRequest('PUT', '/api/jobs/11/state').request.body).toEqual({ state: 'Seen' });
  });

  it('links to the existing application instead of starting a second one', async () => {
    expectRequest('GET', '/api/jobs/11').flush(aJob({ applicationId: 21 }));
    await harness.fixture.whenStable();
    expect(byText(root, 'Open application').getAttribute('href')).toBe('/applications/21');
  });

  it('shows a not-found state for an unknown job, without a toast', async () => {
    expectRequest('GET', '/api/jobs/11').flush(
      { detail: 'Job 11 not found.' },
      { status: 404, statusText: 'Not Found' },
    );
    await harness.fixture.whenStable();
    expect(text(root.querySelector('h1'))).toBe('Job not found');
    expect(toasts()).toEqual([]);
  });
});
