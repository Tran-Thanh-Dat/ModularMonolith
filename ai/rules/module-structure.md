# Module Structure

Mỗi business module: `{Name}.Domain` → `.Application` → `.Infrastructure` → `.Api`.

```
Domain        → BuildingBlocks.Domain only
Application   → Domain, BuildingBlocks.Application
Infrastructure→ Application, Domain, BuildingBlocks.Infrastructure
Api           → Application, BuildingBlocks.Web
```

- Không circular dependency giữa modules
- Không business logic trong `Api`
- Không EF trong `Domain`
- Không expose EF entity ra API

**Domain:** entity kế thừa `AuditableEntity` / `SoftDeletableEntity`; có domain methods (`Create`, `Update`, `Activate`, `SoftDelete`…). Không gán status trực tiếp từ Application — gọi domain method.

**DB:** mỗi module có `{Name}DbContext` + EF configuration (schema, indexes, filtered unique). Migration: `Add{Name}Module`.

**DI:** `Add{Name}Infrastructure` + `Add{Name}Api` / presentation; `Program.cs` chỉ orchestration.
