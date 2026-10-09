import { TestBed } from '@angular/core/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { aDocument, aProfile } from '../../../../testing/fixtures';
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
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import DocumentsPage from './documents-page';

describe('DocumentsPage', () => {
  let harness: RouterTestingHarness;
  let root: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: provideTestApp([{ path: 'documents', component: DocumentsPage }]),
    });
    harness = await RouterTestingHarness.create('/documents');
    root = harness.routeNativeElement!;
    await settle();
    expectRequest('GET', '/api/documents').flush([
      aDocument(),
      aDocument({
        id: 6,
        type: 'Certification',
        displayName: 'Scan',
        extractionStatus: 'NotSupported',
        attachByDefault: false,
      }),
    ]);
    await harness.fixture.whenStable();
  });

  it('lists documents with type and extraction status', () => {
    const rows = root.querySelectorAll('.list-row');
    expect(text(rows[0])).toContain('My CV');
    expect(text(rows[0])).toContain('CV · 117 KB');
    expect(text(rows[1])).toContain('Not supported');
    expect(text(rows[1])).toContain('no readable text');
  });

  it('uploads a dropped file with the chosen type', async () => {
    const file = new File(['hello'], 'reference.pdf', { type: 'application/pdf' });
    const select = root.querySelector<HTMLSelectElement>('#upload-type')!;
    select.value = 'EmploymentReference';
    select.dispatchEvent(new Event('change'));
    const drop = new Event('drop') as Event & { dataTransfer: unknown };
    drop.dataTransfer = { files: [file] };
    root.querySelector('app-file-drop')!.dispatchEvent(drop);
    await nextTask();

    const request = expectRequest('POST', '/api/documents');
    const body = request.request.body as FormData;
    expect((body.get('file') as File).name).toBe('reference.pdf');
    expect(body.get('type')).toBe('EmploymentReference');
    request.flush(aDocument({ id: 7, displayName: 'reference', type: 'EmploymentReference' }));
    await nextTask();
    expectRequest('GET', '/api/documents').flush([aDocument()]);
  });

  it('explains a 409 when deleting an attached document', async () => {
    click(root.querySelector('[aria-label="Delete My CV"]'));
    await harness.fixture.whenStable();
    TestBed.inject(Confirm).answer(true);
    await nextTask();

    expectRequest('DELETE', '/api/documents/5').flush(
      { detail: 'The document is attached to an application.' },
      { status: 409, statusText: 'Conflict' },
    );
    await nextTask();
    expect(toasts()).toEqual(['The document is attached to an application.']);
    expectRequest('GET', '/api/documents').flush([aDocument()]);
  });

  it('fills the profile from a CV after the user agrees to send it', async () => {
    click(byText(root, 'Fill profile from CV'));
    await harness.fixture.whenStable();
    const confirm = TestBed.inject(Confirm);
    expect(confirm.pending()?.message).toContain("sent to NVIDIA's AI service");
    confirm.answer(true);
    await nextTask();

    const request = expectRequest('POST', '/api/profile/import-cv');
    expect(request.request.body).toEqual({ documentId: 5 });
    request.flush(aProfile());
    await nextTask();
    expect(toasts()).toEqual(['Profile filled from your CV.']);
  });
});
