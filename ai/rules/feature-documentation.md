# Feature Documentation — Always Update

Hub: [../README.md](../README.md) · DoD: [../checklists/definition-of-done.md](../checklists/definition-of-done.md)

**Bắt buộc:** Mọi tính năng mới hoặc thay đổi hành vi API/platform phải **cập nhật documentation trong cùng PR/task** — docs phản ánh **trạng thái mới nhất**, không để lệch so với code.

## Khi nào áp dụng

- Module business mới, endpoint mới, flow auth/account mới
- Thêm/sửa permissions, error codes, config keys, migration
- Thay đổi behavior (validation, security, response shape)
- Sửa bug làm đổi contract hoặc hành vi documented

**Không bắt buộc full doc pass** cho: typo, comment nội bộ, refactor không đổi behavior/API.

## Checklist tối thiểu

| Thay đổi | Cập nhật |
|----------|----------|
| Module / API mới | `docs/{MODULE}.md` (tạo mới nếu chưa có) |
| Endpoint / route | Module doc + `docs/MODULES.md` |
| Auth / account | `docs/AUTHENTICATION.md`, `docs/ACCOUNT.md` (nếu liên quan) |
| Permission mới | `docs/AUTHORIZATION.md` + seed `PermissionCodes` |
| Error code mới | `ErrorCodes.cs` + `docs/ERROR_CODES.md` |
| Config mới | Module doc + `appsettings.json` / `.env.example` (placeholder only) |
| Migration / schema | Module doc (Entities) + ghi chú migrate |
| Cross-cutting (cache, jobs, email) | Doc module tương ứng (`CACHING.md`, `BACKGROUND_JOBS.md`, …) |
| Index tổng quan | `docs/README.md` nếu thêm doc file mới |

## Nguyên tắc nội dung

1. **Truthful** — Chỉ document những gì đã implement; feature chưa làm → ghi **Future/Backlog**.
2. **Current** — Sửa/xóa nội dung cũ sai hoặc lỗi thời; không để hai mô tả mâu thuẫn.
3. **Actionable** — Route, method, permission, error code, ví dụ curl/request khi có API.
4. **No secrets** — Chỉ placeholder trong docs và config mẫu.

## Definition of Done (docs)

Task **chưa xong** nếu thiếu doc cập nhật khi có thay đổi user-facing hoặc developer-facing.

Cùng lúc với code:

- [ ] Doc module / feature liên quan
- [ ] `MODULES.md`, `ERROR_CODES.md`, `AUTHORIZATION.md` (nếu đụng)
- [ ] `docs/README.md` link tới doc mới (nếu có)
- [ ] Không claim feature đã xong trong doc khi code chưa có

Chi tiết module business: [business-module-docs.md](./business-module-docs.md) · Checklist đầy đủ: [definition-of-done.md](../checklists/definition-of-done.md)
