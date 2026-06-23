# Categories Module — Master Data Template



This module is the reference pattern for new business / master-data modules in the modular monolith.



## Conventions



- **Result & errors:** Use `Result<T>` and centralized `ErrorCodes` (e.g. `CategoryErrors`). Do not hardcode error strings in handlers.

- **Controllers:** Inherit `BaseApiController`. Use `FromResult`, `FromPagedResult`, and `CreatedFromResult`. Never access `result.Value` directly.

- **Pagination:** Use `PagedRequest` / `PagedResponse` for list APIs (`pageIndex`, `pageSize`, max 100).

- **Reads:** Use `QueryReadOnly()` or `AsNoTracking()` for query handlers.

- **Writes:** Do not call `SaveChanges` in handlers when `TransactionBehavior` owns the command pipeline.

- **Activity logs:** Use `EnqueuePostCommitAsync` for success events. Use `LogImmediateAsync` (or failure-specific helpers) for failed/security events only.

- **Audit logs:** Register `ISaveChangesInterceptor` on any new `DbContext` that should produce EF change-tracking audit entries.

- **Permissions:** Add codes to `PermissionCodes.All` in Identity.Application for seeding.



## Unit of work (multi-DbContext)



Each module owns its own `DbContext` and typed unit of work. **Do not inject plain `IUnitOfWork` into module-specific services** — the last `IUnitOfWork` registration would win for direct injection and can bind the wrong DbContext.



**Pattern for every new module:**



1. Register the concrete unit of work as itself (e.g. `CategoriesUnitOfWork`, `IdentityUnitOfWork`).

2. Also register it as `IUnitOfWork` so `TransactionBehavior` receives it via `IEnumerable<IUnitOfWork>`.



```csharp

services.AddScoped<CategoriesUnitOfWork>();

services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CategoriesUnitOfWork>());

```



- **Module services / repositories:** inject the typed unit of work (`CategoriesUnitOfWork`, `IdentityUnitOfWork`).

- **TransactionBehavior:** depends on `IEnumerable<IUnitOfWork>`, not a single `IUnitOfWork`. It calls `SaveChangesAsync` on each registered instance and flushes post-commit activity logs after a successful commit.



## API routes



Business APIs use versioned routes under `/api/v1/...` (aligned with Users and Auth):



- `GET    /api/v1/categories`

- `GET    /api/v1/categories/{id}`

- `POST   /api/v1/categories`

- `PUT    /api/v1/categories/{id}`

- `DELETE /api/v1/categories/{id}`

- `PATCH  /api/v1/categories/{id}/activate`

- `PATCH  /api/v1/categories/{id}/deactivate`



Audit/activity log read APIs remain on non-versioned routes (`/api/audit-logs`, `/api/activity-logs`) until a future versioning pass.



## Code immutability



Category **Code** is set at creation and cannot be changed via update. `UpdateCategory` accepts only `Name`, `Description`, and `SortOrder`. This keeps the partial unique index on `Code` (where `is_deleted = false`) stable and avoids breaking references that key off code.



## Permissions



Seeded via `IdentitySeeder` from `PermissionCodes.All`:



- `Category.View`, `Category.Create`, `Category.Update`, `Category.Delete`, `Category.Activate`, `Category.Deactivate`



Admin and SuperAdmin roles receive all permissions on startup (idempotent — no duplicate role-permission mappings). Restart the app against an existing dev database to pick up newly added permission codes.



## Layer layout



```

Categories.Domain       — entities, domain behavior

Categories.Application  — CQRS, validators, DTOs, abstractions

Categories.Infrastructure — DbContext, EF config, services, DI

Categories.Api          — controllers, request contracts

```



## DI order in ApiHost



1. `AddAuditLogsInfrastructure` (registers audit interceptor)

2. `AddIdentityInfrastructure`

3. `AddUsersInfrastructure`

4. `AddCategoriesInfrastructure` (DbContext + interceptors + typed UoW)



## EF migrations



```powershell

dotnet ef migrations add <Name> --project src/Modules/Categories/Categories.Infrastructure --startup-project src/ApiHost --context CategoriesDbContext

dotnet ef database update --project src/Modules/Categories/Categories.Infrastructure --startup-project src/ApiHost --context CategoriesDbContext

```



## Smoke test checklist



Run after changes to Categories, Identity, or transaction/activity-log behavior:



1. Login as SuperAdmin (`POST /api/v1/auth/login`).

2. `GET /api/v1/categories` returns **200**.

3. `POST /api/v1/categories` create returns **201**.

4. Duplicate `Code` returns **409** with error code `Category.CodeAlreadyExists` (app-level check or DB unique index).

5. `GET /api/v1/categories/{id}` with empty Guid returns **400** (validation).

6. `PUT /api/v1/categories/{id}` update works; Code is unchanged.

7. `PATCH /api/v1/categories/{id}/deactivate` works.

8. `PATCH /api/v1/categories/{id}/activate` works.

9. `DELETE /api/v1/categories/{id}` soft delete works.

10. ActivityLog entry appears **after** successful commit (not on failed duplicate create).

11. AuditLog records create/update/delete on the categories table.

12. Auth/User APIs still work (`GET /api/v1/users`, login/refresh/logout).


