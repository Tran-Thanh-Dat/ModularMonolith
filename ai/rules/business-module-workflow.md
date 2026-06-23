# Business Module Workflow

Trước khi code, làm rõ: entity, actors, permissions, cache/audit/file/notification/job, ownership.

- Index: [../references/README.md](../references/README.md)
- Prompt: [../prompts/new-module-task.md](../prompts/new-module-task.md)
- DoD: [../checklists/definition-of-done.md](../checklists/definition-of-done.md)

**10 bước:** Requirement → Scope/Impact → Module Design → API/Permission Design → DB/Migration → Implementation → Cross-cutting → Testing → Documentation → Review/DoD.

**Thứ tự implement:**

1. Domain + status constants  
2. Application DTOs + ErrorCodes + validators  
3. DbContext, EF config, typed UnitOfWork, DI, migration  
4. Handlers/services (typed UoW, Result, post-commit enqueue)  
5. Api controllers + permission seed (`PermissionCodes.All`, idempotent seeder)  
6. Cache keys + invalidation (nếu read-heavy)  
7. Tests (create, duplicate, not found, pagination, cache, activity enqueue)  
8. Docs: `docs/{MODULE}.md` + update `MODULES.md`, `AUTHORIZATION.md`, `ERROR_CODES.md`

Sau implement: review theo [docs/BUSINESS_MODULE_RULES.md](../../docs/BUSINESS_MODULE_RULES.md) §34 và `dotnet build` / `dotnet test`.
