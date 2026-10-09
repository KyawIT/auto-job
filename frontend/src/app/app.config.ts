import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { Backend } from './core/backend';
import { backendInterceptor } from './core/backend.interceptor';
import { errorInterceptor } from './core/error.interceptor';
import { utcDatesInterceptor } from './core/utc-dates.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Error handling sees the original relative URL; the backend interceptor then rewrites it.
    provideHttpClient(
      withFetch(),
      withInterceptors([errorInterceptor, utcDatesInterceptor, backendInterceptor]),
    ),
    // Resolve the backend URL/token before anything makes an HTTP call.
    provideAppInitializer(() => inject(Backend).load()),
  ],
};
