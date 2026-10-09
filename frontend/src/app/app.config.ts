import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { Backend } from './core/backend';
import { backendInterceptor } from './core/backend.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([backendInterceptor])),
    // Resolve the backend URL/token before anything makes an HTTP call.
    provideAppInitializer(() => inject(Backend).load()),
  ],
};
