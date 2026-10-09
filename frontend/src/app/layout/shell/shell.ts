import { afterNextRender, Component, DestroyRef, inject, Injector } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { silent } from '../../core/error.interceptor';

interface NavItem {
  label: string;
  link: string;
  icon: string;
  /** Ctrl/⌘ + key */
  key: number;
}

// Outline glyphs (24×24 paths), drawn in currentColor.
const icons = {
  jobs: 'M4 8h16v11H4zM9 8V5h6v3',
  search: 'M10.5 4a6.5 6.5 0 1 1 0 13 6.5 6.5 0 0 1 0-13zM15.5 15.5 20 20',
  applications: 'M5 4h10l4 4v12H5zM15 4v4h4M8 12h8M8 16h6',
  documents: 'M7 3h7l4 4v14H7zM14 3v4h4M4 7v14h10',
  profile: 'M12 4a4 4 0 1 1 0 8 4 4 0 0 1 0-8zM5 20c1-4 4-6 7-6s6 2 7 6',
  settings:
    'M12 9a3 3 0 1 1 0 6 3 3 0 0 1 0-6zM12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1',
};

/** App frame: translucent sidebar, routed page, backend status, keyboard shortcuts. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  host: {
    '(document:keydown)': 'shortcut($event)',
  },
})
export class Shell {
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);

  protected readonly health = httpResource<{ status: string }>(() => ({
    url: '/health',
    context: silent(),
  }));

  protected readonly sections: { heading: string; items: NavItem[] }[] = [
    {
      heading: 'Find work',
      items: [
        { label: 'Jobs', link: '/jobs', icon: icons.jobs, key: 1 },
        { label: 'Search', link: '/search', icon: icons.search, key: 2 },
        { label: 'Applications', link: '/applications', icon: icons.applications, key: 3 },
      ],
    },
    {
      heading: 'About you',
      items: [
        { label: 'Profile', link: '/profile', icon: icons.profile, key: 4 },
        { label: 'Documents', link: '/documents', icon: icons.documents, key: 5 },
        { label: 'Settings', link: '/settings', icon: icons.settings, key: 6 },
      ],
    },
  ];
  protected readonly items = this.sections.flatMap((s) => s.items);
  private readonly mac = /Mac/.test(navigator.platform);
  protected readonly modifier = this.mac ? '⌘' : 'Ctrl+';
  protected readonly keyPrefix = this.mac ? 'Meta+' : 'Control+';

  constructor() {
    // A dead backend is retried quietly, so the status heals by itself once it is up.
    const timer = setInterval(() => {
      if (this.health.error()) this.health.reload();
    }, 5000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));

    // Wayfinding for keyboard and screen reader users: after navigating, focus the new page title.
    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() =>
        afterNextRender(
          () => document.querySelector<HTMLElement>('main h1')?.focus({ preventScroll: true }),
          {
            injector: this.injector,
          },
        ),
      );
  }

  /** Ctrl/⌘ + 1…6 jumps between sections; "/" focuses the page's search box. */
  protected shortcut(event: KeyboardEvent): void {
    const target = event.target;
    const typing =
      target instanceof Element &&
      target.closest('input, textarea, select, [contenteditable="true"]');
    if ((event.ctrlKey || event.metaKey) && !event.altKey && /^[1-6]$/.test(event.key)) {
      event.preventDefault();
      void this.router.navigateByUrl(this.items[Number(event.key) - 1].link);
    } else if (event.key === '/' && !typing) {
      const search = document.querySelector<HTMLInputElement>('main [data-search]');
      if (search) {
        event.preventDefault();
        search.focus();
      }
    }
  }
}
