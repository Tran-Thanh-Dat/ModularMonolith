# Definition of Done — Business Module / Feature

Dùng sau khi AI hoặc dev hoàn thành module/tính năng business.

## Build & test

- [ ] `dotnet build ModularMonolith.sln` — 0 errors
- [ ] `dotnet test ModularMonolith.sln` — pass (ghi rõ nếu thêm skipped test)

## Structure

- [ ] Domain / Application / Infrastructure / Api (nếu module mới)
- [ ] Typed `{Module}UnitOfWork : EfUnitOfWork<{Module}DbContext>`
- [ ] Service **không** inject plain `IUnitOfWork`
- [ ] Không expose EF entity từ API

## Security & API

- [ ] Route `/api/v1/...`
- [ ] `[Authorize]` + `[HasPermission]` trên protected endpoints
- [ ] DTO request/response; `FromResult` / `CreatedFromResult`
- [ ] ErrorCodes trong `ErrorCodes.cs`
- [ ] FluentValidation cho write operations
- [ ] Permissions seed idempotent + `PermissionCodes.All`

## Cross-cutting

- [ ] Audit (EF) cho entity chính
- [ ] ActivityLog: `EnqueuePostCommitAsync` cho mutations
- [ ] Cache: invalidation qua buffer post-commit (nếu có cache)
- [ ] Không secret trong code/docs

## Tests & docs

- [ ] Unit/service tests tối thiểu (create, not found, validation, duplicate nếu có code unique)
- [ ] **Docs luôn đồng bộ code** — xem [../rules/feature-documentation.md](../rules/feature-documentation.md)
- [ ] **`docs/API-DOCUMENT.md`** — cập nhật khi thêm/sửa/xóa API
- [ ] `docs/{MODULE}.md` hoặc cập nhật module doc
- [ ] `docs/MODULES.md`, `AUTHORIZATION.md`, `ERROR_CODES.md`, `docs/README.md` (link doc mới) nếu có thay đổi
- [ ] Future/backlog ghi rõ — không claim đã implement

## Review

Chạy prompt: [../prompts/module-review.md](../prompts/module-review.md)
