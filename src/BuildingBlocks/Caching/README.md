# Caching Foundation

Reusable cache infrastructure for the modular monolith. All modules consume `ICacheService` and `ICacheOperationBuffer` from BuildingBlocks.

## Providers

| Provider | Description |
|----------|-------------|
| `None` | No-op provider (`NoCacheService`). Reads always miss; writes are ignored. Useful for tests or disabling cache. |
| `Memory` | In-process `IMemoryCache` with thread-safe key registry for prefix removal. Safe for **single-instance** development only. |
| `Redis` | Distributed cache via `IDistributedCache` + StackExchange.Redis. **Required for multi-instance production.** |

## Multi-instance deployment

- **Memory cache is process-local.** Each application instance maintains its own cache. Invalidation on one instance does not affect others.
- **Memory cache is only safe for single-instance deployment** (local dev, single-node staging).
- **Multi-instance production must use Redis** for consistent cache reads and invalidation across nodes.
- Prefix invalidation with Memory does **not** cross instance boundaries.
- **Redis fail-fast is recommended in production** (`FailFastOnRedisUnavailableInProduction=true`) so misconfiguration is detected at startup instead of silently running with inconsistent caches.

## Configuration

```json
"Cache": {
  "Provider": "Memory",
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "ModularMonolith:"
  },
  "DefaultExpirationMinutes": 30,
  "UserPermissionsExpirationMinutes": 20,
  "KeyPrefix": "modular-monolith",
  "EnableLogging": true,
  "FailFastOnRedisUnavailableInProduction": true,
  "FallbackToMemoryInDevelopment": true
}
```

Supported `Provider` values: `None`, `Memory`, `Redis`. Any other value throws at startup with `Cache.ProviderNotConfigured`.

### Redis startup behavior

- **Production + `FailFastOnRedisUnavailableInProduction=true`**: startup throws if Redis cannot be configured.
- **Production + fail-fast disabled**: falls back to Memory and logs a **Critical** warning that in-memory cache is unsafe for multi-instance production.
- **Development**: fallback to Memory is allowed with a clear **Warning** log.
- Redis connection strings are never written to logs or API responses.

## Key convention

Keys are built via `CacheKeys` with a version segment and configured prefix:

```
{KeyPrefix}:{InstanceName}v1:{module}:{segment}:...
```

Examples:

- `CacheKeys.UserPermissions(userId)` → `v1:user:{guid}:permissions`
- `CacheKeys.CategoryDetail(id)` → `v1:categories:detail:{guid}`
- `CacheKeys.CategoryList(queryHash)` → `v1:categories:list:{hash}`
- `CacheKeys.EmailTemplateByCode(code)` → `v1:email-templates:code:{normalized-code}`
- `CacheKeys.FileResourceDetail(id)` → `v1:files:detail:{guid}`
- `CacheKeys.NotificationUnreadCount(userId)` → `v1:notifications:unread-count:{guid}`

List/query keys hash parameters via `CacheKeys.HashQueryParameters(...)`. User input is normalized (trim, lowercase for codes) — never used raw in keys.

## Cache operations (post-commit)

Commands enqueue cache work through `ICacheOperationBuffer`:

- `EnqueueSet(key, value, expiration)` — write after successful commit
- `EnqueueRemove(key)`
- `EnqueueRemoveByPrefix(prefix)`

`CacheInvalidationPostCommitHook` flushes the buffer **after** `SaveChanges` succeeds (same pattern as activity logs). On rollback, failed `Result`, or exception, the buffer is cleared without touching cache.

If post-commit cache operations fail, a warning is logged. Business operations are not failed.

## Permission cache (auxiliary)

- Permission cache is **auxiliary** — it does not drive authorization.
- **JWT authorization uses JWT permission claims** embedded in the access token, not cache lookups.
- Permission cache does **not** replace authorization checks.
- After role or permission changes, users should **re-login or refresh their token** if JWT claims must reflect the change immediately.
- Permission cache is populated **after successful login/refresh commit** via `EnqueueSet`.
- Permission cache is invalidated on role/permission assignment and user deactivation where implemented.
- Access tokens and refresh tokens are never cached.

## What is cached (Phase 16)

| Feature | Key | Invalidation |
|---------|-----|--------------|
| Category by id | `CategoryDetail` | create/update/delete/activate/deactivate |
| Category list | `CategoryList` | any category mutation (prefix) |
| Email template render | `EmailTemplateByCode` | template CRUD/activate/deactivate |
| File metadata by id | `FileResourceDetail` | delete, mark permanent |
| Notification unread count | `NotificationUnreadCount` | create, mark read, mark all read, archive (if unread) |
| User permissions (snapshot) | `UserPermissions` | role/permission assignment, deactivate; set on login/refresh (post-commit) |

## What is not cached

- Access tokens, refresh tokens, password hashes, secrets
- Write/mutation API responses
- Failed responses
- File binary/stream content
- Full notification lists
- JWT authorization path (claims from token, not cache lookup)

## Security notes

- Sensitive property names are blocked from cache writes (`Password`, `Token`, etc.).
- Cache values are not logged.
- Redis connection strings are not exposed in API responses or logs.
- Internal cache errors use `CacheErrors` codes in logs only; they do not fail business operations.

## Redis production notes

- Use a dedicated Redis instance with persistence appropriate for your SLA.
- Set `InstanceName` per environment to avoid cross-environment key collisions.
- Monitor `/health` — Redis health check is registered when `Provider=Redis`.
- Prefix removal with `SCAN` is non-blocking but can be slower on large key sets; prefer targeted key removal where possible.
- Keep `FailFastOnRedisUnavailableInProduction=true` in production.

## DI registration

```csharp
services.AddCachingServices(configuration, environment);
```

Registered in `AddBuildingBlocksInfrastructure`. Startup logs the active and requested provider.

## Smoke test checklist

1. Set `Cache:Provider=Memory`, start API — log shows `Cache provider active: Memory`.
2. `GET /api/v1/categories/{id}` twice — second request hits cache.
3. `PUT /api/v1/categories/{id}` — subsequent GET returns updated data.
4. Send template email or call render path twice — second call uses cached template metadata.
5. Update/deactivate template — render reflects change immediately after commit.
6. `GET` file metadata by id twice — second call cached; delete invalidates.
7. Call `GetUnreadCountAsync` — count cached; mark read invalidates.
8. Login successfully — permission cache set only after commit; failed login does not set cache.
9. Assign roles to user — permission cache key invalidated.
10. Set `Provider=None` — all reads go to database; app still works.
11. Set `Provider=Redis` with Redis running — health check includes `redis-cache`.
12. Set invalid `Provider` value — startup fails with `Cache.ProviderNotConfigured`.

## Error codes

- `Cache.ProviderNotConfigured` — invalid or unsupported `Cache:Provider` value
- `Cache.SerializationFailed` — JSON serialize/deserialize failure (logged, bypassed)
- `Cache.ConnectionFailed` — Redis connection failure (logged or fail-fast at startup)

These are for logging/diagnostics only, not client-facing API errors.
