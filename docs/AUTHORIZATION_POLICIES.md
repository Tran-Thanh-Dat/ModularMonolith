# Authorization Policies (Phase 24 / 24.1)

Module mở rộng authorization theo **scope** mà không thay thế `[HasPermission]` / JWT permission hiện tại.

## Schema

PostgreSQL schema: `authorization`

| Table | Mô tả |
|-------|--------|
| `permission_policies` | Policy: permission + module/action/resource + scope + effect |
| `role_permission_policies` | Gán policy cho role |
| `user_permission_policy_overrides` | Override Allow/Deny ở user (Deny ưu tiên cao) |
| `authorization_matrix_entries` | Matrix: module/resource/action/scope → required permission |

## Evaluation order (Phase 24.1)

`CheckAsync`, `AuthorizeAsync`, và `ExplainAsync` dùng **cùng** `EvaluateInternalAsync`:

1. Resolve matrix entry (optional) hoặc dùng `permissionCode` trực tiếp.
2. Resolve scope: explicit scope (`AuthorizeAsync`) hoặc matrix scope hoặc `Global`.
3. Validate resource context theo scope (thiếu context → Deny, không Allow).
4. Load active role policies (assignment active + policy active).
5. Load active user overrides (assignment active + policy active + chưa hết hạn).
6. **User Deny override** (priority cao nhất, tie-break `PolicyCode`) → Deny.
7. **Role Deny policy** (priority cao nhất, tie-break `PolicyCode`) → Deny.
8. User phải có permission tương ứng (JWT / role permissions) — **không bỏ qua bước này**.
9. Role Allow policy (priority cao nhất) xác định scope nếu có; không thì dùng scope từ bước 2.
10. Kiểm tra **scope membership** (tenant/org/workspace/owner/assigned/self).
11. Allow nếu pass tất cả.

### User Allow override

User Allow override **không** cấp permission grant. Chỉ tham gia metadata/effective policy list; permission vẫn phải có trong JWT/role permissions.

### Inactive / expired

| Trạng thái | Hành vi |
|------------|---------|
| Inactive policy | Bỏ qua khi load role policies và user overrides |
| Inactive role assignment | Bỏ qua |
| Expired user override (`ExpiresAt <= now`) | Bỏ qua |
| Disabled matrix entry | Không dùng khi lookup matrix |

### Conditions jsonb

Field `Conditions` trên `PermissionPolicy` được lưu DB (jsonb) nhưng **chưa được evaluate động** trong Phase 24/24.1. Reserved for future — không dùng script/eval.

## Scopes

| Scope | Phase 24.1 |
|-------|------------|
| Global | Supported |
| Tenant | Supported — requires `TenantId` + membership |
| Organization | Supported — requires `OrganizationId` + membership |
| Workspace | Supported — requires `WorkspaceId` + membership |
| OwnerOnly | Supported — requires `OwnerUserId` or `CreatedBy`; user must match |
| AssignedOnly | Supported — requires non-empty `AssignedUserIds`; user must be listed |
| Self | Supported — requires `ResourceId`, `OwnerUserId`, or `CreatedBy`; user must match |
| Department | Not supported — Deny / BadRequest |
| Custom | Not supported — Deny / BadRequest |

## API scope validation

Authorization check API (`/authorization-checks/evaluate`, `/explain`) validate resource context sau khi resolve scope từ matrix (nếu có action+resourceType). Scope thiếu context → `400 BadRequest` với error code rõ (`AuthorizationCheck.MissingTenantId`, …).

Evaluator service cũng validate context trước scope membership để đảm bảo không Allow khi thiếu dữ liệu.

## Integration

- Membership: `IOrganizationUserService`, `IWorkspaceUserService`, `IIdentityUserRepository`.
- Không FK cross-module tới Identity/Organizations entities.
- Không global tenant EF filter.
- `AuthorizeAsync` delegate cùng evaluator với `CheckAsync` — không có luồng logic riêng.

## Seed

- Matrix baseline cho Organizations (Tenant/Organization/Workspace View/Manage).
- Permissions mới seed qua `IdentitySeeder` khi restart (6 permissions Phase 24).

## Technical debt

- Cache effective policies / matrix / membership chưa implement.
- Workspace membership query capped at page size 1000 trong `CurrentUserPermissionContextService`.
- Matrix lookup without `moduleCode` có thể ambiguous khi nhiều module dùng cùng resource/action.

## API

Xem [API-DOCUMENT.md](./API-DOCUMENT.md) — section **16. Authorization Policies**.

Error codes: [ERROR_CODES.md](./ERROR_CODES.md#authorization-policies-errors).
