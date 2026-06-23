# Local Development

Run the API on your machine with the .NET SDK.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 14+ (local install or Docker)
- Optional: Redis (Production-like cache testing)
- Optional: Docker Desktop (full stack via Compose)

## Restore, build, run

```bash
dotnet restore ModularMonolith.sln
dotnet build ModularMonolith.sln
dotnet run --project src/ApiHost/ApiHost.csproj
```

Default Development URL is in `src/ApiHost/Properties/launchSettings.json` (typically https://localhost:7xxx or http://localhost:5xxx).

## Configuration

### dotnet user-secrets (recommended)

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=modular_monolith_dev;Username=postgres;Password=YOUR_PASSWORD" --project src/ApiHost/ApiHost.csproj

dotnet user-secrets set "Jwt:Secret" "YOUR_32_CHAR_MIN_JWT_SECRET" --project src/ApiHost/ApiHost.csproj

dotnet user-secrets set "RefreshToken:Secret" "YOUR_32_CHAR_MIN_REFRESH_SECRET" --project src/ApiHost/ApiHost.csproj
```

See [../SECRETS.md](../SECRETS.md) — never commit real passwords.

### appsettings.Development.local.json (optional)

Gitignored overlay copied from example patterns in SECRETS.md. Loaded after `appsettings.Development.json`.

## PostgreSQL

1. Create database `modular_monolith_dev`
2. Set connection string via user-secrets
3. Enable migrations on startup for local dev:

```json
"Database": {
  "ApplyMigrationsOnStartup": true
}
```

Or apply manually:

```bash
dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --startup-project src/ApiHost --context IdentityDbContext
```

(Repeat per module DbContext or use startup migration runner when enabled.)

## Redis (optional)

Default Development uses `Cache:Provider=Memory`. For Redis:

```json
"Cache": {
  "Provider": "Redis",
  "Redis": { "ConnectionString": "localhost:6379" }
}
```

## Mailpit / SMTP

Point `Email:Smtp:Host` to `localhost` and use Mailpit (Docker stack) or a local SMTP test server.

Enable TestMode in Development to redirect all mail:

```json
"Email": { "TestMode": { "Enabled": true, "RedirectTo": "dev@example.com" } }
```

## Docker local option

Full stack with PostgreSQL, Redis, Mailpit, API:

```powershell
Copy-Item .env.example .env
.\scripts\docker-smoke.ps1
```

API + Swagger: http://localhost:5080/swagger — see [../DOCKER.md](../DOCKER.md).

## Swagger

Available in **Development** and **Docker** environments automatically.

Override: `"Swagger": { "Enabled": true }` in config.

**Not exposed in Production** unless explicitly overridden (Production forces off).

## Admin seed

Default admin user from `AdminSeed` section — change password in user-secrets before first run in shared environments.

## Common issues

See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md).

## Next steps

- [AUTHENTICATION.md](./AUTHENTICATION.md) — login and JWT
- [TESTING_GUIDE.md](./TESTING_GUIDE.md) — run tests
- [MODULE_DEVELOPMENT_GUIDE.md](./MODULE_DEVELOPMENT_GUIDE.md) — add features
