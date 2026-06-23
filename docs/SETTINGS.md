# System Settings

Dynamic configuration stored in PostgreSQL (`settings.system_settings`) without redeploying the application.

## Purpose

- Centralize operational and business configuration
- Support typed access policies (password, login, session, maintenance)
- Mask sensitive values in API responses and audit logs
- Cache frequently read settings

## Entity: `SystemSetting`

| Field | Notes |
|-------|-------|
| `Key` | Unique among non-deleted rows; immutable after create |
| `Group` | Logical grouping (`PasswordPolicy`, `Maintenance`, …) |
| `Name`, `Description` | Display metadata |
| `Value`, `DefaultValue` | Stored as strings; validated by `DataType` |
| `DataType` | `String`, `Number`, `Boolean`, `Decimal`, `Json`, `TimeSpan`, `Email`, `Url` |
| `IsEncrypted`, `IsSensitive` | Encrypted/sensitive values masked unless caller has `Setting.ViewSensitive` |
| `IsSystem` | System rows cannot be soft-deleted |
| `IsEditable` | Non-editable rows reject value updates |
| `IsActive`, soft-delete audit fields | Standard module pattern |

## API Routes

Base: `/api/v1/settings` (auth required)

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/` | `Setting.View` |
| GET | `/{id}` | `Setting.View` |
| GET | `/key/{key}` | `Setting.View` |
| GET | `/groups/{group}` | `Setting.View` |
| POST | `/` | `Setting.Create` |
| PUT | `/{id}` | `Setting.Update` |
| PATCH | `/{id}/value` | `Setting.Update` |
| DELETE | `/{id}` | `Setting.Delete` |
| PATCH | `/{id}/activate` | `Setting.Activate` |
| PATCH | `/{id}/deactivate` | `Setting.Deactivate` |

Sensitive values return `IsValueMasked = true` and `***` unless the caller has `Setting.ViewSensitive`.

## Permissions

`Setting.View`, `Setting.Create`, `Setting.Update`, `Setting.Delete`, `Setting.Activate`, `Setting.Deactivate`, `Setting.ViewSensitive`

See [ACCESS_POLICY.md](./ACCESS_POLICY.md) for policy-specific permissions.

## Cache

| Key pattern | Content |
|-------------|---------|
| `v1:settings:key:{key}` | Resolved string value (non-sensitive only) |
| `v1:settings:detail:{id}` | Detail DTO (non-sensitive) |
| `v1:settings:group:{group}` | Group list |
| `v1:settings:list:{hash}` | Paged list |

Invalidation runs post-commit on create/update/delete/activate/deactivate and policy updates.

## Audit / Activity

- Module name: `Settings`
- Activity logs: create, update, value update, delete, activate, deactivate, policy updates
- EF change tracking via `AuditChangeTrackingInterceptor`
- Sensitive values must not appear in audit old/new JSON

## Seeding

`ISettingsSeeder` runs after Identity seed on startup when `Database:ApplyMigrationsOnStartup` is true. Existing rows are never overwritten.

Defaults include password/login/session/file-storage/notification/maintenance keys. Session token lifetimes seed from `Jwt` / `RefreshToken` appsettings when present.

## Examples

```http
GET /api/v1/settings/groups/Maintenance
Authorization: Bearer {token}
```

```http
PATCH /api/v1/settings/{id}/value
Content-Type: application/json

{ "value": "true" }
```

## Known Limitations

- No encryption/decryption service in this phase (`IsEncrypted` values are not readable via API)
- Not a feature-flag or secret-store platform
- Do not store JWT signing keys or SMTP passwords as settings
- Policy updates require underlying settings to exist (seeded on first startup)
