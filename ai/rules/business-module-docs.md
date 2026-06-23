# Business Module — Doc References

Hub: [../README.md](../README.md) · Index: [../references/README.md](../references/README.md) · Prompts: [../prompts/](../prompts/)

Khi làm **module business mới** hoặc **tính năng lớn**, đọc trước:

- [../references/README.md](../references/README.md) — index nhanh
- [docs/BUSINESS_MODULE_RULES.md](../../docs/BUSINESS_MODULE_RULES.md) — quy chuẩn bắt buộc
- [docs/BUSINESS_MODULE_WORKFLOW_GUIDE.md](../../docs/BUSINESS_MODULE_WORKFLOW_GUIDE.md) — quy trình 10 bước
- [docs/MODULE_DEVELOPMENT_GUIDE.md](../../docs/MODULE_DEVELOPMENT_GUIDE.md) — checklist kỹ thuật
- `docs/API_CONVENTIONS.md`, `docs/AUTHORIZATION.md`, `docs/ERROR_CODES.md`, `docs/POST_COMMIT_HOOKS.md`

Template: [../prompts/new-module-task.md](../prompts/new-module-task.md) · Review: [../prompts/module-review.md](../prompts/module-review.md)

**Không cần full workflow** cho: sửa typo, message text, field response đơn giản, refactor nhỏ không đổi behavior.

**Definition of Done:**

- `dotnet build` + `dotnet test` pass
- Permissions + ErrorCodes + validators + tests + **docs cập nhật (luôn đồng bộ code — xem [feature-documentation.md](./feature-documentation.md))**
- Không secret thật, không phá production hardening
- Feature chưa làm → **Future/Backlog**, không document như đã xong

**Users module:** không có `UsersDbContext` — dùng `IdentityDbContext` / `IdentityUnitOfWork`.
