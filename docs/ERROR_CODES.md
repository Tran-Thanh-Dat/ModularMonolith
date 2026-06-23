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
