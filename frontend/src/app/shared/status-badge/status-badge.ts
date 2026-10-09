import { Component, computed, input } from '@angular/core';
import { label } from '../labels';

type Tone = 'neutral' | 'accent' | 'success' | 'warning' | 'danger';

// Covers application, job, extraction, generation task and batch statuses.
const tones: Record<string, Tone> = {
  Draft: 'neutral',
  Ready: 'accent',
  Sent: 'accent',
  Interview: 'warning',
  Offer: 'success',
  Rejected: 'danger',
  Withdrawn: 'neutral',
  New: 'accent',
  Seen: 'neutral',
  Bookmarked: 'warning',
  Dismissed: 'neutral',
  Pending: 'neutral',
  Queued: 'neutral',
  Running: 'accent',
  Done: 'success',
  Completed: 'success',
  Failed: 'danger',
  NotSupported: 'warning',
  Cancelled: 'neutral',
  Configured: 'success',
};

/** Status pill. Color is never the only signal: the label is always shown. */
@Component({
  selector: 'app-status-badge',
  template: `{{ text() }}`,
  host: {
    class: 'badge',
    '[class.badge--accent]': 'tone() === "accent"',
    '[class.badge--success]': 'tone() === "success"',
    '[class.badge--warning]': 'tone() === "warning"',
    '[class.badge--danger]': 'tone() === "danger"',
  },
})
export class StatusBadge {
  readonly status = input.required<string>();
  protected readonly text = computed(() => label(this.status()));
  protected readonly tone = computed(() => tones[this.status()] ?? 'neutral');
}
