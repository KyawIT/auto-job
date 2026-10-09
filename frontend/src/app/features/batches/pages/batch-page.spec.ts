import { TestBed } from '@angular/core/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { aBatch } from '../../../../testing/fixtures';
import {
  byText,
  click,
  expectRequest,
  nextTask,
  provideTestApp,
  settle,
  text,
} from '../../../../testing/test-app';
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import BatchPage from './batch-page';

describe('BatchPage', () => {
  let harness: RouterTestingHarness;
  let root: HTMLElement;

  beforeEach(async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    TestBed.configureTestingModule({
      providers: provideTestApp([{ path: 'batches/:id', component: BatchPage }]),
    });
    harness = await RouterTestingHarness.create('/batches/31');
    root = harness.routeNativeElement!;
    await settle();
    expectRequest('GET', '/api/batches/31').flush(aBatch());
    await harness.fixture.whenStable();
  });

  afterEach(() => vi.useRealTimers());

  it('shows progress and every job with its status', () => {
    const bar = root.querySelector('[role="progressbar"]')!;
    expect(bar.getAttribute('aria-valuenow')).toBe('1');
    expect(bar.getAttribute('aria-valuetext')).toBe('1 of 3 finished');
    expect(root.querySelectorAll('.tasks li').length).toBe(3);
    expect(root.querySelector('.tasks a')?.getAttribute('href')).toBe('/applications/21');
  });

  it('polls while running and stops once finished', async () => {
    vi.advanceTimersByTime(1500);
    await nextTask();
    expectRequest('GET', '/api/batches/31').flush(
      aBatch({
        status: 'Completed',
        progress: { total: 3, queued: 0, running: 0, done: 3, failed: 0, cancelled: 0 },
      }),
    );
    await harness.fixture.whenStable();
    expect(text(root.querySelector('h1'))).toBe('All letters written');

    vi.advanceTimersByTime(3000);
    await nextTask();
    expect(() => expectRequest('GET', '/api/batches/31')).toThrow();
  });

  it('retries failed tasks', async () => {
    vi.advanceTimersByTime(1500);
    await nextTask();
    expectRequest('GET', '/api/batches/31').flush(
      aBatch({
        status: 'Completed',
        progress: { total: 3, queued: 0, running: 0, done: 2, failed: 1, cancelled: 0 },
      }),
    );
    await harness.fixture.whenStable();
    expect(text(root.querySelector('h1'))).toBe('Finished with problems');

    click(byText(root, 'Retry 1 failed'));
    await nextTask();
    expectRequest('POST', '/api/batches/31/retry').flush(aBatch());
  });

  it('cancels after confirmation', async () => {
    click(byText(root, 'Cancel batch'));
    await harness.fixture.whenStable();
    TestBed.inject(Confirm).answer(true);
    await nextTask();
    expectRequest('POST', '/api/batches/31/cancel').flush(aBatch({ status: 'Cancelled' }));
    await nextTask();
    expect(text(root.querySelector('h1'))).toBe('Batch cancelled');
  });
});
