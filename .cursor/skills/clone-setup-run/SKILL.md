---
name: clone-setup-run
description: Guides first-time setup and run of the Modular Monolith Web API after cloning—PostgreSQL connection, migrations, seed admin login, Visual Studio F5, and common startup failures. Use when the user clones the repo, asks how to start/run the project, configure database, run migrations, or hits Identity migration/seed or Hangfire JobStorage errors on startup.
---

# Clone, Setup & Run (Modular Monolith)

## Source of truth

Read before acting (do not duplicate long content):

| Doc | Purpose |
|-----|---------|
| [README.md](../../README.md#setup-sau-khi-clone-source-net-sdk) | Full setup steps (Vietnamese) |
| [README.md](../../README.md#chạy-migration) | Migration one-liner |
| [SECRETS.md](../../SECRETS.md) | Secret hygiene |
| [docs/LOCAL_DEVELOPMENT.md](../../docs/LOCAL_DEVELOPMENT.md) | SDK details |
| [docs/TROUBLESHOOTING.md](../../docs/TROUBLESHOOTING.md) | Common errors |

## Workflow checklist

```
- [ ] dotnet restore && dotnet build ModularMonolith.sln
- [ ] PostgreSQL reachable; database exists
- [ ] ConnectionStrings:DefaultConnection configured (local file or user-secrets)
- [ ] Migration (auto on startup OR scripts/apply-migrations.ps1)
- [ ] dotnet run / F5 ApiHost
- [ ] POST /api/v1/auth/login with seeded admin
```

## Step 1 — Build

```powershell
dotnet restore ModularMonolith.sln
dotnet build ModularMonolith.sln
```

Startup project: `src/ApiHost/ApiHost.csproj`. URL: **http://localhost:5080/swagger** (`launchSettings.json`).

## Step 2 — Database config (critical)

App uses **`ConnectionStrings:DefaultConnection`** (Npgsql format). It does **not** read `DB_HOST`, `DB_PORT`, `DB_NAME` env vars unless mapped manually.

**Preferred:** create gitignored `src/ApiHost/appsettings.Development.local.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=HOST;Port=PORT;Database=DB;Username=USER;Password=PASSWORD"
  }
}
```

Loaded via `Program.cs` after `appsettings.Development.json` (local wins).

**Never** commit real passwords to `appsettings*.json`. See [SECRETS.md](../../SECRETS.md).

## Step 3 — Migration

**One command (PowerShell):**

```powershell
.\scripts\apply-migrations.ps1
```

Requires `dotnet-ef` global tool if missing: `dotnet tool install --global dotnet-ef`.

**Auto (Development default):** `Database:ApplyMigrationsOnStartup = true` in `appsettings.Development.json` — migrations + Identity seed run at startup.

DbContexts migrated: Identity, AuditLogs, Categories, Files, Notifications, BackgroundJobs.

## Step 4 — Run

- **Visual Studio:** `ModularMonolith.sln` → startup **ApiHost** → F5
- **CLI:** `dotnet run --project src/ApiHost/ApiHost.csproj`

Verify: `GET http://localhost:5080/health/live` → 200.

## Step 5 — Login (seed)

After first successful Identity seed (`AdminSeed:Enabled = true`):

| Field | Development default |
|-------|---------------------|
| Username | `admin` |
| Password | `Admin@123456` |
| Endpoint | `POST /api/v1/auth/login` |

Seed is idempotent — skips if `admin` already exists. Seeds permissions, Admin/SuperAdmin roles, one Admin user.

## Docker alternative

Full stack without local PG install:

```powershell
Copy-Item .env.example .env
.\scripts\docker-smoke.ps1
```

See [DOCKER.md](../../DOCKER.md).

## Common startup failures

| Symptom | Cause | Fix |
|---------|-------|-----|
| `Failed to connect to 127.0.0.1:5432` | App still on default localhost; pgAdmin may use remote host/port | Set `appsettings.Development.local.json` with actual host/port |
| `Identity migration/seed failed` | PG down, wrong connection string, or DB missing | Fix connection; create database; retry |
| `JobStorage instance has not been initialized` | Static `RecurringJob` before Hangfire DI | Fixed in codebase — use `IRecurringJobManager` from DI in `RegisterBackgroundJobsRecurringJobs` |
| Login fails after seed | Wrong password or user inactive | Dev password `Admin@123456`; inactive login returns `Auth.InvalidCredentials` |

## Agent rules when helping setup

1. **Diagnose connection first** — compare app connection string vs user's pgAdmin host/port/database.
2. **Do not put real secrets in committed files** — only `*.local.json` (gitignored) or user-secrets.
3. **Do not map `DB_HOST`/`DB_PORT` in code** unless user explicitly requests env-var support — map to `ConnectionStrings:DefaultConnection` in local config instead.
4. After config changes, suggest `dotnet build` then run; offer `dotnet test ModularMonolith.sln` if appropriate.
5. Link to [docs/KIEN_TRUC.md](../../docs/KIEN_TRUC.md) or [TECHNICAL_ARCHITECTURE.md](../../docs/TECHNICAL_ARCHITECTURE.md) for architecture questions.

## Additional resources

- Hangfire / background jobs: [docs/BACKGROUND_JOBS.md](../../docs/BACKGROUND_JOBS.md)
- Auth flow: [docs/AUTHENTICATION.md](../../docs/AUTHENTICATION.md)
