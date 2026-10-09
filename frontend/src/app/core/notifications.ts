import { Service, signal } from '@angular/core';

export type ToastTone = 'info' | 'success' | 'error';

export type ToastAction = { label: string; run: () => void } | { label: string; link: string };

export interface Toast {
  id: number;
  tone: ToastTone;
  message: string;
  action?: ToastAction;
}

export interface ToastOptions {
  tone?: ToastTone;
  action?: ToastAction;
}

/** Info/success toasts leave on their own; errors stay until dismissed so nobody misses them. */
const autoDismissMs = 5000;

@Service()
export class Notifications {
  private nextId = 1;
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();
  private readonly list = signal<readonly Toast[]>([]);
  readonly toasts = this.list.asReadonly();

  show(message: string, options: ToastOptions = {}): number {
    const toast: Toast = {
      id: this.nextId++,
      tone: options.tone ?? 'info',
      message,
      action: options.action,
    };
    // Same message twice in a row (e.g. several failing requests) shows once.
    if (this.list().some((t) => t.message === message && t.tone === toast.tone)) return toast.id;
    this.list.update((list) => [...list.slice(-3), toast]);
    if (toast.tone !== 'error') this.schedule(toast.id);
    return toast.id;
  }

  success(message: string): number {
    return this.show(message, { tone: 'success' });
  }

  error(message: string, action?: ToastAction): number {
    return this.show(message, { tone: 'error', action });
  }

  dismiss(id: number): void {
    clearTimeout(this.timers.get(id));
    this.timers.delete(id);
    this.list.update((list) => list.filter((t) => t.id !== id));
  }

  /** Hover or focus on a toast pauses its timer (WCAG 2.2.1, timing adjustable). */
  pause(id: number): void {
    clearTimeout(this.timers.get(id));
    this.timers.delete(id);
  }

  resume(id: number): void {
    const toast = this.list().find((t) => t.id === id);
    if (toast && toast.tone !== 'error') this.schedule(id);
  }

  private schedule(id: number): void {
    clearTimeout(this.timers.get(id));
    this.timers.set(
      id,
      setTimeout(() => this.dismiss(id), autoDismissMs),
    );
  }
}
