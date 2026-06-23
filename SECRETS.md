# Secret Hygiene

This project never stores real credentials in source control. Follow these rules for every environment.

## Golden rules

1. **Never commit real secrets** in `appsettings*.json`, scripts, or documentation.
2. **Rotate any secret** that was ever committed to git, even if removed later.
3. **Use environment-specific secret stores** — not shared config files.

## Local development (non-Docker)

### Committed config

`appsettings.Development.json` contains **placeholders only** (for example `Password=CHANGE_ME`).

### Recommended: dotnet user-secrets

From the repository root:

```bash
dotnet user-secrets init --project src/ApiHost/ApiHost.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=modular_monolith_dev;Username=postgres;Password=YOUR_PASSWORD" --project src/ApiHost/ApiHost.csproj
dotnet user-secrets set "Jwt:Secret" "YOUR_LONG_RANDOM_JWT_SECRET" --project src/ApiHost/ApiHost.csproj
dotnet user-secrets set "RefreshToken:Secret" "YOUR_LONG_RANDOM_REFRESH_SECRET" --project src/ApiHost/ApiHost.csproj
```

User secrets override `appsettings.Development.json` at runtime.

### Alternative: gitignored local file

Create `src/ApiHost/appsettings.Development.local.json` (gitignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=modular_monolith_dev;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

ASP.NET Core loads `appsettings.{Environment}.local.json` automatically when present.

## Local Docker

- Copy `.env.example` → `.env` and adjust **dev-only** placeholders.
- `.env` is gitignored.
- `appsettings.Docker.json` contains dev placeholders; production values come from environment variables in `docker-compose.yml`.

## CI / deployment

- CI does **not** require secrets (tests use in-memory fakes).
- Production/deploy secrets belong in your CI/CD or cloud secret manager — not in the repository.

## Previously committed secrets

If a real database password or other credential was ever committed:

1. **Remove it from the codebase** (done in Phase 19.1 for Development config).
2. **Rotate the credential on the server** outside this repository.
3. **Audit git history** if the repository is shared or public (consider `git filter-repo` or platform secret scanning).
4. **Invalidate active sessions/tokens** if JWT or refresh secrets were exposed.

Do not paste old secrets into issues, docs, or chat.

## Files that must stay local-only

| File | Purpose |
|------|---------|
| `.env` | Docker Compose local overrides |
| `appsettings.Development.local.json` | SDK local overrides |
| `appsettings.*.local.json` | Any environment-specific local overrides |

## Docker / dev service credentials

Default passwords in `.env.example` and `appsettings.Docker.json` are **local development only**. Replace them before any shared or production-like environment.
