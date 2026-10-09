import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Notifications } from './notifications';

export interface BackendInfo {
  url: string;
  token: string;
}

/** Per key: undefined = unchanged, '' = remove, anything else = set. */
export interface ApiKeys {
  nimApiKey?: string;
  adzunaAppId?: string;
  adzunaAppKey?: string;
}

/** What electron/preload.js exposes. Undefined when running in a plain browser. */
export interface AutojobBridge {
  getBackend(): Promise<BackendInfo>;
  setApiKeys(keys: ApiKeys): Promise<void>;
  openFile(fullPath: string): Promise<boolean>;
  saveFile(suggestedName: string, bytes: Uint8Array): Promise<boolean>;
}

declare global {
  interface Window {
    autojob?: AutojobBridge;
  }
}

/**
 * Typed access to the Electron bridge with a browser fallback, so every feature works under
 * plain `ng serve` too: keys go straight to the backend, exports download via `<a download>`.
 */
@Service()
export class Desktop {
  private readonly http = inject(HttpClient);
  private readonly notifications = inject(Notifications);
  private readonly bridge = typeof window === 'undefined' ? undefined : window.autojob;

  readonly isElectron = !!this.bridge;

  /** Electron encrypts the keys with safeStorage, keeps them across restarts and forwards them. */
  async setApiKeys(keys: ApiKeys): Promise<void> {
    if (this.bridge) {
      try {
        await this.bridge.setApiKeys(keys);
      } catch (error) {
        this.notifications.error('The keys could not be saved.');
        throw error;
      }
    } else {
      await firstValueFrom(this.http.put('/api/settings/secrets', keys));
    }
  }

  /** Opens a file from the data folder with its default app. False when not possible (browser). */
  async openFile(fullPath: string): Promise<boolean> {
    return this.bridge ? this.bridge.openFile(fullPath) : false;
  }

  /** Save dialog in Electron, a regular download in the browser. False when the user cancelled. */
  async saveFile(suggestedName: string, file: Blob): Promise<boolean> {
    if (this.bridge) {
      return this.bridge.saveFile(suggestedName, new Uint8Array(await file.arrayBuffer()));
    }
    const url = URL.createObjectURL(file);
    const link = document.createElement('a');
    link.href = url;
    link.download = suggestedName;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    return true;
  }
}
