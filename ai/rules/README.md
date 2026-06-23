# Rules (nội dung)

**Source of truth** cho agent rules. Cursor wiring: `.cursor/rules/*.mdc`.

| File | Cursor wiring | Khi nào áp dụng |
|------|---------------|-----------------|
| [business-module-docs.md](./business-module-docs.md) | `business-module-docs.mdc` | Luôn (`alwaysApply`) |
| [feature-documentation.md](./feature-documentation.md) | `feature-documentation.mdc` | Luôn (`alwaysApply`) — mọi tính năng/thay đổi API phải cập nhật docs |
| [module-structure.md](./module-structure.md) | `module-structure.mdc` | `src/Modules/**` |
| [module-unitofwork.md](./module-unitofwork.md) | `module-unitofwork.mdc` | Application/Infrastructure |
| [module-api-layer.md](./module-api-layer.md) | `module-api-layer.mdc` | `*.Api/**` |
| [module-cross-cutting.md](./module-cross-cutting.md) | `module-cross-cutting.mdc` | `src/Modules/**` |
| [business-module-workflow.md](./business-module-workflow.md) | `business-module-workflow.mdc` | `src/Modules/**` |

Chi tiết đầy đủ: [docs/BUSINESS_MODULE_RULES.md](../../docs/BUSINESS_MODULE_RULES.md)

Giải thích `.cursor` vs `ai`: [../CURSOR.md](../CURSOR.md)
