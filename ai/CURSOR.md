# Cursor vs AI — Giải thích ngắn

## Bạn chỉ cần nhớ

| Bạn muốn… | Vào đây |
|-----------|---------|
| Làm việc với AI (prompt, checklist, rule, skill) | **`ai/`** |
| Đọc tài liệu dự án đầy đủ | **`docs/`** |
| Sửa rule cho agent | **`ai/rules/`** (nội dung) — xem bảng dưới |

**Không cần mở `.cursor/` hàng ngày** trừ khi đổi *khi nào* rule được bật (glob / alwaysApply).

## Tại sao vẫn có `.cursor/`?

**Cursor IDE** (phần mềm) chỉ tự động load rule từ:

```
.cursor/rules/*.mdc
```

Đây là **giới hạn kỹ thuật của Cursor**, không phải do team chọn tách cho vui.  
File `.mdc` cần YAML frontmatter (`alwaysApply`, `globs`, `description`) — Cursor không đọc rule từ `ai/rules/` trực tiếp.

## Mô hình hiện tại (1 nguồn nội dung + 1 lớp wiring)

```
ai/rules/*.md          ← NỘI DUNG rule (bạn sửa ở đây)
       ↑
       │  trỏ tới
       │
.cursor/rules/*.mdc    ← WIRING: khi nào áp dụng + bắt AI đọc ai/rules/
```

- **`ai/`** = mọi thứ làm việc với AI: rules (nội dung), prompts, checklists, references, skills sau này  
- **`.cursor/rules/`** = file mỏng: metadata + “đọc `ai/rules/...`”  
- **`docs/`** = sách hướng dẫn dự án (architecture, API, business module đầy đủ)

## Không gộp hết vào `ai/` được không?

| Ý tưởng | Khả thi? |
|---------|----------|
| Xóa `.cursor/`, chỉ giữ `ai/` | ❌ Cursor sẽ **không** auto-apply rule |
| Copy rule chỉ trong `.cursor/` | ✅ Cursor chạy được nhưng dev khó tìm, trùng với hub AI |
| Nội dung trong `ai/rules/`, stub trong `.cursor/` | ✅ **Đang dùng** — một chỗ hiểu, Cursor vẫn hoạt động |

## Thêm / sửa rule

1. Sửa nội dung: `ai/rules/{tên}.md`  
2. Nếu đổi **phạm vi** (luôn bật / chỉ `src/Modules/**`): sửa `.cursor/rules/{tên}.mdc` (frontmatter `alwaysApply`, `globs`)  
3. Cập nhật bảng trong [rules/README.md](./rules/README.md)

## Ba tầng tài liệu

```
docs/     → Chi tiết, đầy đủ (BUSINESS_MODULE_RULES 35 mục, ARCHITECTURE, …)
ai/       → Làm việc với AI: rule tóm tắt, prompt, checklist
.cursor/  → Cấu hình Cursor (chỉ rules wiring)
```

**Luồng:** `docs/` (hiểu sâu) → `ai/rules/` (agent bám theo) → `.cursor/rules/` (Cursor inject vào session).
