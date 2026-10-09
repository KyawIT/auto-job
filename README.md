# AutoJob

Open-source, local-only desktop app. No server, no cloud: everything runs and is stored on your machine.

| Folder | What | Docs |
|---|---|---|
| `backend/` | ASP.NET Core (.NET 10) + EF Core + SQLite, runs as a sidecar process | [backend/README.md](backend/README.md) |
| `frontend/` | Angular 22 UI, rendered inside the Electron window | [frontend/PLAN.md](frontend/PLAN.md) |
| `electron/` | Desktop shell: starts the backend, opens the window, packages the app | – |

## Requirements

- Node.js 24.15+ (or 22.22.3+)
- .NET 10 SDK

## First-time setup

```bash
npm --prefix frontend install
npm --prefix electron install
```

## Develop

```bash
npm --prefix electron run dev
```

Starts the Angular dev server (hot reload), then Electron, which runs the backend from source.
The sidebar shows whether the UI can reach the backend. A fresh install opens the setup wizard.

UI only, in a normal browser:

```bash
dotnet run --project backend/api
npm --prefix frontend start
```

Then open http://localhost:4200.

## Calling the backend from Angular

Use relative URLs; the interceptor in `frontend/src/app/core` adds the backend address and token:

```ts
http.get<Job[]>('/api/jobs')
```

Backend controllers use `[Route("api/[controller]")]`.

## Build an installer

```bash
npm --prefix electron run dist:win
```

Output: `electron/dist/`. Also `dist:mac` / `dist:linux` (must be run on that OS).
