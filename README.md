# Modular Monolith Web API

.NET 8 modular monolith with PostgreSQL, Redis, Hangfire, JWT authentication, and permission-based authorization.

## Documentation

| Document | Description |
|----------|-------------|
| **[ai/README.md](./ai/README.md)** | **AI workspace — rules, prompts, checklists** ([`.cursor` vs `ai`](./ai/CURSOR.md)) |
| **[docs/README.md](./docs/README.md)** | **Developer guide index (start here)** |
| [docs/TECHNICAL_ARCHITECTURE.md](./docs/TECHNICAL_ARCHITECTURE.md) | **Technical Architecture — cấu trúc source, folder/file, onboarding** |
| [DOCKER.md](./DOCKER.md) | Local Docker Compose stack |
| [CI.md](./CI.md) | GitHub Actions CI pipeline |
| [TESTING.md](./TESTING.md) | Unit and integration tests |
| [SECRETS.md](./SECRETS.md) | Secret hygiene and local credential setup |
| [SECURITY.md](./SECURITY.md) | Production security policies |
| [PRODUCTION.md](./PRODUCTION.md) | Production deployment |

---

## Setup sau khi clone source (.NET SDK)

Hướng dẫn chạy lần đầu trên Windows (Visual Studio hoặc CLI). Nếu dùng Docker full stack, xem [Quick start (Docker)](#quick-start-docker) bên dưới.

### Yêu cầu

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 14+ (local, Docker, hoặc server remote — pgAdmin/pgAdmin4)
- Visual Studio 2022 **hoặc** VS Code + C# Dev Kit (tùy chọn)

### Bước 1 — Clone & restore

```powershell
git clone <repo-url> WebApplication
cd WebApplication
dotnet restore ModularMonolith.sln
dotnet build ModularMonolith.sln
```

### Bước 2 — Cấu hình PostgreSQL (bắt buộc)

App đọc connection string qua key **`ConnectionStrings:DefaultConnection`** (format Npgsql). **Không** đọc trực tiếp biến `DB_HOST`, `DB_PORT`…

Tạo file **`src/ApiHost/appsettings.Development.local.json`** (file này **gitignore**, không commit password):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_HOST;Port=5432;Database=YOUR_DB;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

Ví dụ DB local:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=modular_monolith_dev;Username=postgres;Password=YOUR_PASSWORD"
```

Đảm bảo database đã được tạo trên PostgreSQL trước khi chạy app.

> Cách khác: [dotnet user-secrets](./SECRETS.md) — xem [docs/LOCAL_DEVELOPMENT.md](./docs/LOCAL_DEVELOPMENT.md).

### Bước 3 — Migration & seed (chọn 1 trong 2)

**Cách A — Tự động khi start app (mặc định Development):**

`appsettings.Development.json` đã bật:

```json
"Database": { "ApplyMigrationsOnStartup": true }
```

Chỉ cần chạy app (Bước 4) — migration + seed admin chạy lúc startup.

**Cách B — Chạy migration thủ công:** xem section [Chạy migration](#chạy-migration) bên dưới.

### Bước 4 — Chạy API

**Visual Studio:** mở `ModularMonolith.sln` → set startup project **`ApiHost`** → **F5**.

**CLI:**

```powershell
dotnet run --project src/ApiHost/ApiHost.csproj
```

- Swagger: **http://localhost:5080/swagger**
- Health: **http://localhost:5080/health/live**

### Bước 5 — Đăng nhập (account seed mặc định)

Sau seed lần đầu (`AdminSeed:Enabled = true`):

| | |
|-|-|
| **Username** | `admin` |
| **Password** | `Admin@123456` (Development) |
| **API** | `POST /api/v1/auth/login` |

```json
{
  "userNameOrEmail": "admin",
  "password": "Admin@123456"
}
```

Copy `accessToken` → Swagger **Authorize** → `Bearer {token}`.

> Chi tiết auth: [docs/AUTHENTICATION.md](./docs/AUTHENTICATION.md). Không commit password thật — [SECRETS.md](./SECRETS.md).

### Kiểm tra nhanh

```powershell
dotnet test ModularMonolith.sln
```

---

## Chạy migration

**Một lệnh (PowerShell, dùng `.env` hoặc localhost mặc định):**

```powershell
.\scripts\apply-migrations.ps1
```

Script apply tất cả DbContext: Identity, AuditLogs, Categories, Files, Notifications, BackgroundJobs.

**Migration thủ công cho từng module (ví dụ Identity):**

```powershell
dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --startup-project src/ApiHost --context IdentityDbContext
```

**Lưu ý:** Cần cài EF CLI nếu chưa có: `dotnet tool install --global dotnet-ef`

Migration + seed admin cũng chạy tự động khi `Database:ApplyMigrationsOnStartup = true` — xem [docs/LOCAL_DEVELOPMENT.md](./docs/LOCAL_DEVELOPMENT.md).

---

## Quick start (Docker)

```bash
cp .env.example .env
./scripts/docker-smoke.sh
```

Windows PowerShell:

```powershell
Copy-Item .env.example .env
.\scripts\docker-smoke.ps1
```

- API: http://localhost:5080/swagger
- Mailpit: http://localhost:8025

See [DOCKER.md](./DOCKER.md) for details.

## Quick start (local SDK)

Tóm tắt — hướng dẫn đầy đủ: [Setup sau khi clone source](#setup-sau-khi-clone-source-net-sdk).

```bash
dotnet restore ModularMonolith.sln
dotnet build ModularMonolith.sln
dotnet test ModularMonolith.sln
```

Configure secrets before running — see [SECRETS.md](./SECRETS.md) and [docs/LOCAL_DEVELOPMENT.md](./docs/LOCAL_DEVELOPMENT.md).

```bash
dotnet run --project src/ApiHost/ApiHost.csproj
```

Requires PostgreSQL for full runtime. Tests run without external services by default.

## Foundation summary

Phases 1–21 establish:

- Modular monolith (Identity, Users, Categories, Files, Notifications, BackgroundJobs, Audit/Activity logs, Monitoring)
- JWT + refresh token rotation, RBAC permissions
- MediatR pipeline (validation, transactions, post-commit hooks)
- Redis/memory caching, Hangfire jobs, health checks
- Production hardening, Docker/CI, and developer documentation

## Solution structure

```
src/
  ApiHost/           Application host
  BuildingBlocks/    Shared infrastructure
  Modules/           Feature modules
```

See [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) for layering and request flow.

## Secret hygiene

- Never commit real passwords or tokens in `appsettings*.json`
- Use dotnet user-secrets or gitignored local config for SDK development
- Use `.env` (gitignored) for Docker Compose

See [SECRETS.md](./SECRETS.md).
