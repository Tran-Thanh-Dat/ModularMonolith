# Notifications & Email

In-app notifications, SMTP email delivery, and template management.

## Entities

| Entity | Purpose |
|--------|---------|
| `Notification` | In-app message for a user (read/archive state) |
| `EmailMessage` | Outbound email queue/history |
| `EmailTemplate` | Reusable HTML/text templates with placeholders |

## API routes

### Notifications — `/api/v1/notifications`

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/` | `Notification.View` |
| GET | `/{id}` | `Notification.View` |
| POST | `/` | `Notification.Create` |
| PATCH | `/{id}/read` | `Notification.MarkRead` |
| PATCH | `/read-all` | `Notification.MarkRead` |
| PATCH | `/{id}/archive` | `Notification.Archive` |

### Email templates — `/api/v1/email-templates`

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/` | `Email.TemplateView` |
| GET | `/{id}` | `Email.TemplateView` |
| POST | `/` | `Email.TemplateCreate` |
| PUT | `/{id}` | `Email.TemplateUpdate` |
| DELETE | `/{id}` | `Email.TemplateDelete` |
| PATCH | `/{id}/activate` | `Email.TemplateActivate` |
| PATCH | `/{id}/deactivate` | `Email.TemplateDeactivate` |

### Email messages — `/api/v1/email-messages`

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/` | `Email.View` |
| GET | `/{id}` | `Email.View` |
| POST | `/send-test` | `Email.Send` |
| POST | `/send` | `Email.Send` |
| POST | `/send-template` | `Email.Send` |

## SMTP configuration

Config section: `Email`

```json
{
  "Email": {
    "Provider": "Smtp",
    "FromName": "Modular API",
    "FromAddress": "noreply@example.com",
    "Smtp": {
      "Host": "localhost",
      "Port": 587,
      "EnableSsl": true
    },
    "TestMode": {
      "Enabled": false,
      "RedirectTo": ""
    }
  }
}
```

Local Docker stack uses **Mailpit** — UI at http://localhost:8025.

## TestMode

When `Email:TestMode:Enabled=true`, all outbound mail redirects to `RedirectTo` instead of the original recipient. Use in Development/Docker; disable in Production.

## Template placeholders

Templates support `{{PlaceholderName}}` substitution. Values are HTML-encoded when rendered to prevent injection.

Templates are cached by code (`v1:email-templates:code:{code}`) and invalidated on template mutations.

## Email send failure behavior

1. Send attempt recorded on `EmailMessage` with status/history.
2. Failures are retried by recurring job `background-jobs:email-retry` (default every 5 minutes).
3. `MaxRetryCount` (default 5) stops further automatic retries.

SMTP is **not** invoked inside DB transactions — failures do not roll back notification creation.

## Notification ownership

| Permission | Access |
|------------|--------|
| `Notification.View` | Own notifications only |
| `Notification.ViewAll` | Read any user's notifications |
| `Notification.Manage` | Full cross-user management |
| `Notification.Create` | Create notifications (may target other users) |

Cross-user read without `ViewAll`/`Manage` returns **403** (integration test skipped until PostgreSQL suite — see [TESTING_GUIDE.md](./TESTING_GUIDE.md)).

## Unread count cache

Key: `v1:notifications:unread-count:{userId}` (TTL ~15 minutes).

Invalidated when notifications are marked read/archived or created for the user.

## Example APIs

```bash
# List my notifications
curl "http://localhost:5080/api/v1/notifications?pageIndex=1&pageSize=20&isRead=false" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Mark read
curl -X PATCH "http://localhost:5080/api/v1/notifications/{id}/read" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

See [AUTHORIZATION.md](./AUTHORIZATION.md) and [BACKGROUND_JOBS.md](./BACKGROUND_JOBS.md).
