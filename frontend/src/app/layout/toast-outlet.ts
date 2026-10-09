import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Notifications, Toast } from '../core/notifications';

/** Apple's projection: where a flick would come to rest (exponential deceleration). */
function project(velocityPxPerS: number, decelerationRate = 0.998): number {
  return ((velocityPxPerS / 1000) * decelerationRate) / (1 - decelerationRate);
}

interface Drag {
  id: number;
  el: HTMLElement;
  startX: number;
  x: number;
  samples: { x: number; t: number }[];
}

/**
 * Toasts slide in from the bottom-right and leave the same way. They can be flicked away
 * sideways: the toast tracks the pointer 1:1, and on release the throw is projected forward
 * to decide between dismissing and springing back.
 */
@Component({
  selector: 'app-toast-outlet',
  imports: [RouterLink],
  template: `
    <section class="outlet" aria-label="Notifications" aria-live="polite">
      @for (toast of notifications.toasts(); track toast.id) {
        <div
          class="toast"
          [class]="'toast--' + toast.tone"
          [attr.role]="toast.tone === 'error' ? 'alert' : 'status'"
          animate.enter="toast-enter"
          animate.leave="toast-leave"
          (pointerdown)="grab($event, toast)"
          (pointermove)="move($event)"
          (pointerup)="release($event)"
          (pointercancel)="release($event)"
          (mouseenter)="notifications.pause(toast.id)"
          (mouseleave)="notifications.resume(toast.id)"
          (focusin)="notifications.pause(toast.id)"
          (focusout)="notifications.resume(toast.id)"
        >
          <span class="icon" aria-hidden="true">{{ icon(toast) }}</span>
          <p class="message">{{ toast.message }}</p>
          @if (toast.action; as action) {
            @if ('link' in action) {
              <a
                class="btn btn--small btn--plain"
                [routerLink]="action.link"
                (click)="notifications.dismiss(toast.id)"
                >{{ action.label }}</a
              >
            } @else {
              <button
                type="button"
                class="btn btn--small btn--plain"
                (click)="action.run(); notifications.dismiss(toast.id)"
              >
                {{ action.label }}
              </button>
            }
          }
          <button
            type="button"
            class="btn btn--small btn--icon close"
            aria-label="Dismiss notification"
            (click)="notifications.dismiss(toast.id)"
          >
            <span aria-hidden="true">✕</span>
          </button>
        </div>
      }
    </section>
  `,
  styleUrl: './toast-outlet.scss',
})
export class ToastOutlet {
  protected readonly notifications = inject(Notifications);
  private drag: Drag | null = null;

  protected icon(toast: Toast): string {
    return toast.tone === 'error' ? '!' : toast.tone === 'success' ? '✓' : 'i';
  }

  protected grab(event: PointerEvent, toast: Toast): void {
    if ((event.target as HTMLElement).closest('button, a')) return;
    const el = event.currentTarget as HTMLElement;
    el.setPointerCapture(event.pointerId);
    // Start from the on-screen position so grabbing a toast mid-spring doesn't jump.
    const current = new DOMMatrixReadOnly(getComputedStyle(el).transform).m41;
    el.style.transition = 'none';
    el.style.transform = `translateX(${current}px)`;
    this.notifications.pause(toast.id);
    this.drag = {
      id: toast.id,
      el,
      startX: event.clientX - current,
      x: current,
      samples: [{ x: current, t: event.timeStamp }],
    };
  }

  protected move(event: PointerEvent): void {
    const drag = this.drag;
    if (!drag) return;
    const raw = event.clientX - drag.startX;
    // Toasts only leave to the right; dragging left rubber-bands.
    drag.x = raw >= 0 ? raw : rubberband(raw, drag.el.offsetWidth);
    drag.el.style.transform = `translateX(${drag.x}px)`;
    drag.samples = [...drag.samples.slice(-4), { x: drag.x, t: event.timeStamp }];
  }

  protected release(event: PointerEvent): void {
    const drag = this.drag;
    if (!drag) return;
    this.drag = null;
    drag.el.releasePointerCapture(event.pointerId);

    const first = drag.samples[0];
    const last = drag.samples[drag.samples.length - 1];
    const dt = Math.max(last.t - first.t, 1);
    const velocity = ((last.x - first.x) / dt) * 1000;
    const projected = drag.x + project(velocity);

    drag.el.style.transition = '';
    if (projected > drag.el.offsetWidth / 2) {
      drag.el.style.transform = `translateX(${drag.el.offsetWidth + 48}px)`;
      drag.el.style.opacity = '0';
      setTimeout(() => this.notifications.dismiss(drag.id), 200);
    } else {
      drag.el.style.transform = '';
      this.notifications.resume(drag.id);
    }
  }
}

function rubberband(overshoot: number, dimension: number, constant = 0.55): number {
  return (overshoot * dimension * constant) / (dimension + constant * Math.abs(overshoot));
}
