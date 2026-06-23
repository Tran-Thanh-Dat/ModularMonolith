# Authentication

JWT Bearer authentication with refresh token rotation. Identity module owns auth endpoints.

## Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/v1/auth/login` | Anonymous | Issue access + refresh tokens |
| POST | `/api/v1/auth/refresh-token` | Anonymous | Rotate refresh token, new access token |
| POST | `/api/v1/auth/logout` | Anonymous | Revoke refresh token |
| GET | `/api/v1/auth/me` | Bearer | Current user profile, roles, permissions |

Account self-service (profile, change/forgot/reset password): see [ACCOUNT.md](./ACCOUNT.md).

## Login

```bash
curl -X POST http://localhost:5080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "userNameOrEmail": "admin",
    "password": "YOUR_DEV_PASSWORD",
    "rememberMe": false
  }'
```

Response (`200`):

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJ...",
    "accessTokenExpiresAt": "2026-06-19T12:30:00Z",
    "refreshToken": "base64...",
    "refreshTokenExpiresAt": "2026-06-26T12:00:00Z"
  },
  "traceId": "..."
}
```

## Refresh token

```bash
curl -X POST http://localhost:5080/api/v1/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{ "refreshToken": "YOUR_REFRESH_TOKEN" }'
```

Returns new access + refresh tokens. The previous refresh token is **revoked** (rotation).

## Logout

```bash
curl -X POST http://localhost:5080/api/v1/auth/logout \
  -H "Content-Type: application/json" \
  -d '{ "refreshToken": "YOUR_REFRESH_TOKEN" }'
```

## Current user

```bash
curl http://localhost:5080/api/v1/auth/me \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

## JWT Bearer usage

Send on every protected request:

```
Authorization: Bearer {accessToken}
```

Swagger (Dev/Docker): click **Authorize**, enter `Bearer {token}` or just the token depending on UI version.

## Token lifetimes (configurable)

| Setting | Default | Config key |
|---------|---------|------------|
| Access token | 30 minutes | `Jwt:AccessTokenExpirationMinutes` |
| Refresh token | 7 days | `RefreshToken:ExpirationDays` |

## Refresh token security

- Stored **hashed** in database (HMAC with `RefreshToken:Secret`).
- **Rotation:** each refresh revokes the old token and issues a new one.
- **Reuse detection:** if a revoked refresh token is presented again, all active refresh tokens for that user are revoked and a security activity log is written. Client receives generic `Auth.RefreshTokenInvalid` (no reuse details).
- **Inactive/deactivated users:** cannot log in or refresh; `FindActive*` repository methods exclude them. Login returns **`Auth.InvalidCredentials`** (same as wrong password) — not `Auth.UserInactive`. This avoids revealing whether an account exists but is deactivated (account state enumeration).
- **Refresh for inactive users:** returns **`Auth.RefreshTokenInvalid`** (generic client-safe failure).

## Client storage recommendations

| Token | Recommendation |
|-------|----------------|
| Access token | Memory (SPA) or secure short-lived storage; never localStorage if XSS risk is high |
| Refresh token | HttpOnly secure cookie (if same-site) or secure server-side session; avoid localStorage |

Always use HTTPS in non-local environments.

## Configuration (no real secrets in repo)

```json
"Jwt": {
  "Issuer": "ModularApi",
  "Audience": "ModularApiClient",
  "Secret": "CHANGE_ME_TO_A_LONG_SECURE_SECRET_KEY_32_CHARS_MIN",
  "AccessTokenExpirationMinutes": 30
},
"RefreshToken": {
  "Secret": "CHANGE_ME_TO_A_LONG_REFRESH_TOKEN_SECRET_32_CHARS_MIN",
  "ExpirationDays": 7
}
```

Production secrets must come from environment variables — see [../SECRETS.md](../SECRETS.md) and [../SECURITY.md](../SECURITY.md).

## Permission changes

JWT embeds permission claims at login/refresh time. After admin changes roles/permissions, users must **log in again** or **refresh token** to pick up new permissions (permission cache is updated on refresh).

See [AUTHORIZATION.md](./AUTHORIZATION.md).
