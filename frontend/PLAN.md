# Frontend plan

Angular 22 UI for AutoJob, rendered inside Electron. The backend (all features, tested) is done; this plan
covers the UI and the small Electron additions it needs.

**Status:** M0–M7 implemented and covered by page tests (`npm test`, 51 tests); M8 polish done except the
installer test. Verified against the real backend in a browser: wizard, Bundesagentur search, job list/detail,
application start, missing-key toast, batch page; AXE (WCAG 2.2 AA + best practices) clean on every page in
light and dark mode. Not yet verified by hand: the Electron-only paths (key survives restart, save dialog,
opening the `.eml` draft in Outlook/Thunderbird) and a real AI generation (needs an NVIDIA key).

## Principles

- **Angular 22 defaults**: standalone components, OnPush (default), signals for state, `@Service()` for singletons,
  `inject()`, native control flow (`@if`, `@for`), Signal Forms for all forms (see `AGENTS.md`).
- **Server state stays on the server**: read with `httpResource`, write with `HttpClient`, then `reload()` the resource.
  No NgRx; a feature's signals live in its API service or page component.
- **Relative URLs only** (`/api/...`). `core/backend.interceptor.ts` already adds base URL and token.
- **Accessibility is a requirement**: WCAG AA, AXE-clean, full keyboard use, visible focus, labelled fields.
- **Test at seams, test-first**: page components with `HttpTestingController` (what the user sees and which request is sent).
  No tests of private methods or internal signals.

## Folder structure

```
src/app/
├── core/                    app-wide singletons
│   ├── backend.ts               (exists) URL + token from Electron
│   ├── backend.interceptor.ts   (exists)
│   ├── api-error.ts             ProblemDetails → typed ApiError (validation, not-found, conflict, ai-*)
│   ├── error.interceptor.ts     unexpected errors → toast
│   ├── notifications.ts         toast service (signal list)
│   ├── utc-dates.interceptor.ts backend timestamps arrive without "Z" (SQLite) → marked as UTC
│   ├── desktop.ts               typed wrapper around window.autojob (Electron bridge), no-op fallback in browser
│   └── setup.guard.ts           no profile yet → /setup
├── layout/
│   ├── shell/                   sidebar navigation + <router-outlet>, backend status, Ctrl/⌘+1…6 shortcuts
│   └── toast-outlet.ts          toasts (swipe to dismiss), rendered by App next to the confirm dialog
├── shared/                  dumb, reusable UI
│   ├── page-header/  empty-state/  status-badge/  confirm-dialog/  sheet/ (native <dialog>)
│   ├── field-errors/            shows a Signal Forms field's server + client errors
│   └── file-drop/               drag & drop + file picker
└── features/                one folder per area, each lazy-loaded
    ├── setup/  profile/  documents/  settings/
    ├── jobs/  search/  applications/  batches/
    └── <feature>/
        ├── <feature>.models.ts      TS types = backend DTOs (camelCase, enums as string unions)
        ├── <feature>.api.ts         @Service: resources + commands for this area
        ├── <feature>.routes.ts      lazy routes
        └── pages/ + components/
```

## Routes

| Route | Page | Backend |
|---|---|---|
| `/setup` | Wizard: profile → API key → optional CV import | `PUT /api/profile`, `PUT /api/settings/secrets`, `POST /api/documents`, `POST /api/profile/import-cv` |
| `/jobs` | Job list: filters (new, bookmarked, dismissed), text search, multi-select → batch | `GET /api/jobs`, `PUT /api/jobs/{id}/state` |
| `/jobs/new` | Manual job entry | `POST /api/jobs` |
| `/jobs/:id` | Job detail: description, bookmark/dismiss, "Start application" | `GET /api/jobs/{id}`, `POST /api/applications` |
| `/search` | Search form (keywords, location, radius, sources), results, per-source errors | `POST /api/jobs/search` |
| `/search/saved` | Saved searches: list, create, edit, pause, delete | `/api/saved-searches` |
| `/applications` | List grouped/filtered by status | `GET /api/applications?status=` |
| `/applications/:id` | Detail: letter editor, email texts, versions, attachments, recipient, status, timeline, notes, export, email | `/api/applications/{id}/…`, `/api/texts/{id}` |
| `/batches/:id` | Batch progress (polling), per-job status, cancel, retry failed | `/api/batches/{id}` |
| `/profile` | Personal data + experience, education, skills, languages | `/api/profile/…` |
| `/documents` | Upload, list with extraction status, rename/type/attach-by-default, delete, import CV | `/api/documents`, `/api/profile/import-cv` |
| `/settings` | API keys (status only), model, letter language, layout, parallelism, search defaults | `/api/settings` |

`setupGuard` on everything except `/setup`: if `GET /api/profile` returns 404, go to `/setup`.

## Error handling

| Backend answer | UI |
|---|---|
| 400 with `errors` | Shown under the matching form fields (Signal Forms server errors) |
| 400 `type: ai-not-configured` | Toast "NVIDIA key missing" with link to `/settings` |
| 429 `type: ai-rate-limited` | Toast "Rate limit, try again in a moment" |
| 502 `type: ai-failed` | Toast with the message + retry button |
| 404 | "Not found" state on the page |
| 409 | Toast with the message (e.g. "document is still attached") |
| Network / 500 | Toast "Backend error", details in console |

## Electron additions

Exposed through `preload.js` → `core/desktop.ts`:

| Bridge call | Main process does |
|---|---|
| `setApiKeys({ nimApiKey?, adzunaAppId?, adzunaAppKey? })` | Encrypt with `safeStorage`, save in `userData`, forward to `PUT /api/settings/secrets` |
| (on startup, no call) | Decrypt saved keys and push them to the backend once it is ready |
| `openFile(fullPath)` | `shell.openPath` — only paths inside the data folder (email drafts) |
| `saveFile(suggestedName, bytes)` | Save dialog, write file (PDF/DOCX export) |

In a plain browser (`ng serve` only) `desktop.ts` falls back: keys go straight to the backend, exports download via `<a download>`.

## Milestones

Each milestone ends runnable via `npm --prefix electron run dev` and with green tests.

### M0 — Foundation ✅
- Pick and install UI kit (see open decisions), base theme (light/dark), typography.
- Shell layout with sidebar, routes for all areas (placeholder pages), lazy loading.
- `api-error.ts`, error interceptor, toast service and outlet.
- `desktop.ts` bridge wrapper with browser fallback.
- **Done when**: navigation works by mouse and keyboard; a forced 500 shows a toast.

### M1 — Setup & profile ✅
- `setupGuard`; wizard step 1 (personal data).
- Profile page: personal data form; lists with add/edit/delete dialogs for experience, education, skills, languages.
- **Done when**: fresh install lands in the wizard; after saving, the profile page shows everything; server validation errors appear on the fields.

### M2 — Settings & API keys ✅
- Electron: `safeStorage` key store, IPC `setApiKeys`, push keys on startup.
- Settings page: key inputs (write-only, show "configured" status), model, language, layout, parallelism.
- Wizard step 2 (NIM key, skippable).
- **Done when**: key survives an app restart; settings show "configured" without revealing the key.

### M3 — Documents & CV import ✅
- Upload (drag & drop + picker, type select), list with extraction status, edit, delete (409 explained), download.
- "Fill profile from CV" action; wizard step 3.
- **Done when**: uploading a CV and importing it fills the profile.

### M4 — Jobs & search ✅
- Job list with filters, search box, state actions; job detail; manual entry.
- Search page with source selection and per-source error display (e.g. Adzuna key missing).
- Saved searches page.
- **Done when**: a Bundesagentur search shows real jobs; bookmarked/dismissed filters work.

### M5 — Applications & AI letter ✅
- Start application from job detail.
- Application detail: generate button (with progress state), cover letter editor (autosave via `PUT /api/texts/{id}`), email subject/body, versions panel (restore), attachments picker with order, recipient/language form, status changer, timeline, notes.
- Applications list by status.
- **Done when**: job → application → generated letter → edited → older version restored.

### M6 — Export & email ✅ (Electron path not yet tried by hand)
- Electron: `saveFile`, `openFile`.
- Export PDF/DOCX (save dialog); "Prepare email" → opens the `.eml` draft in the mail client.
- **Done when**: Outlook/Thunderbird opens a draft with letter PDF and attachments.

### M7 — Batch generation ✅
- Multi-select in job list → language choice + "this makes N AI requests" confirm → start.
- Batch page: progress bar, per-job status/error, cancel, retry failed; links to each application.
- **Done when**: 5 jobs generate in the background while the user keeps working.

### M8 — Polish & release (installer test open)
- Empty states, loading skeletons, keyboard shortcuts, AXE audit of every page, dark mode check.
- Privacy note where data leaves the machine (generate, CV import).
- Installer test (`dist:win`).

## Decisions

1. **UI kit**: own components, no Material and no CDK. Dialogs are native `<dialog>` (focus trap, Escape and
   focus return come from the browser); the design system lives in `src/styles.scss` (Apple-style: system font
   with size-specific tracking, translucent sidebar/toasts, spring easing via CSS `linear()`, press feedback,
   reduced motion/transparency/contrast variants).
2. **UI language**: English only for now. Labels for backend enum values are in `shared/labels.ts`.
3. **Applications list**: list with a status filter (segmented control). Status changes happen on the detail page.

## Testing notes

- `src/testing/test-app.ts`: `provideTestApp(routes)` (HttpClient + error interceptor + router with input binding),
  `settle()`/`nextTask()` to let resources send their requests, small DOM helpers.
- `src/testing/fixtures.ts`: one builder per DTO.
- `src/test-setup.ts`: jsdom has no `<dialog>` behaviour; a minimal `showModal`/`close` is added.
