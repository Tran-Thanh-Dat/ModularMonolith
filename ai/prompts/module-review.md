# Prompt: Review Business Module

Copy sau khi AI/dev implement xong.

---

Review the implemented **{ModuleName}** module against:

- `docs/BUSINESS_MODULE_RULES.md`
- `docs/BUSINESS_MODULE_WORKFLOW_GUIDE.md`
- `ai/checklists/definition-of-done.md`
- `.cursor/rules/`

Check:

1. Module structure (4 projects, dependency direction)
2. Domain behavior (methods, not anemic setters)
3. DbContext / EF config / migration
4. Typed UnitOfWork registration; no plain `IUnitOfWork` in services
5. DTOs / validators / ErrorCodes
6. Permissions seed + `[HasPermission]` on controllers
7. API: `/api/v1/`, BaseApiController, no entity exposure
8. AuditLog + ActivityLog post-commit
9. Cache post-commit invalidation (if applicable)
10. Files / Notifications / Jobs (if applicable)
11. Security / IDOR / no secrets in logs or responses
12. Tests + docs accuracy (no unimplemented features claimed as done)
13. `dotnet build` + `dotnet test`

Return:

1. What was implemented correctly  
2. What is missing  
3. What is inaccurate or risky  
4. What must be fixed before Done  
5. Whether this is a good reference implementation  

Run:

```bash
dotnet build ModularMonolith.sln
dotnet test ModularMonolith.sln
```
