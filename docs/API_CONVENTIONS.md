# API Conventions

## Base route

Most business APIs use:

```
/api/v1/{resource}
```

**Exceptions (legacy routes, no `v1` prefix):**

- `GET /api/audit-logs`
- `GET /api/activity-logs`

New modules should prefer `/api/v1/...`.

## Controllers

- Inherit `BaseApiController` (`BuildingBlocks.Web`)
- Use `[ApiController]` and explicit `[Route]`
- Protected endpoints: `[Authorize]` + `[HasPermission("Module.Action")]`
- Map handler results with `FromResult`, `FromPagedResult`, or `CreatedFromResult`
- Never return EF entities directly — use Application DTOs / response models

## ApiResponse&lt;T&gt;

Success:

```json
{
  "success": true,
  "code": "Common.Success",
  "message": null,
  "data": { "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "name": "Example" },
  "errors": null,
  "timestamp": "2026-06-19T12:00:00Z",
  "traceId": "0HN5...:00000001"
}
```

Failure:

```json
{
  "success": false,
  "code": "Category.NotFound",
  "message": "Category was not found.",
  "data": null,
  "errors": null,
  "timestamp": "2026-06-19T12:00:00Z",
  "traceId": "0HN5...:00000001"
}
```

Validation failure (`400`):

```json
{
  "success": false,
  "code": "Common.ValidationError",
  "message": "Validation failed.",
  "data": null,
  "errors": [
    { "code": "Category.Code", "message": "Code is required.", "field": "code" }
  ],
  "timestamp": "2026-06-19T12:00:00Z",
  "traceId": "0HN5...:00000001"
}
```

## PagedResponse&lt;T&gt;

```json
{
  "success": true,
  "code": "Common.Success",
  "message": null,
  "data": [
    { "id": "...", "code": "CAT-001", "name": "Electronics" }
  ],
  "pagination": {
    "pageIndex": 1,
    "pageSize": 20,
    "totalItems": 45,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "timestamp": "2026-06-19T12:00:00Z",
  "traceId": "0HN5...:00000001"
}
```

See [PAGINATION.md](./PAGINATION.md).

## TraceId / Correlation ID

- Response includes `traceId` on all API envelopes.
- Clients may send `X-Correlation-Id` header; middleware propagates it to logs and responses.

## HTTP status codes

| Status | When |
|--------|------|
| **200** | Success (GET, PUT, PATCH, POST that returns body) |
| **201** | Created (`CreatedFromResult`) |
| **400** | Validation error, bad request, many business rule failures |
| **401** | Missing/invalid JWT, invalid credentials, invalid refresh token |
| **403** | Authenticated but missing permission |
| **404** | Resource not found |
| **409** | Conflict (duplicate code, concurrency) |
| **429** | Rate limit exceeded |
| **500** | Unhandled server error (sanitized message in Production) |
| **502** | Email send failed (provider error sanitized) |
| **503** | Health readiness failure (dependency down) |

Mapping is centralized in `ResultStatusMapper` (`BuildingBlocks.Application`).

## Controller helpers

| Method | Use |
|--------|-----|
| `FromResult(result)` | Command/query returning `Result<T>` → 200 or mapped error |
| `FromPagedResult(result)` | Paged list |
| `CreatedFromResult(actionName, routeValues, result)` | 201 with `Location` header |
| `OkResponse(data)` | Direct success without Result wrapper |

## Swagger

- **Development / Docker:** enabled by default (`Swagger:Enabled` or environment fallback). JWT Bearer scheme is pre-configured.
- **Production:** disabled by default; startup fails if enabled without reverse-proxy protection.

Use Swagger **Authorize** button with `Bearer {accessToken}` from login — never paste production secrets into Swagger.

**Backlog:** OpenAPI XML comments and per-operation response examples are not enabled yet (`GenerateDocumentationFile` is off in `ApiHost.csproj`).

## Rate limiting

When enabled, excessive requests return `429` with:

```json
{
  "success": false,
  "code": "Common.TooManyRequests",
  "message": "Too many requests. Please try again later."
}
```

Login and refresh have stricter limits than general API. See [../SECURITY.md](../SECURITY.md).
