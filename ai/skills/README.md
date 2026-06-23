# Project Skills

Skill theo project — AI đọc khi user @ mention hoặc khi agent match description.

| Skill | Mục đích | Path |
|-------|----------|------|
| **clone-setup-run** | Setup & chạy source sau clone, migration, login seed, lỗi startup thường gặp | [.cursor/skills/clone-setup-run/SKILL.md](../.cursor/skills/clone-setup-run/SKILL.md) |

## Thêm skill mới

1. Tạo folder: `.cursor/skills/{skill-name}/`
2. Thêm `SKILL.md` với YAML frontmatter (`name`, `description`) — xem Cursor skill `create-skill`
3. (Tuỳ chọn) Stub link trong `ai/skills/{skill-name}/SKILL.md` trỏ về `.cursor/skills/`
4. Cập nhật bảng trên

## Khi nào dùng skill vs rule vs docs

| Loại | Khi nào |
|------|---------|
| `.cursor/rules/*.mdc` | Convention coding tự động mỗi session |
| `.cursor/skills/` | Workflow cụ thể (setup, scaffold module…) |
| `docs/` | Tài liệu đầy đủ cho human + AI reference |

## Ví dụ skill có thể thêm sau

| Skill | Mục đích |
|-------|----------|
| `implement-business-module` | Scaffold module từ Categories template |
| `add-permission-endpoint` | Permission + seed + controller |
| `postgres-integration-test` | Testcontainers setup |
