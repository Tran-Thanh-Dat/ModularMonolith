# Monitoring Module

Health checks, request observability, and protected diagnostics APIs for operations teams.

## Structure

```
Monitoring/
├── Monitoring.Application/         # CQRS queries, DTOs, permissions
├── Monitoring.Infrastructure/        # Health checks, providers, DI registration
├── Monitoring.Infrastructure.Tests/  # Health check unit tests
└── Monitoring.Api/                   # Secured REST endpoints
```

Shared building blocks live in `BuildingBlocks.Infrastructure/Health/` (endpoint mapping, tag matching, sanitization, uptime).

## Endpoints

| Endpoint | Auth | Purpose |
|----------|------|---------|
| `GET /health/live` | Public | Liveness — `self` check only |
| `GET /health/ready` | Public | Readiness — dependency checks |
| `GET /health` | Public | All registered health checks (aggregate) |
| `GET /api/v1/monitoring/health/details` | JWT + `Monitoring.HealthView` (always) | Detailed component status + dependency summary |
| `GET /api/v1/monitoring/system-info` | JWT + `Monitoring.SystemInfoView` | Safe runtime/system information |

### Live vs Ready

- **Live** (`/health/live`): Checks tagged with `live` only — typically the `self` process check.
- **Ready** (`/health/ready`): Checks tagged with **`ready` OR `dependency`**. With default config this includes:
  - `self` (also tagged `dependency`)
  - `postgresql`
  - `cache-provider` or `redis-cache`
  - `hangfire`
  - `smtp-config`
  - `file-storage`

The ready predicate uses the **union** of `HealthChecks:Tags:Ready` and `HealthChecks:Tags:Dependency`, so dependency-tagged checks are always included even when `Ready` and `Dependency` are separate arrays.

Startup logs a warning if health checks are enabled but no dependency checks were registered, or if tag configuration would prevent readiness from matching dependency checks.

### Public vs Protected Responses

- **Public** `/health/*`: Sanitized JSON — no hostnames, ports, paths, connection strings, credentials, or exception details in descriptions.
- **Protected** `/api/v1/monitoring/health/details`: Richer admin-safe descriptions and dependency summary — still no secrets. Always requires JWT + `Monitoring.HealthView` (not configurable as public).

Protected monitoring APIs use `ApiResponse<T>` via `BaseApiController`.

## Configuration

```json
"HealthChecks": {
  "Enabled": true,
  "DetailsEnabled": true,
  "TimeoutSeconds": 5,
  "Tags": {
    "Live": ["live"],
    "Ready": ["ready"],
    "Dependency": ["dependency"]
  }
},
"Monitoring": {
  "EnableRequestLogging": true,
  "EnableCorrelationId": true,
  "SlowRequestThresholdMs": 1000,
  "IncludeRequestBody": false,
  "IncludeResponseBody": false
}
```

Set `HealthChecks:Enabled` to `false` to disable all mapped health endpoints.

Set `HealthChecks:DetailsEnabled` to `false` to block the protected details API (returns `Monitoring.InvalidConfiguration`).

Per-check timeouts use `HealthChecks:TimeoutSeconds` (default 5 seconds).

## Permissions

| Code | Assigned to | Purpose |
|------|-------------|---------|
| `Monitoring.HealthView` | Admin, SuperAdmin | Detailed health status |
| `Monitoring.SystemInfoView` | Admin, SuperAdmin | System/runtime info |

The details endpoint is **always protected** in production. There is no public detailed health endpoint.

Permissions are seeded via `IdentitySeeder` from `PermissionCodes.All`.

## Dependency Checks

### PostgreSQL

- Single check on `DefaultConnection` (Identity, Categories, Files, Notifications, BackgroundJobs, AuditLogs, and Hangfire schemas share this database).
- Included in readiness.
- Connection string is never returned in responses.

### Redis / Cache

| `Cache:Provider` | Behavior |
|------------------|----------|
| `Redis` (connection string set) | TCP connectivity check via AspNetCore.HealthChecks.Redis |
| `Redis` (connection string missing) | **Unhealthy** in production; **Degraded** in development when `FallbackToMemoryInDevelopment=true` |
| `Memory` | **Degraded** — OK for dev/single instance; not distributed |
| `None` | **Healthy** — caching disabled |

Redis connection strings are never exposed in health responses.

### Hangfire

| `BackgroundJobs:Enabled` | Behavior |
|--------------------------|----------|
| `true` | Verifies Hangfire PostgreSQL storage is reachable |
| `false` | **Healthy** — “Background jobs disabled” |

Dashboard credentials are never exposed in health responses.

### SMTP (configuration only)

- **Does not send email.**
- Validates provider, `FromAddress`, and SMTP host/port when `Provider=Smtp`.
- Missing optional email config reports **Degraded** (email is non-critical for API availability).
- Passwords and host/port details are not exposed on public health endpoints.

### File Storage (Local)

- Ensures root directory exists or can be created.
- Writes and deletes a small temporary probe file.
- Public responses do not include absolute paths.
- Non-local providers (e.g. future S3/MinIO) report configured provider name only.

## Correlation ID

- Accepts incoming header: `X-Correlation-Id` (case-insensitive).
- Generates a GUID when missing.
- Returned on every response as `X-Correlation-Id`.
- Added to Serilog log scope as `CorrelationId`.
- Used as `ApiResponse.TraceId` for **both success and error responses** (via `BaseApiController` and `GlobalExceptionHandlingMiddleware`).

Disable via `Monitoring:EnableCorrelationId: false`.

## Request Logging

When `Monitoring:EnableRequestLogging` is `true`:

- Logs one completion line per request: method, path, status code, elapsed ms, correlation id, user id.
- Requests exceeding `SlowRequestThresholdMs` log at **Warning**.
- Request/response bodies are **not** logged by default.
- Authorization headers, cookies, query strings, and sensitive fields are never logged.
- Health and monitoring paths are excluded via `LoggingOptions:ExcludedPaths`.

## Security Notes

- Never expose connection strings, SMTP/Redis passwords, JWT secrets, or Hangfire basic auth in health or monitoring responses.
- Public `/health/*` descriptions are sanitized (generic “Check completed.” when operational details would leak).
- Detailed diagnostics always require authentication and explicit permissions.
- Admin-only responses apply a lighter sanitizer — still no secrets.

## Production Deployment

1. Configure probes:
   - Liveness: `/health/live`
   - Readiness: `/health/ready`
2. Use `Cache:Provider: Redis` with a valid connection string for multi-instance deployments.
3. Ensure PostgreSQL is reachable before marking the instance ready.
4. Keep `IncludeRequestBody` and `IncludeResponseBody` **false** in production.
5. Restrict monitoring API access to Admin/SuperAdmin roles only.

## Smoke Test Checklist

- [ ] `GET /health/live` → `Healthy` with only `self` entry
- [ ] `GET /health/ready` → includes dependency entries (`postgresql`, `cache-provider`/`redis-cache`, `hangfire`, `smtp-config`, `file-storage`, and `self`)
- [ ] Stop PostgreSQL → `/health/ready` returns `Unhealthy` or `Degraded` (not `Healthy` with empty entries)
- [ ] Response includes `X-Correlation-Id` header
- [ ] Send `X-Correlation-Id: my-test-id` → success and error `ApiResponse.traceId` match the header value
- [ ] Logs show correlation id and elapsed time on API requests
- [ ] Slow requests (> threshold) log as Warning
- [ ] Public `/health/ready` descriptions do not contain hostnames, ports, or paths
- [ ] `GET /api/v1/monitoring/health/details` without token → 401
- [ ] With token but without permission → 403
- [ ] With `Monitoring.HealthView` → 200, no secrets in payload
- [ ] `GET /api/v1/monitoring/system-info` with `Monitoring.SystemInfoView` → safe fields only
- [ ] `Cache:Provider=Redis` with empty connection string → readiness reports Unhealthy/Degraded (not silently omitted)
- [ ] Solution builds; unit tests pass

## Error Codes

| Code | When |
|------|------|
| `Monitoring.HealthCheckFailed` | Health operation failed |
| `Monitoring.DependencyUnavailable` | Required dependency down |
| `Monitoring.Forbidden` | Missing monitoring permission |
| `Monitoring.InvalidConfiguration` | Health details disabled or invalid monitoring configuration |
