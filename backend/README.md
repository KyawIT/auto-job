# AutoJob backend

ASP.NET Core (.NET 10) + EF Core + SQLite. Runs as a local sidecar process spawned by the Electron app.

## Projects

| Project | Purpose | References |
|---|---|---|
| `Core` | Entities and contracts (interfaces). No dependencies. | – |
| `Persistence` | `ApplicationDBContext`, repositories, `UnitOfWork`, migrations. | Core |
| `api` | HTTP host, controllers, sidecar plumbing (`api/Sidecar`). | Core, Persistence |

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
