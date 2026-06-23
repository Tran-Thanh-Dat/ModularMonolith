# Access Policy

Strongly typed security policies built on top of system settings.

## Policies

### Password

- Minimum length, complexity flags, optional expiration, reuse count
- Validated by `IPasswordPolicyValidator` (Identity change/reset password, Users create user)

### Login

- Max failed attempts, lockout duration, enable lockout, require confirmed email

### Session

- Access token minutes, refresh token days, optional session timeout, refresh-token reuse detection
- **Auth integration:** `JwtTokenService`, login, and refresh handlers read session policy with `Jwt` / `RefreshToken` appsettings fallback

### Maintenance

- Enabled flag, message, optional start/end window, admin bypass
- **Middleware:** `MaintenanceModeMiddleware` returns HTTP 503 when active; allows `/health/*` and Admin/SuperAdmin when bypass enabled

## API Routes

Base: `/api/v1/access-policy`

| Method | Route | Permission |
|--------|-------|------------|
| GET/PUT | `/password` | `AccessPolicy.View` / `AccessPolicy.Update` |
| GET/PUT | `/login` | `AccessPolicy.View` / `AccessPolicy.Update` |
| GET/PUT | `/session` | `AccessPolicy.View` / `AccessPolicy.Update` |
| GET/PUT | `/maintenance` | `Maintenance.View` / `Maintenance.Update` |

## Defaults (seeded)

| Setting | Default |
|---------|---------|
| Password min length | 8 |
| Require upper/lower/digit | true |
| Require special | false |
| Login max failures | 5 |
| Lockout minutes | 15 |
| Access token minutes | from `Jwt:AccessTokenExpirationMinutes` (30) |
| Refresh days | from `RefreshToken:ExpirationDays` (7) |
| Reuse detection | true |
| Maintenance enabled | false |
| Admin bypass | true |

## Auth Integration

| Component | Behavior |
|-----------|----------|
| `JwtTokenService` | Access token lifetime from session policy, fallback to `JwtOptions` |
| `LoginCommandHandler` / `RefreshTokenCommandHandler` | Refresh expiry from session policy, fallback to `IRefreshTokenSettings` |
| `RefreshTokenCommandHandler` | Reuse detection only when session policy flag is true |
| Password validators | `ApplyPasswordPolicy()` uses live password policy |

Production JWT minimums in `ProductionStartupValidator` remain enforced.

## Security Notes

- Policies are configuration, not secrets
- Invalid policy payloads return `AccessPolicy.*` error codes (400)
- Policy caches invalidated on update

## Known Limitations

- Login lockout enforcement in login handler is future work (policy is stored and exposed)
- File storage limits in settings are not yet wired into Files module upload validation
- Session timeout minutes stored but not enforced server-side yet
