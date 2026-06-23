# AI References Index

Không duplicate nội dung dài — link tới `docs/` và file gốc.

## Business module (bắt buộc khi tạo module mới)

| Doc | Mục đích |
|-----|----------|
| [BUSINESS_MODULE_RULES.md](../docs/BUSINESS_MODULE_RULES.md) | Quy chuẩn bắt buộc (35 sections) |
| [BUSINESS_MODULE_WORKFLOW_GUIDE.md](../docs/BUSINESS_MODULE_WORKFLOW_GUIDE.md) | Quy trình 10 bước + template Cursor |
| [MODULE_DEVELOPMENT_GUIDE.md](../docs/MODULE_DEVELOPMENT_GUIDE.md) | Checklist kỹ thuật + Products example |

## Architecture & API

| Doc | Mục đích |
|-----|----------|
| [TECHNICAL_ARCHITECTURE.md](../docs/TECHNICAL_ARCHITECTURE.md) | **TA đầy đủ — cấu trúc repo, module map, onboarding** |
| [KIEN_TRUC.md](../docs/KIEN_TRUC.md) | Kiến trúc (Tiếng Việt) — layering, MediatR, UoW |
| [ARCHITECTURE.md](../docs/ARCHITECTURE.md) | Architecture (English) |
| [API_CONVENTIONS.md](../docs/API_CONVENTIONS.md) | Routes, ApiResponse, status codes |
| [AUTHENTICATION.md](../docs/AUTHENTICATION.md) | JWT, refresh |
| [AUTHORIZATION.md](../docs/AUTHORIZATION.md) | Permissions, Admin/SuperAdmin |
| [ERROR_CODES.md](../docs/ERROR_CODES.md) | Error naming, HTTP mapping |
| [PAGINATION.md](../docs/PAGINATION.md) | pageIndex/pageSize, sort per endpoint |
| [POST_COMMIT_HOOKS.md](../docs/POST_COMMIT_HOOKS.md) | Cache, activity, file compensation |

## Cross-cutting modules

| Doc | Mục đích |
|-----|----------|
| [CACHING.md](../docs/CACHING.md) | Redis/Memory, CacheKeys |
| [FILES.md](../docs/FILES.md) | Upload, validation, permission-only access |
| [NOTIFICATIONS.md](../docs/NOTIFICATIONS.md) | Email, in-app, ownership |
| [BACKGROUND_JOBS.md](../docs/BACKGROUND_JOBS.md) | Hangfire, recurring jobs |

## Ops & quality

| Doc | Mục đích |
|-----|----------|
| [LOCAL_DEVELOPMENT.md](../docs/LOCAL_DEVELOPMENT.md) | SDK, secrets, Swagger |
| [TESTING.md](../TESTING.md) | Test baseline |
| [TESTING_GUIDE.md](../docs/TESTING_GUIDE.md) | Module tests, skipped tests |
| [SECURITY.md](../SECURITY.md) | Production hardening |
| [PRODUCTION.md](../PRODUCTION.md) | Deploy |

## Code paths thường dùng

| Area | Path |
|------|------|
| Error codes | `src/BuildingBlocks/BuildingBlocks.Application/Errors/ErrorCodes.cs` |
| Permission registry | `src/Modules/Identity/Identity.Application/Permissions/PermissionCodes.cs` |
| Base controller | `src/BuildingBlocks/BuildingBlocks.Web/Controllers/BaseApiController.cs` |
| Transaction pipeline | `src/BuildingBlocks/BuildingBlocks.Application/Behaviors/TransactionBehavior.cs` |
| Cache keys | `src/BuildingBlocks/BuildingBlocks.Application/Caching/CacheKeys.cs` |
| Category reference module | `src/Modules/Categories/` |

## Facts quan trọng (tránh doc cũ)

- **Không có `UsersDbContext`** — Users dùng `IdentityDbContext` / `IdentityUnitOfWork`
- **Admin và SuperAdmin** hiện nhận **tất cả** permissions (chưa tách quyền)
- **Files** — permission-based; chưa enforce per-user ownership
- **Inactive login** → `Auth.InvalidCredentials` (không `Auth.UserInactive`)
