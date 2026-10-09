import { Component, inject } from '@angular/core';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { PageHeader } from '../../../shared/page-header/page-header';
import { ApiKeysForm } from '../components/api-keys-form';
import { PreferencesForm } from '../components/preferences-form';
import { SettingsApi } from '../settings.api';

@Component({
  selector: 'app-settings-page',
  imports: [PageHeader, EmptyState, ApiKeysForm, PreferencesForm],
  template: `
    <app-page-header heading="Settings" />

    @if (settings.hasValue()) {
      @let s = settings.value();
      <div class="stack">
        <section class="card" aria-labelledby="keys-heading">
          <div class="card-header"><h2 class="title-2" id="keys-heading">API keys</h2></div>
          <app-api-keys-form
            [nimConfigured]="s.nimApiKeyConfigured"
            [adzunaConfigured]="s.adzunaConfigured"
            (saved)="settings.reload()"
          />
        </section>
        <section class="card" aria-labelledby="prefs-heading">
          <div class="card-header">
            <h2 class="title-2" id="prefs-heading">Letters & search</h2>
          </div>
          <app-preferences-form [settings]="s" />
        </section>
      </div>
    } @else if (settings.error()) {
      <app-empty-state
        symbol="!"
        heading="Settings not available"
        message="The settings could not be loaded."
      >
        <button type="button" class="btn" (click)="settings.reload()">Try again</button>
      </app-empty-state>
    } @else {
      <div class="card stack" role="status" aria-busy="true" aria-label="Loading settings">
        <span class="skeleton" style="width: 30%"></span>
        <span class="skeleton"></span>
      </div>
    }
  `,
})
export default class SettingsPage {
  protected readonly settings = inject(SettingsApi).settingsResource();
}
