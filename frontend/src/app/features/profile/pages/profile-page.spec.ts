import { TestBed } from '@angular/core/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { aProfile } from '../../../../testing/fixtures';
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
import { Confirm } from '../../../shared/confirm-dialog/confirm-dialog';
import ProfilePage from './profile-page';

describe('ProfilePage', () => {
  let harness: RouterTestingHarness;
  let root: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: provideTestApp([{ path: 'profile', component: ProfilePage }]),
    });
    harness = await RouterTestingHarness.create('/profile');
    root = harness.routeNativeElement!;
    await settle();
    expectRequest('GET', '/api/profile').flush(aProfile());
    await harness.fixture.whenStable();
  });

  it('shows personal data and every list section', () => {
    expect((root.querySelector('#pd-first-name') as HTMLInputElement).value).toBe('Ada');
    expect(text(root)).toContain('Programmer · Analytical Engines');
    expect(text(root)).toContain('Mathematics');
    expect(text(root)).toContain('No education yet.');
  });

  it('saves personal data and shows server validation errors on the field', async () => {
    type(root.querySelector('#pd-email'), 'ada@new.example');
    click(byText(root, 'Save', 'button[type="submit"]'));
    await nextTask();

    const request = expectRequest('PUT', '/api/profile');
    expect(request.request.body).toMatchObject({
      firstName: 'Ada',
      email: 'ada@new.example',
      phone: null,
    });
    request.flush(
      { title: 'Invalid', errors: { Email: ['Not a valid email address.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await nextTask();
    await harness.fixture.whenStable();

    expect(text(root.querySelector('#pd-email-errors'))).toBe('Not a valid email address.');
    expect(root.querySelector('#pd-email')!.getAttribute('aria-invalid')).toBe('true');
  });

  it('blocks submitting without required fields', async () => {
    type(root.querySelector('#pd-first-name'), '');
    click(byText(root, 'Save', 'button[type="submit"]'));
    await harness.fixture.whenStable();

    expect(text(root.querySelector('#pd-first-name-errors'))).toBe('This field is required.');
    expect(() => expectRequest('PUT', '/api/profile')).toThrow();
  });

  it('adds a skill through the sheet and reloads the profile', async () => {
    click(byText(root.querySelector('[aria-labelledby="skills-heading"]')!, 'Add'));
    await harness.fixture.whenStable();
    type(root.querySelector('#sk-name'), 'Angular');
    type(root.querySelector('#sk-level'), 'Advanced');
    click(byText(root.querySelector('app-skill-form')!, 'Save'));
    await nextTask();

    const request = expectRequest('POST', '/api/profile/skills');
    expect(request.request.body).toEqual({ name: 'Angular', level: 'Advanced' });
    request.flush({ id: 9, name: 'Angular', level: 'Advanced' });
    await nextTask();
    expectRequest('GET', '/api/profile').flush(
      aProfile({ skills: [{ id: 9, name: 'Angular', level: 'Advanced' }] }),
    );
    await harness.fixture.whenStable();

    expect(root.querySelector('app-skill-form')).toBeNull();
    expect(text(root)).toContain('Angular');
  });

  it('deletes an entry only after confirmation', async () => {
    const confirm = TestBed.inject(Confirm);
    click(root.querySelector('[aria-label="Delete Mathematics"]'));
    await harness.fixture.whenStable();
    expect(confirm.pending()?.title).toBe('Delete skill?');

    confirm.answer(true);
    await nextTask();
    expectRequest('DELETE', '/api/profile/skills/3').flush(null);
    await nextTask();
    expectRequest('GET', '/api/profile').flush(aProfile({ skills: [] }));
  });
});
