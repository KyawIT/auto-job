import { Service } from '@angular/core';
import './desktop'; // declares window.autojob

/**
 * Location of the C# sidecar. Inside Electron it comes from the preload bridge;
 * in a plain browser (`ng serve` + `dotnet run`) it falls back to the dev backend.
 */
@Service()
export class Backend {
  url = 'http://127.0.0.1:5246';
  token = '';

  async load(): Promise<void> {
    const info = await window.autojob?.getBackend();
    if (info) {
      this.url = info.url;
      this.token = info.token;
    }
  }
}
