# Prompt: Implement New Business Module

Copy và điền vào chat Cursor (Agent mode).

---

Read and follow:

- `ai/references/README.md`
- `docs/BUSINESS_MODULE_RULES.md`
- `docs/BUSINESS_MODULE_WORKFLOW_GUIDE.md`
- `docs/MODULE_DEVELOPMENT_GUIDE.md`
- `.cursor/rules/` (module rules)

Complete `ai/checklists/pre-implement-questions.md` mentally before coding.

## Task

Implement **{ModuleName}** module.

### Business purpose

{Mô tả ngắn}

### Scope

- {API/use case 1}
- {API/use case 2}

### Out of scope

- {Item 1}

### Required APIs

| Method | Route | Permission |
|--------|-------|------------|
| GET | /api/v1/{resource} | {Module}.View |
| … | … | … |

### Required permissions

- `{Module}.View`, `.Create`, …

### Integrations

| Integration | Yes/No | Notes |
|-------------|--------|-------|
| AuditLog | | |
| ActivityLog | | |
| Cache | | |
| Files | | |
| Notifications | | |
| BackgroundJobs | | |

### Rules (mandatory)

- Typed `{Module}UnitOfWork : EfUnitOfWork<{Module}DbContext>` — **do not** inject plain `IUnitOfWork` in services
- No EF entities in API responses
- `Result<T>`, `ApiResponse`, `PagedResponse`
- FluentValidation + centralized ErrorCodes
- Post-commit: activity log enqueue, cache invalidation buffer
- Add tests; update docs

### Expected output

- `dotnet build` and `dotnet test` pass
- `docs/{MODULE}.md` + updates to shared docs
- No real secrets

---

Sau khi xong: chạy [module-review.md](./module-review.md) và [definition-of-done.md](../checklists/definition-of-done.md).
