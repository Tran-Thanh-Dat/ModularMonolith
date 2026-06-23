# Pagination and Filtering

## Defaults

Defined in `BuildingBlocks.Application/Pagination/PagedRequest.cs`:

| Parameter | Default | Max |
|-----------|---------|-----|
| `pageIndex` | 1 | — |
| `pageSize` | 20 | **100** |

Normalization:

- `pageIndex < 1` → `1`
- `pageSize < 1` → `20`
- `pageSize > 100` → capped at `100`

FluentValidation uses `InclusiveBetween(1, 100)` on page size in module validators.

## Query parameters

List endpoints typically accept:

```
GET /api/v1/categories?pageIndex=1&pageSize=20&keyword=elec&isActive=true
```

Common patterns:

| Parameter | Purpose |
|-----------|---------|
| `pageIndex` | 1-based page number |
| `pageSize` | Items per page |
| `keyword` | Free-text search (module-specific) |
| `isActive` | Boolean filter |
| `fromDate` / `toDate` | Date range (where supported) |

### Sorting (endpoint-specific)

`PagedRequest` defines `SortBy` and `SortDirection`, but **not every list endpoint exposes them**. Sort support is per controller/query — check the module guide or Swagger for that endpoint.

**Do not assume** `sortBy` / `sortDirection` query params work unless documented for that API.

| Endpoint | Sort params exposed? | Default order (when known) |
|----------|----------------------|----------------------------|
| `GET /api/v1/categories` | No | `SortOrder` ascending, then `CreatedAt` descending |
| `GET /api/v1/files` | No | Module-specific (typically newest first) |
| `GET /api/v1/notifications` | No | Module-specific |

When adding sort to a new endpoint, document it in the module guide and validate allowed sort fields in the query/handler.

## PagedResponse shape

See [API_CONVENTIONS.md](./API_CONVENTIONS.md).

Handler returns `Result<PagedResult<T>>`; controller uses `FromPagedResult`.

## Read-only queries

List/detail queries should:

- Use `AsNoTracking()` in repositories for read paths
- Avoid unnecessary `Include` graphs
- Prefer projected DTOs over full entities

Example pattern in infrastructure services:

```csharp
var query = _unitOfWork.Repository<Category, Guid>()
    .QueryReadOnly()
    .Where(...);
```

## Examples

### Categories

```bash
curl "http://localhost:5080/api/v1/categories?pageIndex=1&pageSize=10&keyword=phone" \
  -H "Authorization: Bearer TOKEN"
```

### Files

```bash
curl "http://localhost:5080/api/v1/files?pageIndex=1&pageSize=20&moduleName=Categories&isTemporary=false" \
  -H "Authorization: Bearer TOKEN"
```

### Notifications

```bash
curl "http://localhost:5080/api/v1/notifications?pageIndex=1&pageSize=20&isRead=false" \
  -H "Authorization: Bearer TOKEN"
```

## Caching note

Category list cache keys include `pageIndex`, `pageSize`, and filter hash — changing page parameters misses cache intentionally for filtered lists. See [CACHING.md](./CACHING.md).
