# Production Deployment Guide

Operational checklist for running the Modular Monolith Web API in Production.

## Required environment variables

Set these via your host, container orchestrator, or secret store. Do **not** bake values into images.

| Variable / config key | Required | Description |
|----------------------|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Yes | Must be `Production` |
| `ConnectionStrings__DefaultConnection` | Yes | PostgreSQL connection string |
| `Jwt__Secret` | Yes | ≥ 32 chars, cryptographically random |
| `RefreshToken__Secret` | Yes | ≥ 32 chars, cryptographically random |
| `AllowedHosts` | Yes | Explicit host names (e.g. `api.example.com`) |
| `Cache__Provider` | Recommended | `Redis` for multi-instance |
| `Cache__Redis__ConnectionString` | If Redis | Redis endpoint |
| `FileStorage__Local__RootPath` | Yes | Writable upload directory (use volume) |
| `Email__Smtp__*` | If sending mail | SMTP host, port, credentials |
| `AdminSeed__Enabled` | No | Keep `false` in Production |
| `AdminSeed__Password` | If seed enabled | Strong bootstrap password from secret store only |

Optional overrides:

| Key | Default (Production) | Notes |
|-----|---------------------|-------|
| `Swagger__Enabled` | `false` | Keep disabled unless reverse-proxy protected |
| `BackgroundJobs__Dashboard__Enabled` | `false` | Enable only with Basic Auth |
| `Database__ApplyMigrationsOnStartup` | `false` | Never auto-migrate in Production |
| `RateLimiting__Enabled` | `true` | Tune limits per traffic profile |
| `SecurityHeaders__EnableStrictTransportSecurity` | `true` | Requires HTTPS termination |

| `Cors__AllowedOrigins__0` | If browser clients | Explicit origins (no `*`) |

### Config hygiene (Phase 20.1)

- Base `appsettings.json` uses `CHANGE_ME` placeholders only — never deploy with base defaults unchanged.
- Production startup rejects weak DB passwords (`postgres`, `password`, `admin`, etc.) and placeholder AdminSeed passwords.
- When `Cache:Provider=Redis`, a production Redis connection string is required at startup.

## Migration strategy

| Environment | Auto-migrate | Fail on error |
|-------------|--------------|---------------|
| Development | Configurable (`Database:ApplyMigrationsOnStartup`) | Yes (`FailStartupOnMigrationError`) |
| Docker | Yes (Dev stack) | Yes |
| Production | **No** | N/A — run migrations manually |

### Manual migration

```powershell
.\scripts\apply-migrations.ps1 -ConnectionString "Host=...;Database=...;..."
```

Or:

```bash
dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --startup-project src/ApiHost
# Repeat for each module DbContext, or use the apply-migrations script.
```

Run migrations **before** deploying a new API version. The API fails startup on migration errors only when auto-migrate is enabled (Dev/Docker).

**Admin seed in Production:** disabled by default (`AdminSeed:Enabled=false`, `Database:ApplyMigrationsOnStartup=false`). If you explicitly enable seeding, provide a strong `AdminSeed__Password` from your secret store — never use committed defaults.

## Health checks

| Endpoint | Access | Purpose |
|----------|--------|---------|
| `/health/live` | Public | Process alive; not rate-limited aggressively |
| `/health/ready` | Public or internal | DB/cache readiness |
| `/api/v1/monitoring/health/details` | Authenticated + permission | Detailed dependency status (no secrets) |
| `/api/v1/monitoring/system-info` | Authenticated + permission | Runtime info |

Use `/health/live` for load balancer probes. Restrict `/health/ready` to internal networks if it exposes dependency names.

## Docker production notes

- Image builds in **Release** configuration.
- Container runs as **non-root** after entrypoint (`gosu` drops privileges).
- Mount volumes for:
  - **Uploads**: `FileStorage:Local:RootPath` (e.g. `/app/uploads`)
  - **Logs**: Serilog file sink directory
- No secrets in the image; inject via environment or secrets mount.
- `.dockerignore` excludes local secrets and build artifacts.
- Healthcheck defined in `Dockerfile` / compose.
- Dev-only services (Mailpit, pgAdmin) use compose profiles — not for Production.

See [DOCKER.md](./DOCKER.md) for local development stack details.

## Reverse proxy and HTTPS

- Terminate TLS at nginx, IIS, Azure App Gateway, AWS ALB, Cloudflare, etc.
- Set `X-Forwarded-Proto` and `X-Forwarded-For` when behind a proxy.
- HSTS is enabled in Production when requests are HTTPS.
- `AllowedHosts` must match public host names.
- Consider WAF rules for auth endpoints and file uploads.

## Log and upload volumes

```yaml
volumes:
  - api-uploads:/app/uploads
  - api-logs:/app/logs
```

Ensure the entrypoint or init container sets ownership for the app user. Log rotation is handled by Serilog date-based rolling.

Upload directory **writability** is checked by the file storage health check at runtime (probe file write/delete on `/health/ready`).

## Redis for multi-instance

When running **more than one API instance**:

- Set `Cache:Provider=Redis`.
- Configure `Cache:Redis:ConnectionString`.
- `FailFastOnRedisUnavailableInProduction=true` prevents silent in-memory fallback.
- Required for consistent permission caching and distributed rate limiting considerations.

Single-instance deployments may use in-memory cache but Redis is recommended for Production.

## SMTP configuration

```json
"Email": {
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "UserName": "...",
    "Password": "...",
    "EnableSsl": true
  },
  "TestMode": { "Enabled": false }
}
```

Provider errors are logged internally; clients receive a generic `"Email delivery failed."` message.

## Backup considerations

- **PostgreSQL**: regular automated backups (pg_dump, WAL archiving, managed service snapshots).
- **Upload volume**: backup `/app/uploads` or object storage if migrated later.
- **Redis**: ephemeral cache — no backup required unless used for durable data.
- **Secrets**: store in secret manager with rotation policy.
- Test restore procedures periodically.

## Deployment smoke checklist

After each Production deploy:

- [ ] API starts without validation errors
- [ ] `/health/live` returns 200
- [ ] `/health/ready` returns 200 (DB connected)
- [ ] Login with test account succeeds
- [ ] Authenticated API call succeeds
- [ ] Swagger is **not** publicly accessible
- [ ] Hangfire dashboard is **not** publicly accessible
- [ ] Security headers present on responses
- [ ] CORS rejects unauthorized origins (if configured)
- [ ] File upload respects size limits
- [ ] Logs write to mounted volume; no secrets in log output
- [ ] Email send returns safe error on failure (no SMTP internals)

## Related documentation

- [SECURITY.md](./SECURITY.md) — security policies and incident response
- [SECRETS.md](./SECRETS.md) — secret hygiene
- [DOCKER.md](./DOCKER.md) — Docker development stack
- [CI.md](./CI.md) — build and test pipeline
