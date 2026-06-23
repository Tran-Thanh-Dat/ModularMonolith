# Docker Local Development

This guide explains how to run the Modular Monolith API locally with Docker Compose.

## Prerequisites

- Docker Desktop (or Docker Engine + Compose v2)
- .NET 8 SDK (for local builds/tests outside Docker)
- Git

Optional:

- PowerShell (Windows) or Bash (Linux/macOS) for helper scripts in `scripts/`

See also [SECRETS.md](./SECRETS.md) for local credential setup.

## Quick start

1. Copy environment file:

```bash
cp .env.example .env
```

On Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

2. Review `.env` and replace placeholder secrets if needed (safe for local dev as-is).

3. Start the stack:

```bash
./scripts/dev-up.sh
```

Windows:

```powershell
.\scripts\dev-up.ps1
```

Or directly:

```bash
docker compose up -d --build
```

4. Wait for services to become healthy:

```bash
docker compose ps
```

Or run the smoke test (config + up + health checks):

```bash
./scripts/docker-smoke.sh
```

Windows:

```powershell
.\scripts\docker-smoke.ps1
```

## Services

| Service | Purpose | Host URL |
|---------|---------|----------|
| `api` | ASP.NET Core API | http://localhost:5080 |
| `postgres` | PostgreSQL 16 | localhost:5432 |
| `redis` | Redis 7 | localhost:6379 |
| `mailpit` | Dev SMTP + inbox UI | SMTP :1025, UI http://localhost:8025 |
| `pgadmin` | DB admin UI (optional) | http://localhost:5050 |

### Optional pgAdmin

```bash
docker compose --profile tools up -d pgadmin
```

Login with `PGADMIN_EMAIL` / `PGADMIN_PASSWORD` from `.env`.

Add server:

- Host: `postgres`
- Port: `5432`
- Database: value of `POSTGRES_DB`
- Username/password: from `.env`

## API endpoints

| Endpoint | Description |
|----------|-------------|
| http://localhost:5080/swagger | Swagger UI (Docker environment) |
| http://localhost:5080/health/live | Liveness probe |
| http://localhost:5080/health/ready | Readiness probe (may be 503 if a dependency is down) |
| http://localhost:5080/hangfire | Hangfire dashboard (Basic Auth in Docker) |

Default seeded admin (from `.env` / `AdminSeed`):

- Username: `admin`
- Password: `Admin@123456` (change in `.env`)

## Database migrations

### Configuration

```json
"Database": {
  "ApplyMigrationsOnStartup": true,
  "FailStartupOnMigrationError": true
}
```

| Environment | `ApplyMigrationsOnStartup` | `FailStartupOnMigrationError` |
|-------------|---------------------------|------------------------------|
| Production (default) | `false` | `true` |
| Development / Docker | `true` | `true` |
| IntegrationTesting | `false` | `true` |

When `FailStartupOnMigrationError` is `true`, a failed migration **stops startup** (container exits). This is the default in Docker so schema problems are visible immediately.

`/health/live` only confirms the process is running — it does not mean migrations succeeded. With fail-fast enabled, a migration failure prevents the API from staying up.

### Automatic (default in Docker)

When `Database:ApplyMigrationsOnStartup=true` (set in `appsettings.Docker.json` and compose env), the API applies EF Core migrations on startup and seeds identity data.

Migrations are **not** auto-applied in `Production`.

### Manual migration script

```bash
./scripts/apply-migrations.sh
```

Windows:

```powershell
.\scripts\apply-migrations.ps1
```

Requires PostgreSQL reachable at `localhost:5432` (or values from `.env`).

## Email testing (Mailpit)

The Docker stack routes SMTP to Mailpit:

- Host inside compose network: `mailpit:1025`
- Web UI: http://localhost:8025

`Email:TestMode:Enabled=true` redirects outbound mail to `dev@example.com` while still delivering to Mailpit.

Mailpit is **development only**. Do not use in production.

## Volumes and cleanup

Named volumes:

- `postgres_data` — database files
- `redis_data` — Redis AOF data
- `api_uploads` — uploaded files (`FileStorage:Local:RootPath` → `/app/uploads`)
- `api_logs` — Serilog file logs (`logs/api-*.log` → `/app/logs`)

### Volume permissions (non-root API)

The API process runs as the non-root `app` user. On startup, `docker/docker-entrypoint.sh`:

1. Ensures `/app/uploads` and `/app/logs` exist
2. `chown`s them to the `app` user (entrypoint starts as root briefly)
3. Drops privileges with `gosu` before launching `dotnet ApiHost.dll`

Runtime files are written to **mounted volumes**, not the container image layer.

Inspect logs:

```bash
docker compose logs api
docker compose logs api --tail 100 -f
```

Stop containers:

```bash
./scripts/dev-down.sh
# or: docker compose down
```

Remove containers **and volumes** (destroys DB/uploads/logs):

```bash
docker compose down -v
```

## Environment variables

Configuration priority:

1. Environment variables (set in `docker-compose.yml` from `.env`)
2. `appsettings.Docker.json`
3. `appsettings.json`

Key variables (see `.env.example`):

| Variable | Purpose |
|----------|---------|
| `POSTGRES_*` | PostgreSQL credentials |
| `JWT_SECRET` | JWT signing key |
| `REFRESH_TOKEN_SECRET` | Refresh token HMAC key |
| `API_PORT` | Host port mapped to API container |
| `HANGFIRE_DASHBOARD_*` | Hangfire Basic Auth (local Docker) |

Never commit real secrets. Use `.env` locally; use a secret manager in production.

## Health checks

Compose healthchecks:

- **postgres**: `pg_isready`
- **redis**: `redis-cli ping`
- **api**: `GET /health/live`

`/health/ready` includes dependency checks (PostgreSQL, Redis, etc.). It may return **503 Service Unavailable** when a dependency is misconfigured, but still returns JSON entries for troubleshooting.

## Troubleshooting

### Permission denied on uploads or logs

Ensure the API container uses the entrypoint script (see `Dockerfile`). After `docker compose up`, uploads and Serilog file logging should write to the named volumes.

If issues persist:

```bash
docker compose logs api
docker compose exec api ls -la /app/uploads /app/logs
```

Recreate volumes if corrupted:

```bash
docker compose down -v
docker compose up -d --build
```

### API container keeps restarting

```bash
docker compose logs api
```

Common causes:

- PostgreSQL not ready (wait for `postgres` healthy)
- Invalid connection string in `.env`
- **Migration failure** — with `Database:FailStartupOnMigrationError=true`, the container exits instead of running with an incomplete schema

Fix migrations manually:

```bash
./scripts/apply-migrations.sh
```

### DB migration failure

Check API logs for `Identity migration/seed failed` or similar. Verify PostgreSQL credentials in `.env` match `POSTGRES_*` values.

### Redis unavailable

Ensure `redis` service is healthy and `Cache__Provider=Redis` is set for the API. `/health/ready` may return 503 until Redis responds.

### Mailpit not receiving email

- Confirm `Email__Smtp__Host=mailpit` and port `1025` in compose
- Check Mailpit UI at http://localhost:8025
- `Email:TestMode:Enabled=true` redirects recipient but still delivers to Mailpit

### Hangfire dashboard auth

Docker enables Basic Auth (`HANGFIRE_DASHBOARD_USER` / `HANGFIRE_DASHBOARD_PASSWORD` from `.env`). JWT + `BackgroundJob.Dashboard` permission also works for API clients.

### Swagger not loading

Swagger is enabled for `Development` and `Docker` environments only.

### Cannot connect to PostgreSQL from host tools

Use:

- Host: `localhost`
- Port: `POSTGRES_PORT` (default 5432)
- Credentials from `.env`

### Reset everything

```bash
docker compose down -v
docker compose up -d --build
```

## Security notes (local dev)

- Default passwords in `.env.example` are placeholders for local use only.
- PostgreSQL and Redis ports are exposed to the host for developer convenience.
- In production: use strong secrets, restrict network access, disable Mailpit, protect Hangfire dashboard, and do not expose database ports publicly.
- Hangfire dashboard uses Basic Auth in Docker; monitoring details endpoint still requires JWT.

## Skipped integration tests

Four tests are skipped by default (no PostgreSQL/JWT in CI). See [TESTING.md](./TESTING.md).

Future: enable with Testcontainers or `docker compose` profile — see [CI.md](./CI.md).

## Related docs

- [CI.md](./CI.md) — GitHub Actions pipeline
- [TESTING.md](./TESTING.md) — unit/integration test guide
- [SECRETS.md](./SECRETS.md) — secret hygiene and rotation
