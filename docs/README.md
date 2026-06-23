# Developer Documentation

.NET 8 modular monolith Web API with PostgreSQL, Redis, Hangfire, JWT authentication, and permission-based authorization.

**Làm việc với AI (Cursor):** [../ai/README.md](../ai/README.md) — mọi thứ trong `ai/`; `.cursor/` chỉ wiring ([giải thích](../ai/CURSOR.md)).

## Start here

| Topic | Document |
|-------|----------|
| **Setup sau khi clone (README root)** | [../README.md#setup-sau-khi-clone-source-net-sdk](../README.md#setup-sau-khi-clone-source-net-sdk) |
| **Chạy migration (1 lệnh)** | [../README.md#chạy-migration](../README.md#chạy-migration) |
| **Technical Architecture (TA) — cấu trúc source đầy đủ** | [TECHNICAL_ARCHITECTURE.md](./TECHNICAL_ARCHITECTURE.md) |
| Architecture & request flow | [ARCHITECTURE.md](./ARCHITECTURE.md) · [KIEN_TRUC.md](./KIEN_TRUC.md) (Tiếng Việt) |
| API routes, responses, status codes | [API_CONVENTIONS.md](./API_CONVENTIONS.md) |
| Login, JWT, refresh tokens | [AUTHENTICATION.md](./AUTHENTICATION.md) |
| Account profile & password reset | [ACCOUNT.md](./ACCOUNT.md) |
| Roles, permissions, `[HasPermission]` | [AUTHORIZATION.md](./AUTHORIZATION.md) |
| Error code reference | [ERROR_CODES.md](./ERROR_CODES.md) |
| Pagination & filtering | [PAGINATION.md](./PAGINATION.md) |
| Module overview | [MODULES.md](./MODULES.md) |
| Create a new module | [MODULE_DEVELOPMENT_GUIDE.md](./MODULE_DEVELOPMENT_GUIDE.md) |
| Business module rules (bắt buộc) | [BUSINESS_MODULE_RULES.md](./BUSINESS_MODULE_RULES.md) |
| Business module workflow | [BUSINESS_MODULE_WORKFLOW_GUIDE.md](./BUSINESS_MODULE_WORKFLOW_GUIDE.md) |

## Operations & platform

| Topic | Document |
|-------|----------|
| Local SDK setup | [LOCAL_DEVELOPMENT.md](./LOCAL_DEVELOPMENT.md) |
| Docker stack | [../DOCKER.md](../DOCKER.md) |
| Production deployment | [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md) |
| Security hardening | [../SECURITY.md](../SECURITY.md) |
| CI pipeline | [../CI.md](../CI.md) |
| Test baseline & commands | [../TESTING.md](../TESTING.md) |
| Testing guide (developer) | [TESTING_GUIDE.md](./TESTING_GUIDE.md) |
| Troubleshooting | [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) |

## Cross-cutting guides

| Topic | Document |
|-------|----------|
| Post-commit hooks | [POST_COMMIT_HOOKS.md](./POST_COMMIT_HOOKS.md) |
| Caching (Memory/Redis) | [CACHING.md](./CACHING.md) |
| File upload/download | [FILES.md](./FILES.md) |
| Email & notifications | [NOTIFICATIONS.md](./NOTIFICATIONS.md) |
| Hangfire background jobs | [BACKGROUND_JOBS.md](./BACKGROUND_JOBS.md) |
| Health & monitoring | [MONITORING.md](./MONITORING.md) |
| System settings | [SETTINGS.md](./SETTINGS.md) |
| Access / security policies | [ACCESS_POLICY.md](./ACCESS_POLICY.md) |

## Quick commands

```bash
dotnet restore ModularMonolith.sln
dotnet build ModularMonolith.sln
dotnet test ModularMonolith.sln
dotnet run --project src/ApiHost/ApiHost.csproj
```

Docker: see [LOCAL_DEVELOPMENT.md](./LOCAL_DEVELOPMENT.md) and [../DOCKER.md](../DOCKER.md).

Swagger (Development/Docker): `http://localhost:5080/swagger` or the port configured in your environment.

## Solution layout

```
src/
  ApiHost/              Application host (Program.cs, middleware, config)
  BuildingBlocks/       Shared Domain, Application, Infrastructure, Web
  Modules/
    Identity/           Auth, JWT, refresh tokens
    Users/              Users, roles, permissions
    Categories/         Category CRUD
    Files/              Upload, download, storage
    Notifications/      Email, templates, in-app notifications
    BackgroundJobs/     Hangfire jobs
    AuditLogs/          Audit trail & activity logs
    Monitoring/         Health details, system info
    Settings/           System settings & access policies
```

## Contribution workflow

1. Read [ARCHITECTURE.md](./ARCHITECTURE.md) and [MODULE_DEVELOPMENT_GUIDE.md](./MODULE_DEVELOPMENT_GUIDE.md).
2. Follow [API_CONVENTIONS.md](./API_CONVENTIONS.md) for routes and responses.
3. Add permissions and error codes per [AUTHORIZATION.md](./AUTHORIZATION.md) and [ERROR_CODES.md](./ERROR_CODES.md).
4. Run `dotnet build` and `dotnet test` before opening a PR.
5. CI runs build, test, Docker build — see [../CI.md](../CI.md).

Never commit real secrets. See [../SECRETS.md](../SECRETS.md).
