# Account Self-Service

Account endpoints for the logged-in user and anonymous password recovery. Implemented in the **Identity** module (`/api/v1/account/*`).

## Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/v1/account/profile` | Bearer | Current user's profile |
| PUT | `/api/v1/account/profile` | Bearer | Update email and full name |
| POST | `/api/v1/account/change-password` | Bearer | Change password (requires current password) |
| POST | `/api/v1/account/forgot-password` | Anonymous | Request password reset email |
| POST | `/api/v1/account/reset-password` | Anonymous | Set new password using reset token |

## Profile

```bash
curl http://localhost:5080/api/v1/account/profile \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

Update profile:

```bash
curl -X PUT http://localhost:5080/api/v1/account/profile \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "fullName": "Updated Name"
  }'
```

- User name is **read-only** for self-service (admin changes via `/api/v1/users`).
- Email uniqueness is enforced (`User.EmailAlreadyExists` on conflict).

## Change password

```bash
curl -X POST http://localhost:5080/api/v1/account/change-password \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "OldPass@123",
    "newPassword": "NewPass@456"
  }'
```

- Returns `Account.CurrentPasswordInvalid` if the current password is wrong.
- On success, **all active refresh tokens** for the user are revoked (force re-login on other devices).
- Activity log: `ChangePassword`.

Password rules (same as user creation): min 8 chars, upper, lower, digit, special character. New password must differ from current password.

## Forgot / reset password

### 1. Request reset

```bash
curl -X POST http://localhost:5080/api/v1/account/forgot-password \
  -H "Content-Type: application/json" \
  -d '{ "email": "user@example.com" }'
```

Always returns success with a generic message (no email enumeration):

```json
{
  "success": true,
  "data": {
    "message": "If an account exists with that email, a password reset link has been sent."
  }
}
```

- Only **active** users receive email.
- Previous unused reset tokens for the user are invalidated.
- Email is sent via the Notifications module (`IEmailService`).
- Rate limited (same bucket as other auth endpoints).

### 2. Reset password

User opens the link from email: `{FrontendResetUrl}?token={token}`

```bash
curl -X POST http://localhost:5080/api/v1/account/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "token": "TOKEN_FROM_EMAIL",
    "newPassword": "NewPass@456"
  }'
```

- Returns `Account.PasswordResetTokenInvalid` for invalid, expired, or already-used tokens.
- On success: password updated, reset token marked used, all refresh tokens revoked.
- Activity log: `ChangePassword`.

## Configuration

```json
"Account": {
  "PasswordReset": {
    "ExpirationMinutes": 60,
    "FrontendResetUrl": "http://localhost:3000/reset-password"
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `ExpirationMinutes` | 60 | Reset token lifetime |
| `FrontendResetUrl` | `http://localhost:3000/reset-password` | Base URL for reset link in email |

Reset tokens are stored **hashed** (HMAC, same mechanism as refresh tokens).

## Permissions

No extra permissions — profile and change-password require authentication only. Forgot/reset are anonymous.

Admin user management remains under `/api/v1/users` with `Users.*` permissions.

## Related docs

- [AUTHENTICATION.md](./AUTHENTICATION.md) — login, JWT, refresh tokens
- [NOTIFICATIONS.md](./NOTIFICATIONS.md) — SMTP / email setup
- [ERROR_CODES.md](./ERROR_CODES.md) — `Account.*` error codes
