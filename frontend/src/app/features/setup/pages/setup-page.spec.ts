import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Component } from '@angular/core';
import { aProfile, someSettings } from '../../../../testing/fixtures';
import {
  byText,
  click,
  expectRequest,
  nextTask,
  provideTestApp,
  settle,
  text,
  type,
} from '../../../../testing/test-app';
import { SetupStatus } from '../../../core/setup.guard';
import SetupPage from './setup-page';

@Component({ template: '' })
class Jobs {}

describe('SetupPage', () => {
  let harness: RouterTestingHarness;
  let root: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: provideTestApp([
        { path: 'setup', component: SetupPage },
        { path: 'jobs', component: Jobs },
      ]),
    });
    harness = await RouterTestingHarness.create('/setup');
    root = harness.routeNativeElement!;
    await settle();
    expectRequest('GET', '/api/profile').flush(
      { detail: 'Profile not found.' },
      { status: 404, statusText: 'Not Found' },
    );
    expectRequest('GET', '/api/settings').flush(someSettings());
    await harness.fixture.whenStable();
  });

  it('starts with an empty personal data step', () => {
    expect(root.querySelector('[aria-current="step"]')?.textContent).toContain('About you');
    expect((root.querySelector('#pd-first-name') as HTMLInputElement).value).toBe('');
  });

  it('saves the profile, then asks for the key, which can be skipped', async () => {
    type(root.querySelector('#pd-first-name'), 'Ada');
    type(root.querySelector('#pd-last-name'), 'Lovelace');
    type(root.querySelector('#pd-email'), 'ada@example.com');
    click(byText(root, 'Continue'));
    await nextTask();
    expectRequest('PUT', '/api/profile').flush(aProfile());
    await nextTask();

    expect(TestBed.inject(SetupStatus).hasProfile).toBe(true);
    expect(text(root.querySelector('#step2'))).toBe('Connect the AI');
    expect(root.querySelector('#key-adzuna-id')).toBeNull();

    click(byText(root, 'Skip'));
    await harness.fixture.whenStable();
    expect(text(root.querySelector('#step3'))).toBe('Import your CV');

    click(byText(root, 'Skip and finish'));
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/jobs');
  });

  it('sends only the entered key to the backend (browser fallback)', async () => {
    TestBed.inject(SetupStatus).hasProfile = true;
    type(root.querySelector('#pd-first-name'), 'Ada');
    type(root.querySelector('#pd-last-name'), 'Lovelace');
    type(root.querySelector('#pd-email'), 'ada@example.com');
    click(byText(root, 'Continue'));
    await nextTask();
    expectRequest('PUT', '/api/profile').flush(aProfile());
    await nextTask();

    type(root.querySelector('#key-nim'), 'nvapi-secret');
    click(byText(root, 'Save and continue'));
    await nextTask();
    const request = expectRequest('PUT', '/api/settings/secrets');
    expect(request.request.body).toEqual({ nimApiKey: 'nvapi-secret' });
    request.flush(someSettings({ nimApiKeyConfigured: true }));
    await nextTask();

    expect(text(root.querySelector('#step3'))).toBe('Import your CV');
  });
});
