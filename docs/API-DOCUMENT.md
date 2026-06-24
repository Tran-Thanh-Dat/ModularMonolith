# API Document

> **Source of truth** danh sách module và API hiện có của hệ thống.  
> Base URL mặc định (dev): `http://localhost:5080`  
> Chi tiết request/response: [API_CONVENTIONS.md](./API_CONVENTIONS.md)

---

## Quy tắc cập nhật (bắt buộc)

**Mỗi khi thêm / sửa / xóa API**, cập nhật file này **trong cùng PR/task** với code.

Checklist khi có API mới:

- [ ] Thêm dòng vào bảng module tương ứng (Method, Route, Auth, Permission, Mô tả)
- [ ] Cập nhật [MODULES.md](./MODULES.md) nếu là module mới
- [ ] Cập nhật doc chi tiết module (`docs/{MODULE}.md`) nếu có
- [ ] Cập nhật [AUTHORIZATION.md](./AUTHORIZATION.md) nếu có permission mới
- [ ] Cập nhật [AUTHORIZATION_POLICIES.md](./AUTHORIZATION_POLICIES.md) nếu thay đổi evaluator / matrix / scope
- [ ] Cập nhật [ERROR_CODES.md](./ERROR_CODES.md) nếu có error code mới

Xem thêm: [ai/rules/feature-documentation.md](../ai/rules/feature-documentation.md)

**Cập nhật lần cuối:** 2026-06-24

---

## Tổng quan module

| # | Module | Mục đích | Prefix API |
|---|--------|----------|------------|
| 1 | **Identity / Auth** | Đăng nhập, JWT, refresh token, logout | `/api/v1/auth` |
| 2 | **Account** | Self-service: profile, đổi/quên/reset mật khẩu | `/api/v1/account` |
| 3 | **Users** | Quản trị user, role, permission (admin) | `/api/v1/users` |
| 4 | **Categories** | Dữ liệu danh mục tham chiếu | `/api/v1/categories` |
| 5 | **Files** | Upload/download file, metadata | `/api/v1/files` |
| 6 | **Notifications** | Thông báo in-app | `/api/v1/notifications` |
| 7 | **Email Templates** | Quản lý template email | `/api/v1/email-templates` |
| 8 | **Email Messages** | Gửi email, lịch sử gửi | `/api/v1/email-messages` |
| 9 | **Background Jobs** | Hangfire jobs, lịch sử chạy job | `/api/v1/background-jobs` |
| 10 | **Audit Logs** | Audit trail thay đổi entity (EF) | `/api/audit-logs` |
| 11 | **Activity Logs** | Sự kiện nghiệp vụ/bảo mật | `/api/activity-logs` |
| 12 | **Monitoring** | Health chi tiết, system info | `/api/v1/monitoring` |
| 13 | **Settings** | Cấu hình hệ thống động | `/api/v1/settings` |
| 14 | **Access Policy** | Chính sách password/login/session/maintenance | `/api/v1/access-policy` |
| 15 | **Organizations** | Tenant, organization, workspace, membership | `/api/v1/tenants`, `/api/v1/organizations`, … |
| 16 | **Authorization Policies** | Permission policy, matrix, scoped evaluation | `/api/v1/permission-policies`, `/api/v1/authorization-matrix`, `/api/v1/authorization-checks` |
| 17 | **Diagnostics** | Test pipeline/exception (dev) | `/api/v1/diagnostics` |
| — | **Health probes** | Liveness/readiness (không qua controller) | `/health/*` |
| — | **Hangfire Dashboard** | UI quản lý job (Basic Auth) | `/hangfire` |

**Infrastructure (không phải business module):** BuildingBlocks (Result, cache, middleware), Cache (Memory/Redis).

---

## 1. Identity / Auth

**Module:** `Identity` · **Doc:** [AUTHENTICATION.md](./AUTHENTICATION.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| POST | `/api/v1/auth/login` | Anonymous | — | Đăng nhập, nhận access + refresh token |
| POST | `/api/v1/auth/refresh-token` | Anonymous | — | Làm mới token (rotation) |
| POST | `/api/v1/auth/logout` | Anonymous | — | Thu hồi refresh token |
| GET | `/api/v1/auth/me` | Bearer | — | Thông tin user hiện tại (roles, permissions) |

---

## 2. Account (Self-service)

**Module:** `Identity` · **Doc:** [ACCOUNT.md](./ACCOUNT.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/account/profile` | Bearer | — | Xem profile cá nhân |
| PUT | `/api/v1/account/profile` | Bearer | — | Cập nhật email, full name |
| POST | `/api/v1/account/change-password` | Bearer | — | Đổi mật khẩu (cần mật khẩu hiện tại) |
| POST | `/api/v1/account/forgot-password` | Anonymous | — | Yêu cầu gửi email reset mật khẩu |
| POST | `/api/v1/account/reset-password` | Anonymous | — | Đặt mật khẩu mới bằng token từ email |

---

## 3. Users (Admin)

**Module:** `Users` · **Doc:** [AUTHORIZATION.md](./AUTHORIZATION.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/users` | Bearer | `Users.View` | Danh sách user (phân trang, filter) |
| GET | `/api/v1/users/{id}` | Bearer | `Users.View` | Chi tiết user |
| POST | `/api/v1/users` | Bearer | `Users.Create` | Tạo user mới |
| PUT | `/api/v1/users/{id}` | Bearer | `Users.Update` | Cập nhật email, full name |
| POST | `/api/v1/users/{id}/activate` | Bearer | `Users.Activate` | Kích hoạt user |
| POST | `/api/v1/users/{id}/deactivate` | Bearer | `Users.Deactivate` | Vô hiệu hóa user |
| POST | `/api/v1/users/{id}/roles` | Bearer | `Users.AssignRole` | Gán roles |
| DELETE | `/api/v1/users/{id}/roles/{roleId}` | Bearer | `Users.AssignRole` | Gỡ role |
| POST | `/api/v1/users/{id}/permissions` | Bearer | `Users.AssignPermission` | Gán permission trực tiếp |
| DELETE | `/api/v1/users/{id}/permissions/{permissionId}` | Bearer | `Users.AssignPermission` | Gỡ permission trực tiếp |

---

## 4. Categories

**Module:** `Categories` · **Doc:** [MODULES.md](./MODULES.md#categories)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/categories` | Bearer | `Category.View` | Danh sách danh mục |
| GET | `/api/v1/categories/{id}` | Bearer | `Category.View` | Chi tiết danh mục |
| POST | `/api/v1/categories` | Bearer | `Category.Create` | Tạo danh mục |
| PUT | `/api/v1/categories/{id}` | Bearer | `Category.Update` | Cập nhật danh mục |
| DELETE | `/api/v1/categories/{id}` | Bearer | `Category.Delete` | Xóa mềm danh mục |
| PATCH | `/api/v1/categories/{id}/activate` | Bearer | `Category.Activate` | Kích hoạt |
| PATCH | `/api/v1/categories/{id}/deactivate` | Bearer | `Category.Deactivate` | Vô hiệu hóa |

---

## 5. Files

**Module:** `Files` · **Doc:** [FILES.md](./FILES.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/files` | Bearer | `File.View` | Danh sách file metadata |
| GET | `/api/v1/files/{id}` | Bearer | `File.View` | Chi tiết file |
| POST | `/api/v1/files/upload` | Bearer | `File.Upload` | Upload file (multipart) |
| GET | `/api/v1/files/{id}/download` | Bearer | `File.Download` | Tải file |
| DELETE | `/api/v1/files/{id}` | Bearer | `File.Delete` | Xóa file |
| PATCH | `/api/v1/files/{id}/mark-permanent` | Bearer | `File.MarkPermanent` | Đánh dấu file permanent |

---

## 6. Notifications

**Module:** `Notifications` · **Doc:** [NOTIFICATIONS.md](./NOTIFICATIONS.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/notifications` | Bearer | `Notification.View` | Danh sách thông báo của user |
| GET | `/api/v1/notifications/{id}` | Bearer | `Notification.View` | Chi tiết thông báo |
| POST | `/api/v1/notifications` | Bearer | `Notification.Create` / `Notification.Manage` | Tạo thông báo |
| PATCH | `/api/v1/notifications/{id}/read` | Bearer | `Notification.MarkRead` | Đánh dấu đã đọc |
| PATCH | `/api/v1/notifications/read-all` | Bearer | `Notification.MarkRead` | Đánh dấu tất cả đã đọc |
| PATCH | `/api/v1/notifications/{id}/archive` | Bearer | `Notification.Archive` | Lưu trữ thông báo |

---

## 7. Email Templates

**Module:** `Notifications` · **Doc:** [NOTIFICATIONS.md](./NOTIFICATIONS.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/email-templates` | Bearer | `Email.TemplateView` | Danh sách template |
| GET | `/api/v1/email-templates/{id}` | Bearer | `Email.TemplateView` | Chi tiết template |
| POST | `/api/v1/email-templates` | Bearer | `Email.TemplateCreate` | Tạo template |
| PUT | `/api/v1/email-templates/{id}` | Bearer | `Email.TemplateUpdate` | Cập nhật template |
| DELETE | `/api/v1/email-templates/{id}` | Bearer | `Email.TemplateDelete` | Xóa template |
| PATCH | `/api/v1/email-templates/{id}/activate` | Bearer | `Email.TemplateActivate` | Kích hoạt template |
| PATCH | `/api/v1/email-templates/{id}/deactivate` | Bearer | `Email.TemplateDeactivate` | Vô hiệu hóa template |

---

## 8. Email Messages

**Module:** `Notifications` · **Doc:** [NOTIFICATIONS.md](./NOTIFICATIONS.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/email-messages` | Bearer | `Email.View` | Lịch sử email đã gửi |
| GET | `/api/v1/email-messages/{id}` | Bearer | `Email.View` | Chi tiết email message |
| POST | `/api/v1/email-messages/send-test` | Bearer | `Email.Send` | Gửi email test |
| POST | `/api/v1/email-messages/send` | Bearer | `Email.Send` | Gửi email trực tiếp |
| POST | `/api/v1/email-messages/send-template` | Bearer | `Email.Send` | Gửi email theo template |

---

## 9. Background Jobs

**Module:** `BackgroundJobs` · **Doc:** [BACKGROUND_JOBS.md](./BACKGROUND_JOBS.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/background-jobs/executions` | Bearer | `BackgroundJob.View` | Lịch sử chạy job |
| GET | `/api/v1/background-jobs/executions/{id}` | Bearer | `BackgroundJob.View` | Chi tiết execution |
| POST | `/api/v1/background-jobs/email-retry/run` | Bearer | `BackgroundJob.Run` | Chạy job retry email |
| POST | `/api/v1/background-jobs/temporary-files-cleanup/run` | Bearer | `BackgroundJob.Run` | Chạy job dọn file tạm |
| POST | `/api/v1/background-jobs/log-cleanup/run` | Bearer | `BackgroundJob.Run` | Chạy job dọn log |

**Hangfire Dashboard:** `GET /hangfire` — Basic Auth (`BackgroundJob.Dashboard` permission hoặc cấu hình Basic Auth)

---

## 10. Audit Logs

**Module:** `AuditLogs` · **Doc:** [MODULES.md](./MODULES.md#audit-logs)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/audit-logs` | Bearer | `AuditLog.View` hoặc `AuditLogs.View` | Danh sách audit log |
| GET | `/api/audit-logs/{id}` | Bearer | `AuditLog.View` hoặc `AuditLogs.View` | Chi tiết audit log |

> **Lưu ý:** Route chưa version (`/api/audit-logs`), khác với các module khác (`/api/v1/...`).

---

## 11. Activity Logs

**Module:** `AuditLogs` · **Doc:** [MODULES.md](./MODULES.md#activity-logs)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/activity-logs` | Bearer | `ActivityLog.View` | Danh sách activity log |
| GET | `/api/activity-logs/{id}` | Bearer | `ActivityLog.View` | Chi tiết activity log |

---

## 12. Monitoring

**Module:** `Monitoring` · **Doc:** [MONITORING.md](./MONITORING.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/monitoring/health/details` | Bearer | `Monitoring.HealthView` | Chi tiết health checks |
| GET | `/api/v1/monitoring/system-info` | Bearer | `Monitoring.SystemInfoView` | Thông tin hệ thống |

**Health probes (public, không cần JWT):**

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness probe (DB, Redis, Hangfire…) |
| GET | `/health` | Combined health |

---

## 13. Settings

**Module:** `Settings` · **Doc:** [SETTINGS.md](./SETTINGS.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/settings` | Bearer | `Setting.View` | Danh sách setting (filter, phân trang) |
| GET | `/api/v1/settings/{id}` | Bearer | `Setting.View` | Chi tiết setting theo ID |
| GET | `/api/v1/settings/key/{key}` | Bearer | `Setting.View` | Chi tiết setting theo key |
| GET | `/api/v1/settings/groups/{group}` | Bearer | `Setting.View` | Danh sách setting theo group |
| POST | `/api/v1/settings` | Bearer | `Setting.Create` | Tạo setting mới |
| PUT | `/api/v1/settings/{id}` | Bearer | `Setting.Update` | Cập nhật metadata setting |
| PATCH | `/api/v1/settings/{id}/value` | Bearer | `Setting.Update` | Cập nhật giá trị setting |
| DELETE | `/api/v1/settings/{id}` | Bearer | `Setting.Delete` | Xóa setting (không xóa system setting) |
| PATCH | `/api/v1/settings/{id}/activate` | Bearer | `Setting.Activate` | Kích hoạt setting |
| PATCH | `/api/v1/settings/{id}/deactivate` | Bearer | `Setting.Deactivate` | Vô hiệu hóa setting |

**Sensitive values:** cần thêm `Setting.ViewSensitive` để xem giá trị nhạy cảm (không mask).

---

## 14. Access Policy

**Module:** `Settings` · **Doc:** [ACCESS_POLICY.md](./ACCESS_POLICY.md)

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/access-policy/password` | Bearer | `AccessPolicy.View` | Xem chính sách mật khẩu |
| PUT | `/api/v1/access-policy/password` | Bearer | `AccessPolicy.Update` | Cập nhật chính sách mật khẩu |
| GET | `/api/v1/access-policy/login` | Bearer | `AccessPolicy.View` | Xem chính sách đăng nhập |
| PUT | `/api/v1/access-policy/login` | Bearer | `AccessPolicy.Update` | Cập nhật chính sách đăng nhập |
| GET | `/api/v1/access-policy/session` | Bearer | `AccessPolicy.View` | Xem chính sách session/token |
| PUT | `/api/v1/access-policy/session` | Bearer | `AccessPolicy.Update` | Cập nhật chính sách session/token |
| GET | `/api/v1/access-policy/maintenance` | Bearer | `Maintenance.View` | Xem chính sách bảo trì |
| PUT | `/api/v1/access-policy/maintenance` | Bearer | `Maintenance.Update` | Cập nhật chính sách bảo trì |

---

## 15. Organizations

**Module:** `Organizations` · **Doc:** [ORGANIZATIONS.md](./ORGANIZATIONS.md)

### Tenants

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/tenants` | Bearer | `Tenant.View` | Danh sách tenant |
| GET | `/api/v1/tenants/{id}` | Bearer | `Tenant.View` | Chi tiết tenant |
| POST | `/api/v1/tenants` | Bearer | `Tenant.Manage` | Tạo tenant |
| PUT | `/api/v1/tenants/{id}` | Bearer | `Tenant.Manage` | Cập nhật tenant |
| DELETE | `/api/v1/tenants/{id}` | Bearer | `Tenant.Manage` | Xóa mềm tenant |
| PATCH | `/api/v1/tenants/{id}/activate` | Bearer | `Tenant.Manage` | Kích hoạt tenant |
| PATCH | `/api/v1/tenants/{id}/deactivate` | Bearer | `Tenant.Manage` | Vô hiệu hóa tenant |

### Organizations

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/organizations` | Bearer | `Organization.View` | Danh sách organization |
| GET | `/api/v1/organizations/tree?tenantId=` | Bearer | `Organization.View` | Cây organization theo tenant |
| GET | `/api/v1/organizations/{id}` | Bearer | `Organization.View` | Chi tiết organization |
| POST | `/api/v1/organizations` | Bearer | `Organization.Manage` | Tạo organization |
| PUT | `/api/v1/organizations/{id}` | Bearer | `Organization.Manage` | Cập nhật organization |
| DELETE | `/api/v1/organizations/{id}` | Bearer | `Organization.Manage` | Xóa mềm organization |
| PATCH | `/api/v1/organizations/{id}/activate` | Bearer | `Organization.Manage` | Kích hoạt |
| PATCH | `/api/v1/organizations/{id}/deactivate` | Bearer | `Organization.Manage` | Vô hiệu hóa |

### Organization users

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/organization-users` | Bearer | `Organization.View` | Danh sách membership |
| GET | `/api/v1/organization-users/{id}` | Bearer | `Organization.View` | Chi tiết membership |
| GET | `/api/v1/organization-users/users/{userId}/organizations` | Bearer | `Organization.View` | Organizations của user |
| POST | `/api/v1/organization-users` | Bearer | `Organization.Manage` | Gán user vào organization |
| PATCH | `/api/v1/organization-users/{id}/set-default` | Bearer | `Organization.Manage` | Đặt organization mặc định |
| PATCH | `/api/v1/organization-users/{id}/activate` | Bearer | `Organization.Manage` | Kích hoạt membership |
| PATCH | `/api/v1/organization-users/{id}/deactivate` | Bearer | `Organization.Manage` | Vô hiệu hóa membership |
| DELETE | `/api/v1/organization-users/{id}` | Bearer | `Organization.Manage` | Xóa membership |

### Workspaces

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/workspaces` | Bearer | `Workspace.View` | Danh sách workspace |
| GET | `/api/v1/workspaces/{id}` | Bearer | `Workspace.View` | Chi tiết workspace |
| POST | `/api/v1/workspaces` | Bearer | `Workspace.Manage` | Tạo workspace |
| PUT | `/api/v1/workspaces/{id}` | Bearer | `Workspace.Manage` | Cập nhật workspace |
| DELETE | `/api/v1/workspaces/{id}` | Bearer | `Workspace.Manage` | Xóa mềm workspace |
| PATCH | `/api/v1/workspaces/{id}/activate` | Bearer | `Workspace.Manage` | Kích hoạt |
| PATCH | `/api/v1/workspaces/{id}/deactivate` | Bearer | `Workspace.Manage` | Vô hiệu hóa |

### Workspace users

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/workspace-users` | Bearer | `Workspace.View` | Danh sách membership |
| GET | `/api/v1/workspace-users/{id}` | Bearer | `Workspace.View` | Chi tiết membership |
| POST | `/api/v1/workspace-users` | Bearer | `Workspace.Manage` | Gán user vào workspace |
| PATCH | `/api/v1/workspace-users/{id}/activate` | Bearer | `Workspace.Manage` | Kích hoạt membership |
| PATCH | `/api/v1/workspace-users/{id}/deactivate` | Bearer | `Workspace.Manage` | Vô hiệu hóa membership |
| DELETE | `/api/v1/workspace-users/{id}` | Bearer | `Workspace.Manage` | Xóa membership |

---

## 16. Authorization Policies

**Module:** `AuthorizationPolicies` · **Doc:** [AUTHORIZATION_POLICIES.md](./AUTHORIZATION_POLICIES.md)

Scoped authorization layer — **bổ sung** (không thay thế) JWT `[HasPermission]`. Chi tiết evaluation order: [AUTHORIZATION_POLICIES.md](./AUTHORIZATION_POLICIES.md#evaluation-order-phase-241).

### Permission policies

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/permission-policies` | Bearer | `PermissionPolicy.View` | Danh sách permission policy |
| GET | `/api/v1/permission-policies/{id}` | Bearer | `PermissionPolicy.View` | Chi tiết policy |
| POST | `/api/v1/permission-policies` | Bearer | `PermissionPolicy.Manage` | Tạo policy |
| PUT | `/api/v1/permission-policies/{id}` | Bearer | `PermissionPolicy.Manage` | Cập nhật policy |
| DELETE | `/api/v1/permission-policies/{id}` | Bearer | `PermissionPolicy.Manage` | Xóa mềm policy |
| PATCH | `/api/v1/permission-policies/{id}/activate` | Bearer | `PermissionPolicy.Manage` | Kích hoạt |
| PATCH | `/api/v1/permission-policies/{id}/deactivate` | Bearer | `PermissionPolicy.Manage` | Vô hiệu hóa |

### Role permission policy assignments

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/permission-policies/role-assignments` | Bearer | `PermissionPolicy.View` | Danh sách gán role-policy |
| POST | `/api/v1/permission-policies/role-assignments` | Bearer | `PermissionPolicy.Manage` | Gán policy cho role |
| DELETE | `/api/v1/permission-policies/role-assignments/{id}` | Bearer | `PermissionPolicy.Manage` | Xóa gán |
| PATCH | `/api/v1/permission-policies/role-assignments/{id}/activate` | Bearer | `PermissionPolicy.Manage` | Kích hoạt |
| PATCH | `/api/v1/permission-policies/role-assignments/{id}/deactivate` | Bearer | `PermissionPolicy.Manage` | Vô hiệu hóa |

### User permission policy overrides

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/permission-policies/user-overrides` | Bearer | `PermissionPolicy.View` | Danh sách override user |
| POST | `/api/v1/permission-policies/user-overrides` | Bearer | `PermissionPolicy.Manage` | Tạo override |
| DELETE | `/api/v1/permission-policies/user-overrides/{id}` | Bearer | `PermissionPolicy.Manage` | Xóa override |
| PATCH | `/api/v1/permission-policies/user-overrides/{id}/activate` | Bearer | `PermissionPolicy.Manage` | Kích hoạt |
| PATCH | `/api/v1/permission-policies/user-overrides/{id}/deactivate` | Bearer | `PermissionPolicy.Manage` | Vô hiệu hóa |

### Authorization matrix

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| GET | `/api/v1/authorization-matrix` | Bearer | `AuthorizationMatrix.View` | Danh sách matrix entry |
| GET | `/api/v1/authorization-matrix/{id}` | Bearer | `AuthorizationMatrix.View` | Chi tiết entry |
| POST | `/api/v1/authorization-matrix` | Bearer | `AuthorizationMatrix.Manage` | Tạo entry |
| PUT | `/api/v1/authorization-matrix/{id}` | Bearer | `AuthorizationMatrix.Manage` | Cập nhật entry |
| DELETE | `/api/v1/authorization-matrix/{id}` | Bearer | `AuthorizationMatrix.Manage` | Xóa mềm entry |
| PATCH | `/api/v1/authorization-matrix/{id}/enable` | Bearer | `AuthorizationMatrix.Manage` | Bật entry |
| PATCH | `/api/v1/authorization-matrix/{id}/disable` | Bearer | `AuthorizationMatrix.Manage` | Tắt entry |

### Authorization checks

| Method | Route | Auth | Permission | Mô tả |
|--------|-------|------|------------|-------|
| POST | `/api/v1/authorization-checks/evaluate` | Bearer | `AuthorizationCheck.Execute` | Đánh giá Allow/Deny |
| POST | `/api/v1/authorization-checks/explain` | Bearer | `AuthorizationCheck.Explain` | Giải thích quyết định (admin/dev) |

**Request body (evaluate / explain):**

```json
{
  "userId": "f6b94c17-6e42-418f-8bcb-931af3047fa3",
  "permissionCode": "Tenant.View",
  "action": null,
  "resourceType": null,
  "moduleCode": null,
  "resourceContext": {}
}
```

Hoặc dùng matrix lookup (scope lấy từ matrix entry):

```json
{
  "userId": "f6b94c17-6e42-418f-8bcb-931af3047fa3",
  "permissionCode": null,
  "action": "View",
  "resourceType": "Tenant",
  "moduleCode": "Organizations",
  "resourceContext": {
    "tenantId": "33333333-3333-3333-3333-333333333333"
  }
}
```

**`resourceContext` fields (optional theo scope):**

| Field | Dùng khi scope |
|-------|----------------|
| `tenantId` | Tenant |
| `organizationId` | Organization |
| `workspaceId` | Workspace |
| `ownerUserId`, `createdBy` | OwnerOnly, Self |
| `assignedUserIds` | AssignedOnly |
| `resourceId` | Self |

Scope thiếu context → `400` với code `AuthorizationCheck.Missing*` (xem [ERROR_CODES.md](./ERROR_CODES.md)).

**Response (evaluate):** `AuthorizationDecisionResponse` — `isAllowed`, `effect`, `reason`, `matchedPermissionCode`, `matchedPolicyCode`, `scope`, …

---

## 17. Diagnostics (Dev/Test)

**Module:** `ApiHost` · Chỉ dùng kiểm thử pipeline/exception handling.

| Method | Route | Auth | Mô tả |
|--------|-------|------|-------|
| POST | `/api/v1/diagnostics/pipeline` | Bearer | Test POST pipeline |
| GET | `/api/v1/diagnostics/pipeline-query` | Bearer | Test GET pipeline |
| GET | `/api/v1/diagnostics/throw` | Bearer | Trigger exception (500) |
| GET | `/api/v1/diagnostics/not-found` | Bearer | Trigger not found (404) |
| GET | `/api/v1/diagnostics/bad-request` | Bearer | Trigger bad request (400) |

---

## Thống kê nhanh

| Hạng mục | Số lượng |
|----------|----------|
| Business modules | 16 |
| API controllers | 29 |
| REST endpoints (ước tính) | ~170 |
| Health / infra endpoints | 4 |

---

## Tài liệu liên quan

| Doc | Nội dung |
|-----|----------|
| [API_CONVENTIONS.md](./API_CONVENTIONS.md) | Chuẩn response, status code, pagination |
| [AUTHORIZATION.md](./AUTHORIZATION.md) | Toàn bộ permissions |
| [AUTHORIZATION_POLICIES.md](./AUTHORIZATION_POLICIES.md) | Scoped policy, matrix, evaluator |
| [MODULES.md](./MODULES.md) | Tổng quan module + entities |
| [ERROR_CODES.md](./ERROR_CODES.md) | Mã lỗi API |
| [Swagger](http://localhost:5080/swagger) | Interactive API (Dev/Docker) |
