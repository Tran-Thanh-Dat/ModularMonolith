# Security Guide

Production hardening policies for the Modular Monolith Web API (.NET 8, PostgreSQL).

## Secret management

- **Never commit real secrets** in `appsettings*.json`, scripts, or documentation.
- Production values must come from **environment variables** or your secret store (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, etc.).
- Gitignored local files: `.env`, `appsettings.*.local.json`.
- Template only: `.env.example` (placeholders).
- See [SECRETS.md](./SECRETS.md) for local setup and rotation after accidental exposure.

### Production startup validation

When `ASPNETCORE_ENVIRONMENT=Production`, the API **fails fast** if:

| Check | Requirement |
|-------|-------------|
| `ConnectionStrings:DefaultConnection` | Required; no placeholders; no weak passwords (`postgres`, `password`, `admin`, etc.) |
| `Jwt:Secret` | ≥ 32 characters; no placeholders |
| `RefreshToken:Secret` | ≥ 32 characters; no placeholders |
| `AdminSeed:Password` | No placeholders/defaults in Production; strong password required when seed enabled |
| `Cache:Provider=Redis` | `Cache:Redis:ConnectionString` required and non-placeholder |
| `Cors:AllowedOrigins` | Must not contain `*` |
| `Swagger:Enabled` | Must be `false` |
| `BackgroundJobs:Dashboard:Enabled` | Must be `false` **or** protected with non-default Basic Auth |
| `AllowedHosts` | Must not be `*` |
| `FileStorage:MaxFileSizeMb` | 1–200 |
| `FileStorage:Local:RootPath` | Required |

Implementation: `src/ApiHost/Startup/ProductionStartupValidator.cs`.

## JWT requirements

### Configuration keys

| Key | Purpose |
|-----|---------|
| `Jwt:Secret` | HMAC signing key for access tokens |
| `Jwt:Issuer` | Validated issuer |
| `Jwt:Audience` | Validated audience |
| `Jwt:AccessTokenExpirationMinutes` | Access token lifetime (default 30) |
| `RefreshToken:Secret` | HMAC key for refresh token hashing |
| `RefreshToken:ExpirationDays` | Refresh token lifetime (default 7) |

### Production requirements

- Signing secrets: **minimum 32 characters**; use cryptographically random strings (64+ recommended).
- No placeholder values (`CHANGE_ME`, `dev_jwt_secret`, etc.).
- `RequireHttpsMetadata = true` in Production (JWT bearer middleware).
- Token validation: issuer, audience, lifetime, signing key.
- Clock skew: **1 minute** (explicit).

### Token lifetime recommendations

| Token | Recommended | Notes |
|-------|-------------|-------|
| Access | 15–60 minutes | Shorter for high-risk APIs |
| Refresh | 7–30 days | Balance UX vs. exposure window |

### Refresh token policy

- Refresh tokens are **hashed** before storage (HMAC with `RefreshToken:Secret`).
- **Logout/revoke**: refresh token record marked revoked; subsequent refresh rejected.
- **Inactive/deactivated users**: cannot log in or refresh tokens (`FindActive*` lookups).
- **Reuse detection**: presenting a **revoked** refresh token revokes **all active refresh tokens** for that user, writes a security activity log (`RefreshTokenReuseDetected`), and returns a generic `Auth.RefreshTokenInvalid` response to the client (no reuse details exposed).
- **Device/session grouping** is not implemented; reuse handling applies at user level.

### AdminSeed policy

- Base `appsettings.json` contains **placeholders only** (`CHANGE_ME`) — never usable defaults.
- `AdminSeed:Enabled` defaults to `true` for local bootstrap; set `false` in Production.
- Production auto-migration is disabled by default; admin seed should not run in Production unless explicitly configured with a **strong password from secret store**.
- Use user-secrets, `.env`, or `appsettings.Development.local.json` for local admin passwords.

### Password handling

- Password hashes are never logged or returned in API responses.
- Failed login messages are generic (no user enumeration where possible).

## CORS policy

Configuration section: `Cors`.

```json
"Cors": {
  "AllowedOrigins": [],
  "AllowedMethods": ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
  "AllowedHeaders": ["Authorization", "Content-Type", "X-Correlation-Id"],
  "AllowCredentials": false
}
```

- **Production**: never use `AllowAnyOrigin` or `*` in `AllowedOrigins`. Startup fails if `*` is configured.
- Empty `AllowedOrigins` disables CORS middleware (appropriate for server-to-server or same-origin behind a reverse proxy).
- **Development/Docker**: localhost origins may be configured in environment-specific settings.
- `AllowCredentials` should remain `false` unless a browser client on a specific origin requires cookies.

## Rate limiting

Configuration section: `RateLimiting`. Enabled by default; disabled in IntegrationTesting.

| Policy | Default | Path pattern |
|--------|---------|--------------|
| General API | 120 req / 60s per IP | All routes |
| Login | 5 req / 60s per IP | `/auth/login` |
| Refresh | 10 req / 60s per IP | `/auth/refresh-token` |
| File upload | 20 req / 60s per IP | `/files/upload` |
| Background jobs | 10 req / 60s per IP | `/background-jobs` |
| Health probes | Exempt | `/health/live`, `/health/ready` |

Returns **HTTP 429** with JSON body and optional `Retry-After` header.

## Security headers

Middleware: `SecurityHeadersMiddleware`. Configuration: `SecurityHeaders`.

| Header | Value |
|--------|-------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |
| `X-XSS-Protection` | `0` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` |
| `Content-Security-Policy` | Restrictive default; relaxed policy for `/swagger` in Dev/Docker |
| `Strict-Transport-Security` | Enabled in Production when HTTPS is used |

## Upload security

Configuration: `FileStorage`.

- Max request body and multipart limits aligned with `FileStorage:MaxFileSizeMb`.
- Allowed extensions and content types configured explicitly.
- Magic-byte validation on upload (see Files module).
- Path traversal blocked; filenames sanitized.
- Binary content is not logged.
- Temporary file cleanup job runs on schedule (`TemporaryFileCleanupCron`).
- Upload endpoints require permission and rate limiting.
- Upload root **writability** is validated at runtime by the file storage health check (probe write/delete).

**TODO (future):** Antivirus/malware scanning hook before marking files as permanent.

## Known limitations

- Rate limiting is **IP-based** only (no per-user partition behind shared NAT).
- Refresh token reuse handling revokes tokens at **user** scope (no device/session family model yet).
- Antivirus scanning hook is not implemented.
- Outdated package scan is documented but not a CI gate (`dotnet list package --outdated`).

## Logging and PII

- Authorization headers, cookies, passwords, tokens, and secrets are **not logged**.
- `Cookie`, `Set-Cookie`, and `Authorization` headers block request/response body logging.
- `SensitiveDataMasker` masks JSON fields including `cookie` and `set-cookie`.
- Request/response body logging disabled by default in Production (`Monitoring:IncludeRequestBody/ResponseBody = false`).
- SMTP, Redis, and database connection errors are **sanitized** before returning to API clients.
- Detailed provider errors are stored in activity/audit logs and server logs only.
- File logs rotate by date; logs directory configurable via Serilog settings.
- Correlation ID and UserId included in structured logs where available.
- `SensitiveDataMasker` used in logging pipeline where applicable.

## Swagger production policy

- **Development/Docker**: Swagger enabled by default (`Swagger:Enabled` or environment fallback).
- **Production**: Swagger **disabled by default**. Startup fails if `Swagger:Enabled=true`.
- To expose Swagger in a controlled environment, protect it via reverse proxy (IP allowlist, OAuth, basic auth) — do not enable publicly without protection.
- JWT Bearer scheme remains configured for Dev/Docker UI.

## Hangfire dashboard policy

- Route: `/hangfire` (configurable via `BackgroundJobs:Dashboard:Path`).
- **Production default**: dashboard disabled (`BackgroundJobs:Dashboard:Enabled=false`).
- If enabled in Production, **Basic Auth** with non-placeholder credentials is required or startup fails.
- Manual job triggers require `[Authorize]` and `BackgroundJob.Dashboard` permission.
- Never expose an unprotected dashboard to the public internet.

## Authorization checklist

Sensitive endpoints require authentication and permissions:

| Area | Protection |
|------|------------|
| Monitoring details / system info | `Monitoring.HealthView` |
| Audit logs / activity logs | Module permissions |
| File download | Owner or `Files.ViewAll` |
| Notifications (cross-user) | `ViewAll` / `Manage` |
| Background job manual triggers | `BackgroundJob.Dashboard` |
| Hangfire dashboard | Basic Auth + permission filter |

## Dependency vulnerability scan

Run locally or in CI (non-blocking initially):

```bash
dotnet list ModularMonolith.sln package --vulnerable --include-transitive
dotnet list ModularMonolith.sln package --outdated
```

Update vulnerable packages when safe. As of Phase 20, transitive `Newtonsoft.Json` 11.x from Hangfire is overridden to **13.0.3** in `ApiHost`, `BackgroundJobs.Infrastructure`, and `Monitoring.Infrastructure`.

Document any remaining advisories that require major-version upgrades of upstream packages.

## Incident checklist: leaked secrets

1. **Revoke immediately** — rotate DB password, JWT secret, refresh secret, SMTP credentials, Redis auth, Hangfire dashboard password.
2. **Invalidate sessions** — force re-login; purge refresh tokens if JWT/refresh secrets were exposed.
3. **Audit access logs** for suspicious activity during exposure window.
4. **Remove secret from codebase** and git history if the repo is shared/public.
5. **Enable secret scanning** on the repository (GitHub Advanced Security, GitLab scanning, etc.).
6. **Notify stakeholders** per your incident response policy.
7. **Document timeline** and remediation in your internal incident tracker.

## Related documentation

- [PRODUCTION.md](./PRODUCTION.md) — deployment and environment variables
- [SECRETS.md](./SECRETS.md) — local secret hygiene
- [DOCKER.md](./DOCKER.md) — container runtime notes
- [CI.md](./CI.md) — pipeline and optional security gates
