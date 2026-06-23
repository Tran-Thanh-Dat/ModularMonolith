# Troubleshooting

Common development and deployment issues.

## Database connection failure

**Symptoms:** Startup exception, 500 on API calls, `/health/ready` unhealthy for `db`.

**Checks:**

1. PostgreSQL running and reachable
2. `ConnectionStrings:DefaultConnection` correct (user-secrets or env var)
3. Database exists; migrations applied (`Database:ApplyMigrationsOnStartup` or manual `dotnet ef database update`)
4. Production: `ProductionStartupValidator` rejects placeholder connection strings

## Migration failure

**Symptoms:** App fails on startup when `FailStartupOnMigrationError=true`.

**Checks:**

1. DB user has CREATE/ALTER rights
2. No conflicting manual schema changes
3. Run migration per module DbContext with correct `--context`
4. Check `__ef_migrations_history` in each schema

## Redis unavailable

**Symptoms:** `/health/ready` 503, cache errors in logs.

**Checks:**

1. `Cache:Provider` — Memory does not need Redis
2. Production multi-instance: Redis required; verify `Cache:Redis:ConnectionString`
3. `FailFastOnRedisUnavailableInProduction` may block startup

## Docker volume permission denied

**Symptoms:** Upload or log write fails in container.

**Checks:**

1. Volume mount permissions for `uploads/` and log paths
2. Run container user vs host UID (Linux)
3. See [../DOCKER.md](../DOCKER.md)

## Upload fails

| Error | Likely cause |
|-------|--------------|
| `File.TooLarge` | Exceeds `MaxFileSizeMb` |
| `File.ExtensionNotAllowed` | Extension not in whitelist |
| `File.ContentTypeNotAllowed` | Declared content-type not allowed, or magic bytes do not match extension |
| `File.ContentTypeNotAllowed` | MIME type mismatch |

See [FILES.md](./FILES.md).

## Email not received

1. Check Mailpit UI (Docker) at http://localhost:8025
2. Verify `Email:TestMode` — mail may redirect elsewhere
3. Check `EmailMessage` status in DB or via API
4. `/health/ready` SMTP check
5. Email retry job: `background-jobs:email-retry`

**Never use production SMTP in tests.**

## Hangfire dashboard inaccessible

1. `BackgroundJobs:Enabled=true`
2. `BackgroundJobs:Dashboard:Enabled=true`
3. User has `BackgroundJob.Dashboard` permission
4. Correct path (default `/hangfire`)
5. Production: dashboard disabled by default — intentional

## Swagger not available in Production

Expected behavior. Swagger is enabled for Development/Docker only (`ShouldExposeSwagger` in `Program.cs`).

Enable locally: `ASPNETCORE_ENVIRONMENT=Development` or `Swagger:Enabled=true` (non-Production).

## 401 Unauthorized

1. Missing or expired access token
2. Wrong `Authorization: Bearer` header format
3. JWT secret mismatch between token issue and validation
4. User deactivated after token issued — refresh may also fail

See [AUTHENTICATION.md](./AUTHENTICATION.md).

## 403 Forbidden

1. User lacks required permission for `[HasPermission]`
2. Cross-user resource access (notifications, etc.)
3. Permission changed — **re-login or refresh** to update JWT claims

See [AUTHORIZATION.md](./AUTHORIZATION.md).

## Health ready returns 503

Inspect JSON body for failing dependency entries. Common: PostgreSQL down, Redis misconfigured, SMTP unreachable, Hangfire storage issue.

Public `/health/ready` is rate-limit exempt — use for probes.

## Cache stale behavior

1. Permissions stale after role change → user must refresh token
2. Category list stale → verify post-commit cache invalidation
3. Multi-instance with Memory cache → switch to Redis

See [CACHING.md](./CACHING.md).

## Rate limit 429

Auth endpoints have stricter limits. Wait and retry. Health endpoints exempt from global rate limit where configured.

See [../SECURITY.md](../SECURITY.md).

## Still stuck?

1. Enable Serilog debug for your subsystem
2. Note `traceId` from error response
3. Check [../PRODUCTION.md](../PRODUCTION.md) for environment-specific settings
