import { TestBed } from '@angular/core/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { someSettings } from '../../../../testing/fixtures';
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
import SettingsPage from './settings-page';

describe('SettingsPage', () => {
  let harness: RouterTestingHarness;
  let root: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: provideTestApp([{ path: 'settings', component: SettingsPage }]),
    });
    harness = await RouterTestingHarness.create('/settings');
    root = harness.routeNativeElement!;
    await settle();
    expectRequest('GET', '/api/settings').flush(someSettings({ nimApiKeyConfigured: true }));
    await harness.fixture.whenStable();
  });

  it('shows that a key is configured without revealing it', () => {
    const keyField = root.querySelector<HTMLInputElement>('#key-nim')!;
    expect(keyField.value).toBe('');
    expect(keyField.type).toBe('password');
    expect(text(keyField.closest('.field'))).toContain('Configured');
    expect(text(root)).toContain('Remove NVIDIA key');
  });

  it('removes a key by sending an empty value', async () => {
    click(byText(root, 'Remove NVIDIA key'));
    await nextTask();
    const request = expectRequest('PUT', '/api/settings/secrets');
    expect(request.request.body).toEqual({ nimApiKey: '' });
    request.flush(someSettings());
    await nextTask();
    expectRequest('GET', '/api/settings').flush(someSettings());
    await harness.fixture.whenStable();
    expect(text(root.querySelector('#key-nim')!.closest('.field'))).toContain('Not set');
  });

  it('saves preferences and shows a range error from the client', async () => {
    type(root.querySelector('#pref-parallel'), '9');
    await harness.fixture.whenStable();
    click(byText(root, 'Save preferences'));
    await harness.fixture.whenStable();
    expect(text(root.querySelector('#pref-parallel-errors'))).toBe('The value is too large.');

    type(root.querySelector('#pref-parallel'), '3');
    type(root.querySelector('#pref-layout'), 'International');
    await harness.fixture.whenStable();
    click(byText(root, 'Save preferences'));
    await nextTask();
    const request = expectRequest('PUT', '/api/settings');
    expect(request.request.body).toEqual({
      defaultLetterLanguage: 'de',
      nimModel: null,
      letterLayout: 'International',
      maxParallelGenerations: 3,
      defaultSearchLocation: 'Berlin',
      defaultSearchRadiusKm: 25,
    });
  });
});
