import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { aBatch, aJobSummary, someSettings } from '../../../../testing/fixtures';
import {
  byText,
  click,
  expectRequest,
  http,
  nextTask,
  provideTestApp,
  settle,
  text,
} from '../../../../testing/test-app';
import JobListPage from './job-list-page';

@Component({ template: '' })
class BatchStub {}

describe('JobListPage', () => {
  let harness: RouterTestingHarness;
  let root: HTMLElement;

  async function open(
    url: string,
    jobs = [aJobSummary(), aJobSummary({ id: 12, title: 'UI Engineer', state: 'Bookmarked' })],
  ) {
    harness = await RouterTestingHarness.create(url);
    root = harness.routeNativeElement!;
    await settle();
    expectRequest('GET', '/api/settings').flush(someSettings({ nimApiKeyConfigured: true }));
    const request = expectRequest('GET', '/api/jobs');
    request.flush(jobs);
    await harness.fixture.whenStable();
    return request;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideTestApp([
        { path: 'jobs', component: JobListPage },
        { path: 'batches/:id', component: BatchStub },
      ]),
    });
  });

  it('lists jobs, marking new ones', async () => {
    const request = await open('/jobs');
    expect(request.request.params.keys()).toEqual([]);
    const rows = root.querySelectorAll('.job-row');
    expect(rows.length).toBe(2);
    expect(text(rows[0])).toContain('New: Frontend Developer');
    expect(text(rows[0])).toContain('Acme · Berlin · Hybrid · Bundesagentur für Arbeit');
  });

  it('asks the backend for the filter in the URL', async () => {
    const request = await open('/jobs?filter=bookmarked&q=angular');
    expect(request.request.params.get('state')).toBe('Bookmarked');
    expect(request.request.params.get('search')).toBe('angular');
    expect(root.querySelector('a[aria-current="page"]')?.textContent?.trim()).toBe('Bookmarked');
  });

  it('bookmarks a job and reloads the list', async () => {
    await open('/jobs');
    click(root.querySelector('[aria-label="Bookmark: Frontend Developer"]'));
    await nextTask();
    const request = expectRequest('PUT', '/api/jobs/11/state');
    expect(request.request.body).toEqual({ state: 'Bookmarked' });
    request.flush({});
    await nextTask();
    expectRequest('GET', '/api/jobs').flush([]);
  });

  it('starts a batch for the selected jobs after confirming the AI requests', async () => {
    await open('/jobs');
    click(root.querySelector('[aria-label="Select Frontend Developer at Acme"]'));
    click(root.querySelector('[aria-label="Select UI Engineer at Acme"]'));
    await harness.fixture.whenStable();
    expect(text(root.querySelector('.selection-bar'))).toContain('2 selected');

    click(byText(root, 'Generate letters…'));
    await harness.fixture.whenStable();
    expect(text(root.querySelector('app-batch-start-sheet'))).toContain(
      'This makes 2 AI requests to NVIDIA',
    );

    click(byText(root, 'Start 2 letters'));
    await nextTask();
    const request = expectRequest('POST', '/api/batches');
    expect(request.request.body).toEqual({ jobPostingIds: [11, 12], language: null });
    request.flush(aBatch());
    await nextTask();
    expect(TestBed.inject(Router).url).toBe('/batches/31');
    http().verify();
  });

  it('invites a first search when there are no jobs', async () => {
    await open('/jobs', []);
    expect(text(root)).toContain('No jobs yet');
    expect(byText(root, 'Search for jobs').getAttribute('href')).toBe('/search');
  });
});
