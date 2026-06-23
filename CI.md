# Continuous Integration

This repository includes a GitHub Actions workflow for build, test, publish, and Docker image validation.

## Workflow file

`.github/workflows/ci.yml`

## Triggers

- Push to `main`, `master`, or `develop`
- Pull requests targeting those branches

## Jobs

### 1. Build and Test

Runs on `ubuntu-latest`:

1. Checkout
2. Setup .NET 8 (with NuGet cache)
3. `dotnet restore ModularMonolith.sln`
4. `dotnet build ModularMonolith.sln --configuration Release --no-restore`
5. `dotnet test ModularMonolith.sln --configuration Release --no-build`
6. `dotnet list ModularMonolith.sln package --vulnerable --include-transitive` (non-blocking; `continue-on-error: true`)
7. Upload TRX test results as artifact
8. `dotnet publish src/ApiHost/ApiHost.csproj --configuration Release --output ./publish --no-build`

**External services:** not required. Tests use in-memory fakes and the `IntegrationTesting` environment.

Expected baseline: **150+ passed**, **4 skipped**, **0 failed** (includes Phase 20.1 security tests).

See [SECURITY.md](./SECURITY.md) for dependency scan commands and vulnerability response.

### 2. Docker Build

Runs after build/test succeeds:

1. Checkout
2. Docker Buildx setup
3. Build image from root `Dockerfile` (tag: `modular-monolith-api:ci`)
4. GitHub Actions cache for Docker layers
5. `docker compose config --quiet` (validates compose file; uses inline defaults from compose)

No secrets are required for the Docker build or compose validation.

## Running CI steps locally

```bash
dotnet restore ModularMonolith.sln
dotnet build ModularMonolith.sln --configuration Release --no-restore
dotnet test ModularMonolith.sln --configuration Release --no-build
dotnet publish src/ApiHost/ApiHost.csproj --configuration Release --output ./publish --no-build
docker build -t modular-monolith-api:local .
```

Windows PowerShell:

```powershell
.\scripts\run-tests.ps1
docker build -t modular-monolith-api:local .
```

## Optional integration test profile (future)

The workflow includes a commented placeholder job for PostgreSQL/Redis-backed integration tests.

To enable later:

1. Start dependencies: `docker compose up -d postgres redis`
2. Remove `[Fact(Skip = ...)]` from business HTTP tests in `ApiHost.IntegrationTests`
3. Add Testcontainers or a compose-backed job in CI
4. Use a dedicated test category/filter to avoid flaky default CI

Do **not** make default CI depend on Docker services until the setup is reliable.

## Artifacts

| Artifact | Contents |
|----------|----------|
| `test-results` | `*.trx` files from xUnit |

## Coverage

No coverage upload is configured. Add a step if you introduce Coverlet or similar.

## Related docs

- [DOCKER.md](./DOCKER.md) — local Docker stack
- [TESTING.md](./TESTING.md) — test types and skipped tests
- [SECRETS.md](./SECRETS.md) — secret hygiene
