import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { EnvironmentProviders, Provider } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Routes, withComponentInputBinding } from '@angular/router';
import { errorInterceptor } from '../app/core/error.interceptor';
import { Notifications } from '../app/core/notifications';

/** What the real app has, minus the backend URL rewrite: requests keep their relative `/api/...` URLs. */
export function provideTestApp(routes: Routes = []): (Provider | EnvironmentProviders)[] {
  return [
    provideHttpClient(withInterceptors([errorInterceptor])),
    provideHttpClientTesting(),
    provideRouter(routes, withComponentInputBinding()),
  ];
}

export function http(): HttpTestingController {
  return TestBed.inject(HttpTestingController);
}

/** Lets effects run (resources start their requests) and waits for the UI to settle. */
export async function settle(): Promise<void> {
  TestBed.tick();
  await Promise.resolve();
  TestBed.tick();
}

/**
 * Waits for async work queued as macrotasks (guards, awaited promises in handlers),
 * then lets effects run so resources that were reloaded send their requests.
 */
export async function nextTask(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve));
  await settle();
}

/** The single pending request matching method + URL (query string ignored). */
export function expectRequest(method: string, url: string): TestRequest {
  return http().expectOne((r) => r.method === method && r.url === url, `${method} ${url}`);
}

export function toasts(): string[] {
  return TestBed.inject(Notifications)
    .toasts()
    .map((t) => t.message);
}

export function text(root: Element | null | undefined): string {
  return (root?.textContent ?? '').replace(/\s+/g, ' ').trim();
}

/** Types into a native input/textarea/select the way a user would. */
export function type(element: Element | null, value: string): void {
  const input = element as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input'));
  input.dispatchEvent(new Event('change'));
}

export function click(element: Element | null): void {
  (element as HTMLElement).click();
}

/** First button/link whose text contains `label`. */
export function byText(root: Element, label: string, selector = 'button, a'): HTMLElement {
  const match = Array.from(root.querySelectorAll<HTMLElement>(selector)).find((el) =>
    text(el).includes(label),
  );
  if (!match) throw new Error(`No ${selector} with text "${label}"`);
  return match;
}
