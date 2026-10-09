import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiKeys, Desktop } from '../../core/desktop';
import { Settings, UpdateSettingsRequest } from './settings.models';

@Service()
export class SettingsApi {
  private readonly http = inject(HttpClient);
  private readonly desktop = inject(Desktop);

  settingsResource() {
    return httpResource<Settings>(() => '/api/settings');
  }

  update(request: UpdateSettingsRequest): Promise<Settings> {
    return firstValueFrom(this.http.put<Settings>('/api/settings', request));
  }

  /** Goes through Electron so the keys are stored encrypted and survive a restart. */
  setApiKeys(keys: ApiKeys): Promise<void> {
    return this.desktop.setApiKeys(keys);
  }
}
