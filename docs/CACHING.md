# Caching

Cache abstraction in BuildingBlocks supports **None**, **Memory**, and **Redis**.

Config: `Cache` section in `appsettings.json`. See also `src/BuildingBlocks/Caching/README.md`.

## Providers

| Provider | Use case |
|----------|----------|
| `Memory` | Single instance, local dev, integration tests |
| `Redis` | Multi-instance Production (required for consistent permissions cache) |
| `None` | Disable cache reads/writes |

Production with `Provider=Redis` requires valid `Cache:Redis:ConnectionString` — startup validator enforces this.

## Key convention

Prefix: `v1:` — defined in `CacheKeys` (`BuildingBlocks.Application`).

| Key | Data |
|-----|------|
| `v1:user:{userId}:permissions` | Permission code array |
| `v1:categories:detail:{id}` | Category detail DTO |
| `v1:categories:list:{hash}` | Paged category list |
| `v1:email-templates:code:{code}` | Template render snapshot |
| `v1:files:detail:{id}` | File metadata DTO |
| `v1:notifications:unread-count:{userId}` | Unread count (TTL ~15 min) |

## Invalidation

Mutations enqueue operations via `ICacheOperationBuffer`:

- `EnqueueSet` / `EnqueueRemove` / `EnqueueRemoveByPrefix`

`CacheInvalidationPostCommitHook` applies them after successful DB commit.

## What is cached

- Category list/detail
- Email template by code (for render)
- File metadata detail
- Notification unread count
- User permissions snapshot (set on login/refresh)

## What is NOT cached

- Access tokens / refresh tokens
- Password hashes
- Raw file binary streams
- Secrets or connection strings

## Multi-instance

Use **Redis** in Production when running more than one API instance. `FailFastOnRedisUnavailableInProduction` prevents silent fallback to per-node memory caches.

## Memory limitations

Memory cache is per process — invalidation on one node does not affect others. Fine for dev/single instance only.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Stale permissions after role change | User must refresh token; verify cache invalidation on Users mutations |
| Stale category list | Cache key includes filters — verify invalidation prefix on write |
| Redis connection errors | `/health/ready`, `Cache:Redis:ConnectionString`, Production validator |
| Cache miss every time | `Cache:Provider`, `Cache:EnableLogging` |

See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md).
