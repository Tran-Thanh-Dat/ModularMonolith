# Notifications Module

Email sending, email templates, email message history, and in-app notifications.

## Structure

- `Notifications.Domain` — entities and constants
- `Notifications.Application` — CQRS, validators, abstractions
- `Notifications.Infrastructure` — EF Core, SMTP sender, services
- `Notifications.Api` — controllers and API contracts

## Notification Security Model

### Self-service permissions

| Permission | Scope |
|------------|-------|
| `Notification.View` | List and view **own** notifications only |
| `Notification.MarkRead` | Mark **own** notifications as read (including read-all for self) |
| `Notification.Archive` | Archive **own** notifications |

### Admin permissions

| Permission | Scope |
|------------|-------|
| `Notification.ViewAll` | List/view notifications for **any user** (pass `?userId=` on GET) |
| `Notification.Manage` | Mark read / archive notifications for **any user** |
| `Notification.Create` | Create notifications for any user (privileged; Admin/SuperAdmin only) |

### Default list behavior

- `GET /api/v1/notifications` without `userId` returns the **current user's** notifications.
- Admins with `Notification.ViewAll` may pass `?userId={guid}` to query another user.
- Non-admins cannot list another user's notifications (403 `Notification.ViewAllRequired`).
- Cross-user access by notification id requires `Notification.ViewAll` (view) or `Notification.Manage` (mutate).

Authorization is enforced in `INotificationAuthorizationService` in addition to controller `[HasPermission]` attributes.

## SMTP Configuration

Configure in `appsettings.json`:

```json
"Email": {
  "Provider": "Smtp",
  "FromName": "Modular API",
  "FromAddress": "noreply@example.com",
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "UserName": "",
    "Password": "",
    "EnableSsl": true,
    "UseDefaultCredentials": false
  },
  "TestMode": {
    "Enabled": false,
    "RedirectTo": ""
  }
}
```

- Do not commit real SMTP credentials.
- Passwords are never logged or returned in API responses.
- **Production:** explicitly set `TestMode.Enabled = false`.
- **Development:** override in `appsettings.Development.json` with `Enabled = true` and a safe `RedirectTo` mailbox.

## TestMode Behavior

When `TestMode.Enabled = true` and `RedirectTo` is set:

- All outgoing emails are sent to `RedirectTo` instead of real recipients.
- Original `To` / `Cc` / `Bcc` are preserved in the message body for debugging.
- Email history still records the intended recipients.

When `TestMode.Enabled = true` but `RedirectTo` is **empty**:

- Send is **blocked** (no SMTP attempt).
- `EmailMessage` is persisted with `Failed` status.
- API returns a controlled failure (`Email.ProviderNotConfigured` semantics).
- This prevents accidental delivery to real recipients.

**Code default:** `TestMode.Enabled` defaults to `false` when not configured.

## Template Placeholder Syntax

Simple safe string replacement only (no Razor/Liquid/script execution):

```
Hello {{UserName}},

Click here to reset your password: {{ResetLink}}

Regards,
{{AppName}}
```

When `EmailTemplate.IsHtml = true`, placeholder **values** are HTML-encoded before insertion (e.g. `<script>` becomes encoded text).

Pass values via `templateData` when calling `POST /api/v1/email-messages/send-template`.

## Direct Send vs Pending/Retry

**Direct send (this phase):**

1. Create `EmailMessage` row with status `Pending`
2. Attempt SMTP send immediately
3. Update status to `Sent` or `Failed`
4. Store `SentAt` or `ErrorMessage`

**Email send failure semantics:**

- Failed sends are **persisted** as `EmailMessage` records with status `Failed`.
- The API may return HTTP 502 (`Email.SendFailed`) while the history row is still committed.
- This is intentional: failed attempts are auditable and prepared for future retry.
- Raw SMTP details are never exposed to clients; failures are logged server-side only.

**Prepared for background retry (not implemented yet):**

- `RetryCount`, `NextRetryAt`, and `Pending` status are stored on `EmailMessage`
- Background Job phase (Hangfire) will process pending/failed emails

## API Overview

### Email Messages (`/api/v1/email-messages`)

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/` | Email.View |
| GET | `/{id}` | Email.View |
| POST | `/send-test` | Email.Send |
| POST | `/send` | Email.Send |
| POST | `/send-template` | Email.Send |

### Email Templates (`/api/v1/email-templates`)

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/` | Email.TemplateView |
| GET | `/{id}` | Email.TemplateView |
| POST | `/` | Email.TemplateCreate |
| PUT | `/{id}` | Email.TemplateUpdate |
| DELETE | `/{id}` | Email.TemplateDelete |
| PATCH | `/{id}/activate` | Email.TemplateActivate |
| PATCH | `/{id}/deactivate` | Email.TemplateDeactivate |

### Notifications (`/api/v1/notifications`)

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/` | Notification.View (+ ViewAll for other users) |
| GET | `/{id}` | Notification.View (+ ViewAll for other users) |
| POST | `/` | Notification.Create |
| PATCH | `/{id}/read` | Notification.MarkRead (+ Manage for other users) |
| PATCH | `/read-all` | Notification.MarkRead (current user only) |
| PATCH | `/{id}/archive` | Notification.Archive (+ Manage for other users) |

## Unit of Work Convention

- Module services inject `NotificationsUnitOfWork` (typed), not plain `IUnitOfWork`.
- `NotificationsUnitOfWork` is also registered as `IUnitOfWork` for `TransactionBehavior`.
- Handlers do not call `SaveChanges` manually; `TransactionBehavior` owns commits for commands.

## Activity Log Convention

Post-commit success logs (via `EnqueuePostCommitAsync`, module `Notifications`):

- Email sent
- Email template created / updated / deleted / activated / deactivated
- Notification created / marked read / archived

Immediate logs:

- Email send failed or blocked (`SystemError`)

Failed transactions do not emit false success activity logs (pending queue cleared on rollback).

## Audit Log Convention

`EmailMessage`, `EmailTemplate`, and `Notification` changes are captured automatically via the audit interceptor:

- Create → `NewValues`
- Update → `ChangedColumns` (including mark-read and archive)
- Soft delete → `Delete` action

## Migration

```bash
dotnet ef migrations add AddNotificationsModule --project src/Modules/Notifications/Notifications.Infrastructure --startup-project src/ApiHost --context NotificationsDbContext

dotnet ef database update --project src/Modules/Notifications/Notifications.Infrastructure --startup-project src/ApiHost --context NotificationsDbContext
```

## Smoke Test Checklist

### Notification security

- [ ] User A receives a notification
- [ ] User B cannot list User A notifications (`GET ?userId=`)
- [ ] User B cannot read User A notification by id (403 ViewAllRequired)
- [ ] User B cannot mark User A notification as read (403 ManageRequired)
- [ ] User B cannot archive User A notification (403 ManageRequired)
- [ ] Admin with `Notification.ViewAll` can query User A notifications
- [ ] Admin with `Notification.Manage` can mark/archive User A notifications
- [ ] `PATCH /read-all` only affects current user's notifications

### Email safety

- [ ] TestMode disabled by default in base `appsettings.json`
- [ ] Development TestMode redirects to configured address
- [ ] TestMode enabled without RedirectTo blocks send (no real delivery)
- [ ] HTML template value `<script>alert(1)</script>` is encoded in output
- [ ] SMTP password is not logged or exposed in API responses
- [ ] Failed email send persists history and returns controlled 502

### General

- [ ] Login and refresh token work
- [ ] Categories and Files APIs still work
- [ ] Email templates CRUD works
- [ ] AuditLog and ActivityLog entries appear for entity changes

## Deferred

- Hangfire / background job retry worker
- SignalR real-time push
- SMS / mobile push notifications
