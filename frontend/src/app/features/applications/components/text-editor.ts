import {
  Component,
  DestroyRef,
  inject,
  input,
  linkedSignal,
  output,
  signal,
  untracked,
} from '@angular/core';
import { ApplicationsApi } from '../applications.api';
import { GeneratedText } from '../applications.models';

type SaveState = 'idle' | 'pending' | 'saving' | 'saved' | 'error';

const autosaveDelayMs = 800;

/**
 * Edits one generated text in place and saves it by itself: shortly after typing stops,
 * on blur, and when leaving the page. Typing is never blocked by a save in flight.
 */
@Component({
  selector: 'app-text-editor',
  template: `
    <div class="head">
      <label class="field-label" [for]="inputId()">{{ heading() }}</label>
      <span class="state caption" aria-live="polite">{{ stateText() }}</span>
      <ng-content />
    </div>
    @if (multiline()) {
      <textarea
        class="textarea"
        [id]="inputId()"
        [rows]="rows()"
        [value]="content()"
        [disabled]="disabled()"
        (input)="edit($any($event.target).value)"
        (blur)="flush()"
      ></textarea>
    } @else {
      <input
        class="input"
        [id]="inputId()"
        [value]="content()"
        [disabled]="disabled()"
        (input)="edit($any($event.target).value)"
        (blur)="flush()"
      />
    }
  `,
  styles: `
    :host {
      display: grid;
      gap: 0.375rem;
    }
    .head {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .state {
      flex: 1;
    }
    textarea {
      font-size: 1rem;
      line-height: 1.65;
    }
  `,
})
export class TextEditor {
  private readonly api = inject(ApplicationsApi);

  readonly text = input.required<GeneratedText>();
  readonly heading = input.required<string>();
  readonly inputId = input.required<string>();
  readonly multiline = input(true);
  readonly rows = input(16);
  readonly disabled = input(false);
  readonly saved = output<GeneratedText>();

  /** Local copy; only a different version (new id) replaces what the user is typing. */
  protected readonly content = linkedSignal({
    source: () => this.text().id,
    computation: () => untracked(this.text).content,
  });
  protected readonly state = signal<SaveState>('idle');
  private timer: ReturnType<typeof setTimeout> | undefined;
  private savedContent: string | null = null;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.flush());
  }

  protected stateText(): string {
    switch (this.state()) {
      case 'pending':
      case 'saving':
        return 'Saving…';
      case 'saved':
        return 'Saved';
      case 'error':
        return 'Not saved — will retry when you type';
      default:
        return this.text().isEdited ? 'Edited by you' : '';
    }
  }

  protected edit(value: string): void {
    this.content.set(value);
    this.state.set('pending');
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.flush(), autosaveDelayMs);
  }

  protected flush(): void {
    clearTimeout(this.timer);
    if (this.state() !== 'pending') return;
    const content = this.content();
    if (!content.trim() || content === (this.savedContent ?? this.text().content)) {
      this.state.set('idle');
      return;
    }
    this.state.set('saving');
    this.api.editText(this.text().id, content).then(
      (text) => {
        this.savedContent = text.content;
        // A newer keystroke may have arrived meanwhile; keep it pending.
        if (this.state() === 'saving') this.state.set('saved');
        this.saved.emit(text);
      },
      () => this.state.set('error'),
    );
  }
}
