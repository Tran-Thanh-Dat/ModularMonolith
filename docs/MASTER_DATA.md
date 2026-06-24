# Master Data Module (Phase 25)

> Quản lý dữ liệu tham chiếu dùng chung (lookup groups/items) — schema `master_data`.

## Overview

Module **MasterData** cung cấp:

- **MasterDataGroup** — nhóm lookup (ví dụ `USER_STATUS`, `PRIORITY`)
- **MasterDataItem** — item trong group (ví dụ `ACTIVE`, `MEDIUM`)
- **Lookups API** — đọc nhanh lookup active cho FE/module khác

Không thay thế **Categories** (danh mục nghiệp vụ riêng). Không có global tenant filter — scope enforce trong service/query.

## Schema

| Table | Mô tả |
|-------|--------|
| `master_data.master_data_groups` | Group lookup |
| `master_data.master_data_items` | Item thuộc group |

## Scope

| Scope | TenantId | OrganizationId |
|-------|----------|----------------|
| Global | null | null |
| Tenant | required | null |
| Organization | required | required |

Organization phải thuộc đúng tenant (validate qua `IOrganizationService`).

**Scope validation (Phase 25.1):** Khi tạo/sửa group **active** với scope Tenant hoặc Organization:

- Tenant phải tồn tại và `IsActive = true` (`MasterDataGroup.TenantInactive` nếu inactive)
- Organization phải tồn tại, thuộc tenant, và `IsActive = true` (`MasterDataGroup.OrganizationInactive` nếu inactive)

Global scope không yêu cầu tenant/org.

## Permissions

| Permission | Usage |
|------------|--------|
| `MasterDataGroup.View` | GET groups |
| `MasterDataGroup.Manage` | Mutate groups |
| `MasterDataItem.View` | GET items |
| `MasterDataItem.Manage` | Mutate items |
| `Lookup.View` | GET/POST lookups (bao gồm `includeInactive=true`) |

**Không có lookup fallback** org → tenant → global; lookup chỉ trả đúng scope được chỉ định.

## Authorization matrix baseline

Seeded idempotent trong `AuthorizationPoliciesSeeder`:

- MasterData / MasterDataGroup / View|Manage / Global
- MasterData / MasterDataItem / View|Manage / Global
- MasterData / Lookup / View / Global, Tenant, Organization

## System seed (idempotent)

8 global system groups: `USER_STATUS`, `FILE_TYPE`, `APPROVAL_STATUS`, `PRIORITY`, `LANGUAGE`, `JOB_STATUS`, `IMPORT_STATUS`, `EXPORT_STATUS` với items mặc định. Không ghi đè name/metadata nếu admin đã chỉnh.

Default items: USER_STATUS→ACTIVE, PRIORITY→MEDIUM, LANGUAGE→VI, APPROVAL_STATUS→DRAFT.

## Cache

Lookup read-through cache qua `ICacheService.GetOrSetAsync`:

- Key prefix: `v1:master-data:lookup:...` (scope, tenant, org, groupCode, inactive, metadata, effective date)
- Invalidate post-commit qua `ICacheInvalidationBuffer.EnqueueRemoveByPrefix` khi mutate group/item

## Activity & audit

- **Activity log:** post-commit cho group/item mutations (`MasterDataGroupCreated`, …)
- **Audit log:** EF change tracking tự động (module `MasterData` trong interceptor)

## Business rules (high level)

- Unique group code theo scope + tenant + organization
- Unique item code trong group
- Một default item active per group
- Parent item cùng group, không circular
- System group/item: không delete, không đổi code
- Deactivate/delete group khi còn active items → reject
- Lookup active: group + item active, effective date filter (default `effectiveAt` = `IDateTimeProvider.UtcNow`)
- GET `/api/v1/lookups?groupCodes=...` và POST batch: tối đa **50** group codes (`Lookup.TooManyGroupCodes`)

## API

Xem [API-DOCUMENT.md §17](./API-DOCUMENT.md#17-master-data).

## Error codes

Xem [ERROR_CODES.md](./ERROR_CODES.md#master-data-errors).

## Migration

```bash
dotnet ef migrations add AddMasterDataModule --project src/Modules/MasterData/MasterData.Infrastructure --startup-project src/ApiHost --context MasterDataDbContext
dotnet ef database update --project src/Modules/MasterData/MasterData.Infrastructure --startup-project src/ApiHost --context MasterDataDbContext
```

## Technical debt / Phase 26+

- Lookup fallback chain (org → tenant → global) chưa implement
- Batch lookup loops per groupCode (acceptable for max 50); chưa optimize single IN query
- Authorization matrix evaluator chưa wire trực tiếp vào MasterData controllers (dùng `[HasPermission]`)
- i18n metadata engine chưa có (metadata jsonb only)
