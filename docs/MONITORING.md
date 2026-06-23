# Monitoring

Health probes, dependency checks, and operational visibility.

## Public health endpoints

| Endpoint | Purpose |
|----------|---------|
| `/health/live` | Process is running (liveness) |
| `/health/ready` | Dependencies available (readiness) |
| `/health` | Combined health check |

These endpoints are **anonymous** and return sanitized JSON (no secrets).

`/health/ready` is **exempt from rate limiting** for orchestrator probes.

### Ready returns 503

When PostgreSQL, Redis (if required), SMTP, file storage, or Hangfire checks fail, readiness returns **503** with dependency entries in the payload. Liveness may still return 200.

## Protected monitoring APIs

Base route: `/api/v1/monitoring`

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/health/details` | `Monitoring.HealthView` |
| GET | `/system-info` | `Monitoring.SystemInfoView` |

These return richer detail than public probes (versions, check durations, configuration hints — still sanitized).

## Dependency checks

Registered when health checks are enabled:

| Check | Tag |
|-------|-----|
| PostgreSQL | `db`, `ready` |
| Redis | `cache`, `ready` (when Redis provider configured) |
| Hangfire | `hangfire`, `ready` (when BackgroundJobs enabled) |
| SMTP | `email`, `ready` |
| File storage | `files`, `ready` |

Configure tags in `HealthChecks` section — see `appsettings.json`.

## Correlation ID

Every request receives/propagates `X-Correlation-Id`. Included in:

- Serilog log context
- `ApiResponse.traceId` on errors
- Exception middleware responses

## Request logging

Config: `LoggingOptions`

- Slow request threshold logging
- Sensitive fields/headers masked (password, tokens, cookies)
- Health and Swagger paths excluded by default

## Production deployment

- Use `/health/live` + `/health/ready` for load balancer/orchestrator probes
- Do not expose detailed monitoring APIs without auth
- Public health responses must not leak connection strings or internal hostnames
- See [../PRODUCTION.md](../PRODUCTION.md) and [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md)

## Example

```bash
curl http://localhost:5080/health/ready
curl -H "Authorization: Bearer TOKEN" http://localhost:5080/api/v1/monitoring/health/details
```

See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) for 503 debugging.
