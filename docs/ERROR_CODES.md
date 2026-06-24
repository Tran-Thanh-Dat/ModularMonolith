# Error Codes

Central definitions: `src/BuildingBlocks/BuildingBlocks.Application/Errors/ErrorCodes.cs`

## Naming convention

```
{Module}.{Reason}
```

Examples: `Category.NotFound`, `Auth.InvalidCredentials`, `File.TooLarge`

Use PascalCase after the dot. Keep codes stable — clients may branch on them.

## HTTP status mapping

`ResultStatusMapper.MapStatusCode(code)` maps codes to HTTP status. Common rules:

| Pattern / code | HTTP |
|----------------|------|
| `Common.ValidationError` | 400 |
| `Common.Unauthorized`, `Auth.*` invalid token/credentials | 401 |
| `Common.Forbidden`, `Auth.PermissionDenied`, `Notification.Forbidden` | 403 |
| `*.NotFound` | 404 |
| `*.AlreadyExists`, `Common.Conflict` | 409 |
| `Common.UnknownError` | 500 |
| `Email.SendFailed` | 502 |

Suffix heuristics: `.NotFound` → 404, `.AlreadyExists` → 409, `.Invalid` → 400.

## Common errors

| Code | Description |
|------|-------------|
| `Common.Success` | Operation succeeded |
| `Common.ValidationError` | FluentValidation failure |
| `Common.NotFound` | Generic not found |
| `Common.BadRequest` | Invalid request |
| `Common.Unauthorized` | Not authenticated |
| `Common.Forbidden` | Not authorized |
| `Common.Conflict` | Conflict |
| `Common.UnknownError` | Unexpected failure |

## Auth errors

| Code | Description |
|------|-------------|
| `Auth.InvalidCredentials` | Login failed — wrong credentials **or** inactive/deactivated user (intentionally indistinguishable) |
| `Auth.UserInactive` | **Reserved** — defined in `ErrorCodes.cs` but **not returned by login today**; inactive users receive `Auth.InvalidCredentials` instead |
| `Auth.TokenExpired` | Access token expired |
| `Auth.RefreshTokenInvalid` | Invalid/expired refresh, inactive user on refresh (client-safe) |
| `Auth.RefreshTokenReuseDetected` | Logged internally on reuse; client receives `Auth.RefreshTokenInvalid` |
| `Auth.PermissionDenied` | Missing permission |

## Account errors

| Code | Description |
|------|-------------|
| `Account.CurrentPasswordInvalid` | Change password — wrong current password |
| `Account.PasswordResetTokenInvalid` | Reset token invalid, expired, or already used |

## Category errors

`Category.NotFound`, `Category.CodeAlreadyExists`, `Category.AlreadyDeleted`, `Category.AlreadyActive`, `Category.AlreadyInactive`

## File errors

`File.NotFound`, `File.Empty`, `File.TooLarge`, `File.ExtensionNotAllowed`, `File.ContentTypeNotAllowed`, `File.UploadFailed`, `File.DownloadFailed`, `File.InvalidFileName`, `File.AlreadyDeleted`, `File.StorageProviderNotFound`

## Email / notification errors

**Email:** `Email.SendFailed`, `Email.TemplateNotFound`, `Email.TemplateInactive`, `Email.ProviderNotConfigured`, …

**Notification:** `Notification.NotFound`, `Notification.Forbidden`, `Notification.ViewAllRequired`, `Notification.ManageRequired`, `Notification.AlreadyRead`, `Notification.AlreadyArchived`

## Background job errors

`BackgroundJob.NotFound`, `BackgroundJob.Disabled`, `BackgroundJob.RunFailed`, `BackgroundJob.DashboardForbidden`, `BackgroundJob.AlreadyRunning`

## Cache / monitoring errors

**Cache:** `Cache.ProviderNotConfigured`, `Cache.SerializationFailed`, `Cache.ConnectionFailed`

**Monitoring:** `Monitoring.HealthCheckFailed`, `Monitoring.DependencyUnavailable`, `Monitoring.Forbidden`

## Setting errors

`Setting.NotFound`, `Setting.KeyAlreadyExists`, `Setting.InvalidKey`, `Setting.InvalidGroup`, `Setting.InvalidDataType`, `Setting.InvalidValue`, `Setting.NotEditable`, `Setting.SystemSettingCannotBeDeleted`, `Setting.SensitiveValueHidden`, `Setting.EncryptedValueNotReadable`

## Access policy errors

`AccessPolicy.InvalidPasswordPolicy`, `AccessPolicy.InvalidLoginPolicy`, `AccessPolicy.InvalidSessionPolicy`, `AccessPolicy.InvalidMaintenancePolicy`

## Tenant errors

`Tenant.NotFound`, `Tenant.InvalidCode`, `Tenant.InvalidName`, `Tenant.CodeAlreadyExists`, `Tenant.AlreadyDeleted`, `Tenant.AlreadyActive`, `Tenant.AlreadyInactive`, `Tenant.HasActiveDependencies`

## Organization errors

`Organization.NotFound`, `Organization.InvalidCode`, `Organization.InvalidName`, `Organization.InvalidTenant`, `Organization.InvalidSortOrder`, `Organization.CodeAlreadyExists`, `Organization.AlreadyDeleted`, `Organization.AlreadyActive`, `Organization.AlreadyInactive`, `Organization.ParentSelfReference`, `Organization.ParentNotFound`, `Organization.ParentDifferentTenant`, `Organization.ParentInactive`, `Organization.CircularParent`, `Organization.TenantInactive`, `Organization.HasActiveDependencies`, `Organization.Inactive`

## Organization user errors

`OrganizationUser.NotFound`, `OrganizationUser.InvalidUser`, `OrganizationUser.UserNotFound`, `OrganizationUser.AlreadyExists`, `OrganizationUser.AlreadyActive`, `OrganizationUser.AlreadyInactive`, `OrganizationUser.AlreadyRemoved`, `OrganizationUser.TenantInactive`, `OrganizationUser.OrganizationInactive`

## Workspace errors

`Workspace.NotFound`, `Workspace.InvalidCode`, `Workspace.InvalidName`, `Workspace.InvalidTenant`, `Workspace.CodeAlreadyExists`, `Workspace.AlreadyDeleted`, `Workspace.AlreadyActive`, `Workspace.AlreadyInactive`, `Workspace.TenantInactive`, `Workspace.OrganizationInactive`, `Workspace.OrganizationDifferentTenant`, `Workspace.OrganizationNotFound`, `Workspace.HasActiveDependencies`, `Workspace.Inactive`

## Workspace user errors

`WorkspaceUser.NotFound`, `WorkspaceUser.InvalidUser`, `WorkspaceUser.UserNotFound`, `WorkspaceUser.AlreadyExists`, `WorkspaceUser.AlreadyActive`, `WorkspaceUser.AlreadyInactive`, `WorkspaceUser.AlreadyRemoved`, `WorkspaceUser.TenantInactive`, `WorkspaceUser.WorkspaceInactive`, `WorkspaceUser.OrganizationMembershipRequired`

## Authorization Policies errors

Defined in `src/Modules/AuthorizationPolicies/AuthorizationPolicies.Domain/Errors/AuthorizationPolicyErrors.cs`.

### Permission policy

`PermissionPolicy.NotFound`, `PermissionPolicy.CodeAlreadyExists`, `PermissionPolicy.AlreadyDeleted`, `PermissionPolicy.AlreadyActive`, `PermissionPolicy.AlreadyInactive`, `PermissionPolicy.Inactive`, `PermissionPolicy.HasAssignments`

### Role permission policy

`RolePermissionPolicy.NotFound`, `RolePermissionPolicy.AlreadyExists`, `RolePermissionPolicy.AlreadyActive`, `RolePermissionPolicy.AlreadyInactive`, `RolePermissionPolicy.PolicyInactive`

### User permission policy override

`UserPermissionPolicyOverride.NotFound`, `UserPermissionPolicyOverride.AlreadyExists`, `UserPermissionPolicyOverride.AlreadyActive`, `UserPermissionPolicyOverride.AlreadyInactive`, `UserPermissionPolicyOverride.PolicyInactive`, `UserPermissionPolicyOverride.InvalidExpiresAt`

### Authorization matrix

`AuthorizationMatrix.NotFound`, `AuthorizationMatrix.AlreadyExists`, `AuthorizationMatrix.AlreadyDeleted`, `AuthorizationMatrix.AlreadyEnabled`, `AuthorizationMatrix.AlreadyDisabled`

### Authorization check (evaluate / explain)

| Code | HTTP | Description |
|------|------|-------------|
| `AuthorizationCheck.MissingPermissionOrAction` | 400 | Neither `permissionCode` nor `action`+`resourceType` provided |
| `AuthorizationCheck.ScopeNotSupported` | 400 | Scope `Department` or `Custom` (not supported in Phase 24) |
| `AuthorizationCheck.InvalidScope` | 400 | Unknown scope enum |
| `AuthorizationCheck.MissingTenantId` | 400 | Tenant scope without `resourceContext.tenantId` |
| `AuthorizationCheck.MissingOrganizationId` | 400 | Organization scope without `resourceContext.organizationId` |
| `AuthorizationCheck.MissingWorkspaceId` | 400 | Workspace scope without `resourceContext.workspaceId` |
| `AuthorizationCheck.MissingOwnerContext` | 400 | OwnerOnly without `ownerUserId` or `createdBy` |
| `AuthorizationCheck.MissingAssignedUsers` | 400 | AssignedOnly with empty `assignedUserIds` |
| `AuthorizationCheck.MissingSelfContext` | 400 | Self without `resourceId`, `ownerUserId`, or `createdBy` |
| `AuthorizationCheck.UserNotFound` | 404 | Target user not found (evaluator) |

## Master Data errors

Defined in `src/Modules/MasterData/MasterData.Domain/Errors/MasterDataErrors.cs`.

### Master data group

`MasterDataGroup.NotFound`, `MasterDataGroup.CodeAlreadyExists`, `MasterDataGroup.AlreadyDeleted`, `MasterDataGroup.AlreadyActive`, `MasterDataGroup.AlreadyInactive`, `MasterDataGroup.Inactive`, `MasterDataGroup.HasActiveItems`, `MasterDataGroup.SystemProtected`, `MasterDataGroup.CodeChangeNotAllowed`, `MasterDataGroup.InvalidScope`, `MasterDataGroup.InvalidTenant`, `MasterDataGroup.TenantInactive`, `MasterDataGroup.InvalidOrganization`, `MasterDataGroup.OrganizationInactive`, `MasterDataGroup.OrganizationTenantMismatch`

### Master data item

`MasterDataItem.NotFound`, `MasterDataItem.CodeAlreadyExists`, `MasterDataItem.AlreadyDeleted`, `MasterDataItem.AlreadyActive`, `MasterDataItem.AlreadyInactive`, `MasterDataItem.GroupInactive`, `MasterDataItem.SystemProtected`, `MasterDataItem.CodeChangeNotAllowed`, `MasterDataItem.InvalidParent`, `MasterDataItem.ParentSelfReference`, `MasterDataItem.CircularParent`, `MasterDataItem.InvalidEffectiveRange`, `MasterDataItem.InactiveCannotBeDefault`

### Lookup

`Lookup.GroupCodeRequired`, `Lookup.GroupCodesRequired`, `Lookup.TooManyGroupCodes`, `Lookup.GroupNotFound`

## Async Tasks errors

Defined in `src/Modules/AsyncTasks/AsyncTasks.Domain/Errors/AsyncTaskErrors.cs`.

`AsyncTask.NotFound`, `AsyncTask.TaskNoAlreadyExists`, `AsyncTask.InvalidStatus`, `AsyncTask.InvalidType`, `AsyncTask.InvalidProgress`, `AsyncTask.CannotCancel`, `AsyncTask.CannotRetry`, `AsyncTask.MaxRetryExceeded`, `AsyncTask.MessageQueueDisabled`, `AsyncTask.PublishFailed`, `AsyncTask.ProcessorNotFound`, `AsyncTask.ProcessingFailed`, `AsyncTask.PayloadTooLarge`

## Adding a new error code

1. Add `public const string MyError = "MyModule.MyError";` to appropriate static class in `ErrorCodes.cs` (or module-specific file if split later)
2. Return `Result.Failure(MyModuleErrors.MyError, "Safe user message")` from handler
3. Add explicit mapping in `ResultStatusMapper` if suffix heuristics are insufficient
4. Document in this file

## Example handler usage

```csharp
if (category is null)
{
    return Result<CategoryDetail>.Failure(
        CategoryErrors.NotFound,
        "Category was not found.");
}
```

Controller maps via `FromResult` → `404` with:

```json
{
  "success": false,
  "code": "Category.NotFound",
  "message": "Category was not found.",
  "traceId": "..."
}
```

Production 500 responses do not expose internal exception messages — see [../SECURITY.md](../SECURITY.md).
