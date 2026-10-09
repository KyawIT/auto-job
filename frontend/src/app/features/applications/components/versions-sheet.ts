import { Component, inject, input, model, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { label } from '../../../shared/labels';
import { Sheet } from '../../../shared/sheet/sheet';
import { ApplicationsApi } from '../applications.api';
import { ApplicationDetails, GeneratedTextKind } from '../applications.models';

/** All versions of one text, newest first. "Use this version" makes it current again. */
@Component({
  selector: 'app-versions-sheet',
  imports: [DatePipe, Sheet],
  template: `
    <app-sheet
      [heading]="'Versions · ' + label(kind() ?? '')"
      [open]="!!kind()"
      (openChange)="!$event && kind.set(null)"
    >
      @if (versions.hasValue()) {
        <ol class="versions">
          @for (v of versions.value(); track v.id) {
            <li class="version" [class.current]="v.isCurrent">
              <div class="meta">
                <strong>{{ v.createdAt | date: 'medium' }}</strong>
                <span class="caption">
                  {{ v.isEdited ? 'edited by you' : 'AI' }}
                  @if (v.model) {
                    · {{ v.model }}
                  }
                </span>
                @if (v.isCurrent) {
                  <span class="badge badge--accent">Current</span>
                } @else {
                  <button
                    type="button"
                    class="btn btn--small"
                    [disabled]="busy()"
                    (click)="restore(v.id)"
                  >
                    Use this version
                  </button>
                }
              </div>
              <p class="preview">{{ v.content }}</p>
            </li>
          }
        </ol>
      } @else {
        <span class="skeleton" role="status" aria-label="Loading versions"></span>
      }
      <div class="form-actions">
        <button type="button" class="btn" (click)="kind.set(null)">Close</button>
      </div>
    </app-sheet>
  `,
  styles: `
    .versions {
      display: grid;
      gap: 0.75rem;
      margin: 0;
      padding: 0;
      list-style: none;
    }
    .version {
      display: grid;
      gap: 0.5rem;
      padding: 0.875rem 1rem;
      border: 1px solid var(--border);
      border-radius: var(--radius);
    }
    .version.current {
      border-color: var(--accent);
    }
    .meta {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.5rem 0.75rem;
    }
    .meta .caption {
      flex: 1;
    }
    .preview {
      display: -webkit-box;
      -webkit-line-clamp: 4;
      -webkit-box-orient: vertical;
      overflow: hidden;
      white-space: pre-line;
      font-size: 0.875rem;
      color: var(--text-muted);
    }
  `,
})
export class VersionsSheet {
  private readonly api = inject(ApplicationsApi);

  readonly applicationId = input.required<number>();
  /** Open while set. */
  readonly kind = model<GeneratedTextKind | null>(null);
  readonly restored = output<ApplicationDetails>();

  protected readonly versions = this.api.versionsResource(this.applicationId, this.kind);
  protected readonly busy = signal(false);
  protected readonly label = label;

  protected async restore(textId: number): Promise<void> {
    this.busy.set(true);
    try {
      this.restored.emit(await this.api.selectVersion(textId));
      this.kind.set(null);
    } catch {
      // toast
    } finally {
      this.busy.set(false);
    }
  }
}
