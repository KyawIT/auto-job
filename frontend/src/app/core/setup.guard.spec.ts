import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { expectRequest, nextTask, provideTestApp, toasts } from '../../testing/test-app';
import { setupGuard } from './setup.guard';

@Component({ template: '' })
class Page {}

describe('setupGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideTestApp([
        { path: 'setup', component: Page },
        { path: 'jobs', component: Page, canActivate: [setupGuard] },
      ]),
    });
  });

  it('sends a fresh install (no profile) to the wizard, without an error toast', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/jobs');
    await nextTask();
    expectRequest('GET', '/api/profile').flush(
      { detail: 'Profile not found.' },
      { status: 404, statusText: 'Not Found' },
    );
    await navigation;
    expect(TestBed.inject(Router).url).toBe('/setup');
    expect(toasts()).toEqual([]);
  });

  it('lets the user in once a profile exists, and stops asking', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/jobs');
    await nextTask();
    expectRequest('GET', '/api/profile').flush({ id: 1 });
    await navigation;
    expect(TestBed.inject(Router).url).toBe('/jobs');

    await harness.navigateByUrl('/setup');
    await harness.navigateByUrl('/jobs');
    expect(TestBed.inject(Router).url).toBe('/jobs');
  });
});
