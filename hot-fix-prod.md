# Hot-fix Production Checklist

> **Mục đích:** Ghi lại các việc **bắt buộc / nên làm** trước khi deploy staging/production.
> Hiện tại project đang dev/test local — **chưa cần làm ngay**.
> Cập nhật lần cuối: Phase 8 review (Identity / Users / AuditLogs).

---

## Trạng thái hiện tại

| Môi trường | Ghi chú |
|---|---|
| Local / test | OK tiếp tục dev, build pass |
| Staging / Production | **Chưa sẵn sàng** — hoàn thành checklist bên dưới trước khi deploy |

---

## P0 — Bắt buộc trước deploy

### 1. Secrets & credentials

- [ ] **Không commit** password DB, JWT secret, refresh token secret vào git.
- [ ] Di chuyển sang **environment variables** hoặc **User Secrets / Key Vault**:
  - `ConnectionStrings__DefaultConnection`
  - `Jwt__Secret`
  - `Jwt__Issuer` / `Jwt__Audience` (theo môi trường thật)
  - `RefreshToken__Secret`
  - `AdminSeed__Password` (Production **bắt buộc** — seeder đã throw nếu thiếu)
- [ ] Rotate password DB nếu từng commit vào `appsettings.Development.json`.
- [ ] Reject placeholder secrets ở Production:
  - `Jwt:Secret` không được là `CHANGE_ME_TO_A_LONG_SECURE_SECRET_KEY_32_CHARS_MIN`
  - `RefreshToken:Secret` tương tự
- [ ] File tham chiếu:
  - `src/ApiHost/appsettings.json`
  - `src/ApiHost/appsettings.Development.json` — **không dùng file này trên prod**

### 2. Direct permissions không có trong JWT (bug authorization)

**Vấn đề:** `Users.AssignPermission` lưu vào `identity.user_permissions`, nhưng Login / Refresh / Me chỉ đưa permissions từ **roles** vào JWT. `[HasPermission]` check JWT claim `"permission"` → user được assign permission trực tiếp **không có quyền thực tế**.

**Fix:**

- [ ] Merge `user.DirectPermissions` vào danh sách permissions khi issue JWT:
  - `Identity.Application/Auth/Login/LoginCommandHandler.cs`
  - `Identity.Application/Auth/RefreshToken/RefreshTokenCommandHandler.cs`
  - `Identity.Application/Auth/Me/GetCurrentUserQuery.cs`
- [ ] Repository include `DirectPermissions`:
  - `Identity.Infrastructure/Persistence/IdentityUserRepository.cs`
- [ ] Test: assign direct permission → login/refresh → endpoint protected phải trả 200.

**Gợi ý code:**

```csharp
var permissions = user.Roles
    .Where(r => r.IsActive)
    .SelectMany(r => r.Permissions)
    .Select(p => p.Code)
    .Concat(user.DirectPermissions.Select(p => p.Code))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
```

### 3. Admin account mặc định

- [ ] Đổi password admin seed — không dùng `Admin@123456` trên prod.
- [ ] Cân nhắc tắt auto-seed admin sau lần deploy đầu (hoặc chỉ seed khi env flag bật).

---

## P1 — Security hardening (nên làm trước prod)

### 4. HTTPS & JWT Bearer

- [ ] Bật HTTPS termination (reverse proxy / Kestrel cert).
- [ ] `RequireHttpsMetadata = true` khi deploy Production:
  - `Identity.Infrastructure/DependencyInjection.cs` → `AddIdentityAuthentication`
- [ ] Cân nhắc giảm `AccessTokenExpirationMinutes` (hiện 30 phút).

### 5. Logging — không leak sensitive data

- [ ] **Tắt** `EnableRequestBodyLogging` / `EnableResponseBodyLogging` trên prod (hoặc chỉ bật tạm khi debug có kiểm soát).
- [ ] Giữ `LoggingOptions.SensitiveFields` đầy đủ (password, token, authorization, secret, …).
- [ ] Exclude path `/api/v1/auth/*` khỏi response body logging nếu vẫn bật ở staging.
- [ ] Không log failed-login username ở mức Warning nếu log tập trung dễ bị đọc (hoặc hash username).

### 6. Swagger

- [ ] Xác nhận Swagger UI **chỉ** chạy Development (đã gate `IsDevelopment()` — giữ nguyên).
- [ ] Staging: cân nhắc tắt hoặc bảo vệ bằng auth / IP whitelist.

### 7. Refresh token

- [ ] Cân nhắc **token family / reuse detection**: dùng refresh token đã revoke → revoke toàn bộ session user.
- [ ] Giới hạn số refresh token active / user (optional).
- [ ] Background job dọn token expired/revoked:
  - Table: `identity.user_refresh_tokens`

### 8. Brute-force protection

- [ ] Rate limit `POST /api/v1/auth/login` (theo IP + username).
- [ ] (Optional) Account lockout sau N lần sai password.

---

## P2 — Cải thiện vận hành (có thể làm sau go-live)

### 9. JWT permissions stale

- [ ] Document: đổi role/permission → user cần **refresh token** hoặc login lại.
- [ ] (Optional) Permission version claim + invalidate token khi đổi quyền.

### 10. Audit transaction ordering

- [ ] Audit hiện save **trước** Identity transaction commit → có thể orphan audit nếu business rollback.
- [ ] Fix: ghi audit sau commit, hoặc dùng Outbox (phase sau).

### 11. Identity business audit (TODO Phase 8)

- [ ] Audit Login / Logout / RefreshToken vào `audit.audit_logs` (không lưu token/password).

### 12. Password hashing

- [ ] Set BCrypt work factor explicit (ví dụ 12) trong `PasswordHasher.cs`.

### 13. Endpoint / permission chưa dùng

- [ ] `Users.Delete` permission đã seed nhưng chưa có API — implement hoặc bỏ khỏi seed.

---

## Checklist deploy nhanh

Trước mỗi lần deploy staging/prod, chạy qua:

```
[ ] Secrets qua env / vault — không trong appsettings committed
[ ] Connection string prod đúng host/port/database
[ ] Jwt:Secret & RefreshToken:Secret mạnh, unique, ≥ 32 ký tự
[ ] AdminSeed:Password đã set (Production)
[ ] Direct permissions fix đã merge vào JWT (nếu dùng AssignPermission)
[ ] Body logging tắt trên prod
[ ] Swagger không public
[ ] HTTPS bật
[ ] dotnet ef database update (Identity + AuditLogs contexts)
[ ] Smoke test: login → protected API → audit log → permission 403/200
[ ] Rotate secrets nếu từng leak vào git history
```

---

## File liên quan (tra cứu nhanh)

| Chủ đề | File |
|---|---|
| JWT issue / claims | `src/Modules/Identity/Identity.Infrastructure/Authentication/JwtTokenService.cs` |
| Login permissions | `src/Modules/Identity/Identity.Application/Auth/Login/LoginCommandHandler.cs` |
| Refresh permissions | `src/Modules/Identity/Identity.Application/Auth/RefreshToken/RefreshTokenCommandHandler.cs` |
| Permission authorize | `src/BuildingBlocks/BuildingBlocks.Web/Authorization/PermissionAuthorizationHandler.cs` |
| JWT bearer config | `src/Modules/Identity/Identity.Infrastructure/DependencyInjection.cs` |
| Admin seed | `src/Modules/Identity/Identity.Infrastructure/Seeding/IdentitySeeder.cs` |
| Sensitive log mask | `src/BuildingBlocks/BuildingBlocks.Application/Logging/SensitiveDataMasker.cs` |
| Request/response log | `src/ApiHost/Middlewares/RequestResponseLoggingMiddleware.cs` |
| App config | `src/ApiHost/appsettings.json`, `appsettings.Production.json` (tạo khi deploy) |

---

## Ghi chú

- **Local/test:** Các mục P0–P2 **không block** dev tiếp Phase 9.
- Khi bắt đầu chuẩn bị deploy, ưu tiên **P0 trước**, rồi P1, P2 có thể làm incremental.
- Sau khi fix từng mục, tick `[ ]` → `[x]` và ghi ngày + PR/commit tương ứng.
