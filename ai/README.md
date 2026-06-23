# AI Workspace

**Một chỗ làm việc với AI.** Developer docs đầy đủ: [`docs/`](../docs/README.md).

> **`.cursor/` là gì?** Chỉ là “công tắc” Cursor bắt buộc — xem [CURSOR.md](./CURSOR.md). Hàng ngày bạn **chỉ cần `ai/`**, không cần mở `.cursor/`.

## Ba tầng — nhớ một câu

| Folder | Vai trò | Ai dùng |
|--------|---------|---------|
| **`docs/`** | Sách hướng dẫn dự án (đầy đủ) | Dev + AI khi cần chi tiết |
| **`ai/`** | Hub AI: rules, prompts, checklists | **Bạn + Cursor** |
| **`.cursor/rules/`** | Wiring Cursor (file `.mdc` mỏng) | Cursor tự đọc — **đừng sửa nội dung dài ở đây** |

```
docs/  ── chi tiết (BUSINESS_MODULE_RULES 35 mục, ARCHITECTURE, …)
  ↑
ai/    ── tóm tắt + prompt + checklist + rules (ai/rules/)
  ↑
.cursor/rules/  ── Cursor: khi nào bật rule + trỏ tới ai/rules/
```

## Cấu trúc `ai/`

```
ai/
  README.md           ← bạn đang ở đây
  CURSOR.md           ← giải thích tại sao không gộp hết vào ai/
  rules/              ← NỘI DUNG rule (sửa ở đây)
  references/         Index link tới docs/
  prompts/            Template task / review
  checklists/         DoD, câu hỏi trước code
  skills/             Skill project → .cursor/skills/
```

## Workflow nhanh

1. [references/README.md](./references/README.md) — chọn doc  
2. Module mới → [prompts/new-module-task.md](./prompts/new-module-task.md)  
3. Sửa rule agent → [rules/](./rules/)  
4. Sau code → [prompts/module-review.md](./prompts/module-review.md) + [checklists/definition-of-done.md](./checklists/definition-of-done.md)  
5. Clone / chạy lần đầu → skill [clone-setup-run](../.cursor/skills/clone-setup-run/SKILL.md)  
6. `dotnet build` + `dotnet test`

## Tài liệu business module

- [docs/BUSINESS_MODULE_RULES.md](../docs/BUSINESS_MODULE_RULES.md)
- [docs/BUSINESS_MODULE_WORKFLOW_GUIDE.md](../docs/BUSINESS_MODULE_WORKFLOW_GUIDE.md)
- [docs/MODULE_DEVELOPMENT_GUIDE.md](../docs/MODULE_DEVELOPMENT_GUIDE.md)
