import { TestBed } from '@angular/core/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { anApplication, aDocument, withLetter } from '../../../../testing/fixtures';
import {
  byText,
  click,
  expectRequest,
  nextTask,
  provideTestApp,
  settle,
  text,
  toasts,
  type,
} from '../../../../testing/test-app';
import { Notifications } from '../../../core/notifications';
import ApplicationDetailPage from './application-detail-page';

describe('ApplicationDetailPage', () => {
  let harness: RouterTestingHarness;
  let root: HTMLElement;

  async function open(application = anApplication()) {
    harness = await RouterTestingHarness.create('/applications/21');
    root = harness.routeNativeElement!;
    await settle();
    expectRequest('GET', '/api/applications/21').flush(application);
    await nextTask();
    expectRequest('GET', '/api/documents').flush([aDocument()]);
    await harness.fixture.whenStable();
  }

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'], shouldAdvanceTime: true });
    TestBed.configureTestingModule({
      providers: provideTestApp([{ path: 'applications/:id', component: ApplicationDetailPage }]),
    });
  });

  afterEach(() => vi.useRealTimers());

  it('offers to generate when there is no letter yet, and says what leaves the machine', async () => {
    await open();
    expect(text(root)).toContain('No letter yet');
    expect(text(root)).toContain("sends your profile and this job posting to NVIDIA's AI service");
  });

  it('generates a letter and shows it in the editor', async () => {
    await open();
    click(byText(root, 'Generate letter'));
    await nextTask();
    expect(text(root.querySelector('.generating'))).toContain('Writing your letter…');

    expectRequest('POST', '/api/applications/21/generate').flush(
      withLetter(anApplication(), 'Dear Acme team'),
    );
    await nextTask();
    expect(root.querySelector<HTMLTextAreaElement>('#text-letter')!.value).toBe('Dear Acme team');
    expect(root.querySelector<HTMLInputElement>('#text-subject')!.value).toBe('Application');
  });

  it('offers a retry when the AI fails', async () => {
    await open();
    click(byText(root, 'Generate letter'));
    await nextTask();
    expectRequest('POST', '/api/applications/21/generate').flush(
      { type: 'ai-failed', detail: 'The AI service answered 500.' },
      { status: 502, statusText: 'Bad Gateway' },
    );
    await nextTask();
    const [toast] = TestBed.inject(Notifications).toasts();
    expect(toast.message).toBe('The AI service answered 500.');
    expect(toast.action?.label).toBe('Retry');
  });

  it('autosaves the edited letter shortly after typing stops', async () => {
    await open(withLetter(anApplication()));
    type(root.querySelector('#text-letter'), 'Dear Acme, I would love to join.');
    await vi.advanceTimersByTimeAsync(900);
    await nextTask();

    const request = expectRequest('PUT', '/api/texts/101');
    expect(request.request.body).toEqual({ content: 'Dear Acme, I would love to join.' });
    request.flush({
      ...withLetter(anApplication()).texts[0],
      content: 'Dear Acme, I would love to join.',
      isEdited: true,
    });
    await nextTask();
    expect(text(root.querySelector('app-text-editor'))).toContain('Saved');
    expectRequest('GET', '/api/applications/21').flush(withLetter(anApplication()));
  });

  it('restores an older version', async () => {
    await open(withLetter(anApplication()));
    click(byText(root.querySelector('app-text-editor')!, 'Versions'));
    await nextTask();
    const old = {
      ...withLetter(anApplication()).texts[0],
      id: 90,
      isCurrent: false,
      content: 'Old letter',
    };
    expectRequest('GET', '/api/applications/21/texts').flush([
      withLetter(anApplication()).texts[0],
      old,
    ]);
    await harness.fixture.whenStable();

    click(byText(root, 'Use this version'));
    await nextTask();
    expectRequest('POST', '/api/texts/90/select').flush(withLetter(anApplication(), 'Old letter'));
    await nextTask();
    expect(root.querySelector<HTMLTextAreaElement>('#text-letter')!.value).toBe('Old letter');
  });

  it('changes the status with a note', async () => {
    await open(withLetter(anApplication()));
    type(root.querySelector('#st-status'), 'Sent');
    await harness.fixture.whenStable();
    type(root.querySelector('#st-note'), 'Sent by mail');
    click(byText(root, 'Change status'));
    await nextTask();
    const request = expectRequest('PUT', '/api/applications/21/status');
    expect(request.request.body).toEqual({ status: 'Sent', note: 'Sent by mail' });
    request.flush(withLetter(anApplication({ status: 'Sent' })));
    await nextTask();
    expect(text(root.querySelector('app-page-header app-status-badge'))).toBe('Sent');
  });

  it('attaches a document', async () => {
    await open(withLetter(anApplication()));
    const select = root.querySelector<HTMLSelectElement>('#attach-add')!;
    select.value = '5';
    select.dispatchEvent(new Event('change'));
    await harness.fixture.whenStable();
    click(byText(root, 'Save attachments'));
    await nextTask();
    expect(expectRequest('PUT', '/api/applications/21/attachments').request.body).toEqual({
      documentIds: [5],
    });
  });

  it('prepares the email draft (browser: tells where the file is)', async () => {
    await open(withLetter(anApplication()));
    click(byText(root, 'Prepare email'));
    await nextTask();
    expectRequest('POST', '/api/applications/21/email-draft').flush({
      relativePath: 'drafts/a.eml',
      fullPath: 'C:/data/drafts/a.eml',
    });
    await nextTask();
    expect(toasts()).toEqual([
      'Email draft saved to C:/data/drafts/a.eml. Open it with your mail app.',
    ]);
    expectRequest('GET', '/api/applications/21').flush(withLetter(anApplication()));
  });
});
