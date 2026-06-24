# Modules

Overview of implemented feature modules.

## Identity / Auth

| | |
|-|-|
| **Purpose** | Login, JWT, refresh tokens, logout, current user |
| **API** | `/api/v1/auth/*` |
| **Permissions** | Public login/refresh; `/me` requires auth |
| **Entities** | `User`, `Role`, `Permission`, `UserRefreshToken` |
| **Notes** | Refresh rotation + reuse detection; inactive users blocked |

## Account (self-service)

| | |
|-|-|
| **Purpose** | Profile, change password, forgot/reset password |
| **API** | `/api/v1/account/*` |
| **Permissions** | Auth required for profile/change-password; forgot/reset are anonymous |
| **Entities** | `User`, `PasswordResetToken` |
| **Notes** | Revokes refresh tokens on password change/reset; anti-enumeration on forgot |

See [ACCOUNT.md](./ACCOUNT.md).

## Users / Roles / Permissions

| | |
|-|-|
| **Purpose** | User CRUD, activate/deactivate, role & permission assignment |
| **API** | `/api/v1/users` |
| **Permissions** | `Users.*` |
| **Entities** | Uses Identity entities (`User`, `Role`, `Permission`) via `IdentityDbContext` |
| **Notes** | No separate Users DbContext; permission cache invalidated on role changes. Request flow: [CQRS_REQUEST_FLOW.md](./CQRS_REQUEST_FLOW.md) |

## Categories

| | |
|-|-|
| **Purpose** | Reference data CRUD with soft activate/deactivate |
| **API** | `/api/v1/categories` |
| **Permissions** | `Category.*` |
| **Entities** | `Category` |
| **Notes** | List/detail cached; invalidation on mutations |

## Files

| | |
|-|-|
| **Purpose** | Upload, download, metadata, temporary vs permanent files |
| **API** | `/api/v1/files` |
| **Permissions** | `File.*` |
| **Entities** | `FileResource` |
| **Notes** | Local storage provider; magic-byte validation; cleanup job |

See [FILES.md](./FILES.md).

## Notifications / Email

| | |
|-|-|
| **Purpose** | In-app notifications, SMTP email, templates |
| **API** | `/api/v1/notifications`, `/api/v1/email-templates`, `/api/v1/email-messages` |
| **Permissions** | `Notification.*`, `Email.*` |
| **Entities** | `Notification`, `EmailMessage`, `EmailTemplate` |
| **Notes** | TestMode redirect; cross-user needs ViewAll/Manage |

See [NOTIFICATIONS.md](./NOTIFICATIONS.md).

## Background Jobs

| | |
|-|-|
| **Purpose** | Hangfire recurring jobs, manual triggers, execution history |
| **API** | `/api/v1/background-jobs` |
| **Permissions** | `BackgroundJob.View`, `.Run`, `.Dashboard` |
| **Entities** | `BackgroundJobExecution` |
| **Notes** | Email retry, temp file cleanup, optional log cleanup |

See [BACKGROUND_JOBS.md](./BACKGROUND_JOBS.md).

## Audit Logs

| | |
|-|-|
| **Purpose** | Entity change audit trail (who changed what) |
| **API** | `/api/audit-logs` |
| **Permissions** | `AuditLog.View` / legacy `AuditLogs.View` |
| **Entities** | `AuditLog` |
| **Notes** | Populated by EF change-tracking interceptor |

## Activity Logs

| | |
|-|-|
| **Purpose** | Business/security events (login, CRUD, auth failures) |
| **API** | `/api/activity-logs` |
| **Permissions** | `ActivityLog.View` |
| **Entities** | `ActivityLog` |
| **Notes** | Post-commit flush for enqueued entries |

## Monitoring

| | |
|-|-|
| **Purpose** | Detailed health, system info |
| **API** | `/api/v1/monitoring/*` |
| **Permissions** | `Monitoring.HealthView`, `Monitoring.SystemInfoView` |
| **Notes** | Public probes at `/health/live`, `/health/ready` |

See [MONITORING.md](./MONITORING.md).

## Settings / Access Policy

| | |
|-|-|
| **Purpose** | Dynamic system configuration and typed security policies |
| **API** | `/api/v1/settings`, `/api/v1/access-policy` |
| **Permissions** | `Setting.*`, `AccessPolicy.*`, `Maintenance.*` |
| **Entities** | `SystemSetting` |
| **Notes** | Cached reads; sensitive masking; maintenance middleware; Auth uses session/password policy |

See [SETTINGS.md](./SETTINGS.md) and [ACCESS_POLICY.md](./ACCESS_POLICY.md).

## Organizations

| | |
|-|-|
| **Purpose** | Tenant, organization hierarchy, workspace, user membership |
| **API** | `/api/v1/tenants`, `/api/v1/organizations`, `/api/v1/organization-users`, `/api/v1/workspaces`, `/api/v1/workspace-users` |
| **Permissions** | `Tenant.View`, `Tenant.Manage`, `Organization.View`, `Organization.Manage`, `Workspace.View`, `Workspace.Manage` |
| **Entities** | `Tenant`, `Organization`, `OrganizationUser`, `Workspace`, `WorkspaceUser` |
| **Notes** | Schema `organizations`; no global tenant filter; user id validated via Identity repository |

See [ORGANIZATIONS.md](./ORGANIZATIONS.md).

## Authorization Policies

| | |
|-|-|
| **Purpose** | Permission policy management, authorization matrix, scoped evaluation |
| **API** | `/api/v1/permission-policies`, `/api/v1/authorization-matrix`, `/api/v1/authorization-checks` |
| **Permissions** | `PermissionPolicy.*`, `AuthorizationMatrix.*`, `AuthorizationCheck.*` |
| **Entities** | `PermissionPolicy`, `RolePermissionPolicy`, `UserPermissionPolicyOverride`, `AuthorizationMatrixEntry` |
| **Notes** | Schema `authorization`; extends (does not replace) JWT `[HasPermission]`; uses Organizations membership for scope |

See [AUTHORIZATION_POLICIES.md](./AUTHORIZATION_POLICIES.md).

## Master Data

| | |
|-|-|
| **Purpose** | Shared reference lookup groups/items (USER_STATUS, PRIORITY, …) |
| **API** | `/api/v1/master-data-groups`, `/api/v1/master-data-items`, `/api/v1/lookups` |
| **Permissions** | `MasterDataGroup.View`, `MasterDataGroup.Manage`, `MasterDataItem.View`, `MasterDataItem.Manage`, `Lookup.View` |
| **Entities** | `MasterDataGroup`, `MasterDataItem` |
| **Notes** | Schema `master_data`; scope Global/Tenant/Organization; cached lookups; idempotent system seed |

See [MASTER_DATA.md](./MASTER_DATA.md).

## Cache (BuildingBlocks)

| | |
|-|-|
| **Purpose** | Distributed/in-memory cache abstraction |
| **API** | None (infrastructure) |
| **Notes** | Memory or Redis; post-commit invalidation |

See [CACHING.md](./CACHING.md).

## BuildingBlocks

Shared library — not a business module. Provides Result pattern, MediatR behaviors, `BaseApiController`, cache, health, middleware.

See [ARCHITECTURE.md](./ARCHITECTURE.md).
