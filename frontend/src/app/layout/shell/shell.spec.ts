import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { expectRequest, provideTestApp, settle, text } from '../../../testing/test-app';
import { Shell } from './shell';

@Component({ template: '<h1 tabindex="-1">Placeholder</h1>' })
class Placeholder {}

describe('Shell', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: provideTestApp([
        {
          path: '',
          component: Shell,
          children: ['jobs', 'search', 'applications', 'profile', 'documents', 'settings'].map(
            (path) => ({
              path,
              component: Placeholder,
            }),
          ),
        },
      ]),
    });
    harness = await RouterTestingHarness.create('/jobs');
    await settle();
  });

  it('lists every area and marks the current one', () => {
    const links = Array.from(
      harness.routeNativeElement!.querySelectorAll<HTMLAnchorElement>('nav[aria-label="Main"] a'),
    );
    expect(links.map((a) => text(a))).toEqual([
      'Jobs',
      'Search',
      'Applications',
      'Profile',
      'Documents',
      'Settings',
    ]);
    expect(links[0].getAttribute('aria-current')).toBe('page');
  });

  it('shows whether the backend answers', async () => {
    expectRequest('GET', '/health').flush({ status: 'ok' });
    await harness.fixture.whenStable();
    expect(text(harness.routeNativeElement!.querySelector('[role="status"]'))).toContain(
      'Backend connected',
    );
  });

  it('reports an unreachable backend', async () => {
    expectRequest('GET', '/health').error(new ProgressEvent('error'));
    await harness.fixture.whenStable();
    expect(text(harness.routeNativeElement!.querySelector('[role="status"]'))).toContain(
      'Backend unreachable',
    );
  });

  it('switches sections with Ctrl + number', async () => {
    expectRequest('GET', '/health').flush({ status: 'ok' });
    document.dispatchEvent(
      new KeyboardEvent('keydown', { key: '2', ctrlKey: true, bubbles: true }),
    );
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/search');
  });
});
