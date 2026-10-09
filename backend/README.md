# AutoJob backend

ASP.NET Core (.NET 10) + EF Core + SQLite. Runs as a local sidecar process spawned by the Electron app.

## Architecture

Clean architecture, dependencies point inward:

```
api (HTTP, controllers, error mapping, background workers)
 ├─> Services (use cases, DTOs, ports)  ──> Core (entities, enums, repository contracts, exceptions)
 ├─> Persistence (EF Core + SQLite, UnitOfWork, repositories)  ──> Core
 └─> Infrastructure (adapters for the ports)  ──> Services, Core
```

| Project | Purpose |
|---|---|
| `Core` | Entities, enums, repository contracts, persistence exceptions. No dependencies. |
| `Services` | Use cases (`ProfileService`, `DocumentService`, `JobService`, `JobSearchService`, `ApplicationService`, `GenerationService`, `ProfileImportService`, `BatchService`, `BatchProcessor`, `ExportService`) and **ports** in `Services/Ports` for everything external. |
| `Persistence` | `ApplicationDBContext`, repositories, `UnitOfWork`, migrations. |
| `Infrastructure` | Port adapters: NVIDIA NIM (`NimClient`), Adzuna, Bundesagentur für Arbeit, file storage, PDF/DOCX/TXT text extraction (PdfPig, OpenXml), cover letter PDF/DOCX (QuestPDF, OpenXml), `.eml` drafts (MimeKit), in-memory API key store. |
| `api` | Controllers, `ApiExceptionHandler`, `GenerationWorker` (batches), `SavedSearchRefresher`, sidecar plumbing. |

### Ports (Services/Ports) and adapters (Infrastructure)

| Port | Adapter | Notes |
|---|---|---|
| `IAiClient` | `NimClient` | `integrate.api.nvidia.com/v1/chat/completions`, user's own key |
| `IJobSearchClient` | `AdzunaClient`, `BundesagenturClient` | Adzuna needs app ID/key; Bundesagentur uses the public key |
| `ISecretStore` | `InMemorySecretStore` | Keys only in memory; Electron pushes them from `safeStorage` |
| `IFileStorage` | `LocalFileStorage` | Everything under the data folder |
| `ITextExtractor` | `DocumentTextExtractor` | PDF/DOCX/TXT; scanned PDFs → `NotSupported` (OCR not yet) |
| `ICoverLetterRenderer` | `CoverLetterRenderer` | DIN 5008 / international layout |
| `IEmailDraftBuilder` | `EmlDraftBuilder` | `.eml` with `X-Unsent: 1` |

## HTTP API

All routes need the `X-AutoJob-Token` header (except `/health`). Errors are RFC 7807 problem details:
400 validation (`errors` per field), 404 not found, 409 conflict, 400 `type: ai-not-configured`, 429 `type: ai-rate-limited`, 502 `type: ai-failed`.

| Area | Endpoints |
|---|---|
| Profile | `GET/PUT /api/profile`, `POST /api/profile/import-cv`, `POST/PUT/DELETE /api/profile/{work-experiences,educations,skills,languages}[/{id}]` |
| Settings | `GET/PUT /api/settings`, `PUT /api/settings/secrets` (keys in, never out) |
| Documents | `GET/POST(multipart) /api/documents`, `PUT/DELETE /api/documents/{id}`, `GET /api/documents/{id}/file` |
| Jobs | `GET /api/jobs?state=&search=&includeDismissed=`, `POST /api/jobs`, `GET/DELETE /api/jobs/{id}`, `PUT /api/jobs/{id}/state`, `POST /api/jobs/search` |
| Saved searches | `GET/POST /api/saved-searches`, `PUT/DELETE /api/saved-searches/{id}` (refreshed in the background) |
| Applications | `GET/POST /api/applications`, `GET/PUT/DELETE /api/applications/{id}`, `PUT .../status`, `PUT .../attachments`, `POST .../notes`, `POST .../generate`, `GET .../texts?kind=`, `GET .../export?format=Pdf\|Docx`, `POST .../email-draft` |
| Texts | `PUT /api/texts/{id}` (edit), `POST /api/texts/{id}/select` (make current) |
| Batches | `POST /api/batches`, `GET /api/batches/{id}`, `POST /api/batches/{id}/cancel`, `POST /api/batches/{id}/retry` |

## Sidecar contract (for Electron)

Configuration is passed as environment variables when spawning the process:

| Variable | Required | Meaning |
|---|---|---|
| `Sidecar__Token` | yes (outside Development) | Random secret per launch. Every request must send it as `X-AutoJob-Token`. |
| `Sidecar__Port` | no | Port to listen on. `0` (default) = OS picks a free port. |
| `Sidecar__DataDirectory` | no | Where `autojob.db` and `logs/` live. Default: `%APPDATA%/AutoJob` (pass Electron's `app.getPath('userData')`). |
| `Sidecar__ParentPid` | recommended | Electron's PID. Backend exits when that process dies. |
| `Sidecar__AllowedOrigins__0` | no | CORS origin of the renderer. Default `app://autojob`. |

Lifecycle:

1. Spawn the backend executable with the variables above.
2. Read stdout until a line `AUTOJOB_READY http://127.0.0.1:<port>` appears. That is the base URL.
3. Send `X-AutoJob-Token` on every request. `GET /health` works without it.
4. On quit, `POST /shutdown` (with token) for a graceful stop; kill the process only as a fallback.

The backend listens on `127.0.0.1` only, rejects other `Host` headers, and applies pending EF migrations on startup.

## Development

```bash
dotnet run --project api
```

Development mode: fixed port `5246`, no token required, database and logs in `api/.data/`, CORS allows `http://localhost:4200` (Angular dev server).

### Tests

```bash
dotnet test
```

| Project | Seam | How |
|---|---|---|
| `Persistence.Tests` | `IUnitOfWork` | Real SQLite (in memory / temp file) built from the migrations |
| `Services.Tests` | Use-case services, `BatchProcessor` | Real SQLite + fakes only for ports (AI, job sources, files, clock) |
| `Infrastructure.Tests` | Adapters | Fake HTTP handler with recorded API shapes; real PDF/DOCX/EML files read back |
| `Api.Tests` | HTTP | `WebApplicationFactory`, temp data folder, AI/job sources replaced |

### Migrations

```bash
dotnet ef migrations add <Name> --project Persistence --startup-project api --output-dir Migrations
```

No need to run `database update` – the app migrates itself on start.

## Publishing

Self-contained single file per platform (users don't need .NET installed):

```bash
dotnet publish api -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
```

Other runtime identifiers: `osx-arm64`, `osx-x64`, `linux-x64`. Ship the output via electron-builder `extraResources`.
