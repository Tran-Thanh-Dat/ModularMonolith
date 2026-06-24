# Authorization

Permission-based authorization using JWT claims and `[HasPermission]`.

## Model

- **Users** have **Roles**
- **Roles** have **Permissions** (string codes)
- JWT access token includes `permission` claims for active roles
- Endpoints require `[Authorize]` and optionally `[HasPermission("Code")]`

Built-in roles (seeded in `IdentitySeeder`):

| Role | Current behavior |
|------|------------------|
| `Admin` | Receives **all** permissions from `PermissionCodes.All` |
| `SuperAdmin` | Receives **all** permissions from `PermissionCodes.All` |

**Important:** Today there is **no permission difference** between `Admin` and `SuperAdmin` — both get every seeded permission, including audit/activity log access. Role names/descriptions differ, but authorization is equivalent until you explicitly split assignments in `IdentitySeeder` or via the admin API.

**Future:** To restrict Admin (e.g. exclude audit logs), assign a subset of permissions in `SeedAdminRoleAsync` instead of all permissions. Any SuperAdmin-only access must be implemented explicitly — it is not enforced by code today.

## Permission code format

```
{Module}.{Action}
```

Examples: `Category.View`, `File.Upload`, `Notification.ViewAll`

Canonical registry: `src/Modules/Identity/Identity.Application/Permissions/PermissionCodes.cs`

## Module permission reference

### Users (`UsersPermissionCodes`)

`Users.View`, `Users.Create`, `Users.Update`, `Users.Delete`, `Users.Activate`, `Users.Deactivate`, `Users.AssignRole`, `Users.AssignPermission`

### Categories (`CategoriesPermissionCodes`)

`Category.View`, `Category.Create`, `Category.Update`, `Category.Delete`, `Category.Activate`, `Category.Deactivate`

### Files (`FilesPermissionCodes`)

`File.View`, `File.Upload`, `File.Download`, `File.Delete`, `File.MarkPermanent`

### Notifications (`NotificationsPermissionCodes`)

**Email:** `Email.View`, `Email.Send`, `Email.TemplateView`, `Email.TemplateCreate`, `Email.TemplateUpdate`, `Email.TemplateDelete`, `Email.TemplateActivate`, `Email.TemplateDeactivate`

**In-app:** `Notification.View`, `Notification.ViewAll`, `Notification.Manage`, `Notification.Create`, `Notification.MarkRead`, `Notification.Archive`

### Background jobs (`BackgroundJobsPermissionCodes`)

`BackgroundJob.View`, `BackgroundJob.Run`, `BackgroundJob.Dashboard`

### Monitoring (`MonitoringPermissionCodes`)

`Monitoring.HealthView`, `Monitoring.SystemInfoView`

### Settings (`SettingsPermissionCodes`)

`Setting.View`, `Setting.Create`, `Setting.Update`, `Setting.Delete`, `Setting.Activate`, `Setting.Deactivate`, `Setting.ViewSensitive`

### Access policy / maintenance

`AccessPolicy.View`, `AccessPolicy.Update`, `Maintenance.View`, `Maintenance.Update`

### Organizations (`OrganizationsPermissionCodes`)

`Tenant.View`, `Tenant.Manage`, `Organization.View`, `Organization.Manage`, `Workspace.View`, `Workspace.Manage`

### Authorization Policies (`AuthorizationPoliciesPermissionCodes`)

`PermissionPolicy.View`, `PermissionPolicy.Manage`, `AuthorizationMatrix.View`, `AuthorizationMatrix.Manage`, `AuthorizationCheck.Execute`, `AuthorizationCheck.Explain`

### Audit logs

`AuditLog.View` (and legacy `AuditLogs.View`), `ActivityLog.View`

### Master Data (`MasterDataPermissionCodes`)

`MasterDataGroup.View`, `MasterDataGroup.Manage`, `MasterDataItem.View`, `MasterDataItem.Manage`, `Lookup.View`

**Matrix baseline (seeded):**

| Resource | View | Manage | Scopes |
|----------|------|--------|--------|
| MasterDataGroup | `MasterDataGroup.View` | `MasterDataGroup.Manage` | Global |
| MasterDataItem | `MasterDataItem.View` | `MasterDataItem.Manage` | Global |
| Lookup | `Lookup.View` | — | Global, Tenant, Organization |

Lookup endpoints use `[HasPermission(Lookup.View)]` for both active and `includeInactive=true` reads — no separate permission for inactive lookups.

## Usage in controllers

```csharp
[Authorize]
[Route("api/v1/categories")]
public sealed class CategoriesController : BaseApiController
{
    [HttpGet]
    [HasPermission(CategoriesPermissionCodes.View)]
    public async Task<ActionResult<PagedResponse<CategoryListItem>>> GetCategories(...) { ... }
}
```

Auth endpoints use `[AllowAnonymous]` (login/refresh) or `[Authorize]` only (`/me`).

## Scoped authorization (Phase 24+)

JWT `[HasPermission]` remains the **primary gate** for API endpoints. The **Authorization Policies** module adds a second layer for **resource-scoped** decisions:

```
User → Role → Permission → Policy → Scope → Resource
```

| Layer | Purpose |
|-------|---------|
| JWT permission | Endpoint access (`[HasPermission]`) |
| Permission policy | Allow/Deny + scope rules per module/action/resource |
| Authorization matrix | Maps action + resourceType → required permission + default scope |
| Evaluator | `CheckAsync`, `AuthorizeAsync`, `ExplainAsync` on `IAuthorizationMatrixService` |

**Important:**

- `[HasPermission]` is **not replaced** — business modules call the evaluator when they need tenant/org/workspace/owner scope.
- Organizations membership is used for Tenant / Organization / Workspace scopes.
- User **Deny override** beats Allow; inactive policies and expired overrides are ignored.

Full design: [AUTHORIZATION_POLICIES.md](./AUTHORIZATION_POLICIES.md).

## Cross-user access

### Notifications

| Scenario | Required permission |
|----------|---------------------|
| View own notifications | `Notification.View` |
| List/view another user's notifications | `Notification.ViewAll` |
| Manage/create for others | `Notification.Manage` |

Enforced in `NotificationAuthorizationService`.

### Files

Access is **permission-based only** — `File.View`, `File.Download`, etc. The Files module does **not** enforce per-user file ownership today. Business modules that need ownership should validate `ModuleName` / `ReferenceType` / `ReferenceId` at the consuming layer, or extend Files in a future phase.

## Adding a new permission

1. Add constant to module `*PermissionCodes.cs`
2. Add to `PermissionCodes.All` in Identity if central seed should include it
3. Seed permission in `IdentitySeeder.SeedPermissionsAsync` (idempotent)
4. Assign to appropriate roles in seeder or via admin API
5. Apply `[HasPermission]` on controller actions
6. Document in this file

## JWT permission claims

Claims type: `permission` (one claim per permission code).

Authorization handler in BuildingBlocks validates against `[HasPermission]` attribute.

## After permission changes

- Permission snapshot cached in Redis/memory under `v1:user:{userId}:permissions`
- Updated on login/refresh
- Invalidated when roles/permissions change via Users module (post-commit cache invalidation)
- **Users must refresh token or re-login** to get updated JWT claims

## 401 vs 403

| Status | Meaning |
|--------|---------|
| **401** | No token, expired token, invalid credentials |
| **403** | Valid token but missing required permission |

Authorization failures may be logged via `AuthorizationFailureActivityLoggingHandler`.
