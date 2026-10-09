import { TestBed } from '@angular/core/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { aJobSummary, someSettings } from '../../../../testing/fixtures';
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
import SearchPage from './search-page';

describe('SearchPage', () => {
  let harness: RouterTestingHarness;
  let root: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: provideTestApp([{ path: 'search', component: SearchPage }]),
    });
    harness = await RouterTestingHarness.create('/search');
    root = harness.routeNativeElement!;
    await settle();
    expectRequest('GET', '/api/settings').flush(someSettings({ adzunaConfigured: true }));
    await harness.fixture.whenStable();
  });

  it('starts from the default location and radius in Settings', () => {
    expect(root.querySelector<HTMLInputElement>('#s-location')!.value).toBe('Berlin');
    expect(root.querySelector<HTMLInputElement>('#s-radius')!.value).toBe('25');
  });

  it('searches the selected sources and shows results plus per-source errors', async () => {
    type(root.querySelector('#s-keywords'), 'Angular');
    click(byText(root, 'Search', 'button[type="submit"]'));
    await nextTask();

    const request = expectRequest('POST', '/api/jobs/search');
    expect(request.request.body).toEqual({
      keywords: 'Angular',
      location: 'Berlin',
      radiusKm: 25,
      sources: ['BundesagenturFuerArbeit', 'Adzuna'],
    });
    request.flush({
      jobs: [aJobSummary()],
      newCount: 1,
      errors: [{ source: 'Adzuna', message: 'Adzuna keys are not configured.' }],
    });
    await nextTask();

    expect(text(root.querySelector('.summary [role="status"]'))).toBe(
      '1 posting found, 1 new in your jobs.',
    );
    expect(text(root.querySelector('[role="alert"]'))).toContain(
      'Adzuna: Adzuna keys are not configured.',
    );
    expect(root.querySelector('.list a')?.getAttribute('href')).toBe('/jobs/11');
  });

  it('needs keywords and at least one source', async () => {
    root.querySelectorAll<HTMLInputElement>('.sources input').forEach((box) => box.click());
    click(byText(root, 'Search', 'button[type="submit"]'));
    await harness.fixture.whenStable();
    expect(text(root.querySelector('#s-keywords-errors'))).toBe('This field is required.');
    expect(text(root.querySelector('#s-adzuna-errors'))).toBe('Select at least one source.');
  });
});
