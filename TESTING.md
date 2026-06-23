# Testing Guide

This solution uses **xUnit** for unit and integration tests. Tests are colocated under `src/` next to the code they exercise.

**Current status (Phase 21):** **151 passed**, **4 skipped**, **155 total** across **13 test projects**. See [docs/TESTING_GUIDE.md](./docs/TESTING_GUIDE.md) for module-specific commands.

## Run all tests

From the repository root:

```bash
dotnet test ModularMonolith.sln
```

Build and test in one step:

```bash
dotnet test ModularMonolith.sln --no-build   # after dotnet build
```

## Run module-specific tests

```bash
dotnet test src/BuildingBlocks/BuildingBlocks.Application.Tests
dotnet test src/BuildingBlocks/BuildingBlocks.Infrastructure.Tests
dotnet test src/BuildingBlocks/BuildingBlocks.Web.Tests
dotnet test src/Modules/Identity/Identity.Application.Tests
dotnet test src/Modules/Categories/Categories.Infrastructure.Tests
dotnet test src/Modules/Files/Files.Application.Tests
dotnet test src/Modules/Files/Files.Infrastructure.Tests
dotnet test src/Modules/Notifications/Notifications.Application.Tests
dotnet test src/Modules/Notifications/Notifications.Infrastructure.Tests
dotnet test src/Modules/BackgroundJobs/BackgroundJobs.Application.Tests
dotnet test src/Modules/BackgroundJobs/BackgroundJobs.Infrastructure.Tests
dotnet test src/Modules/Monitoring/Monitoring.Infrastructure.Tests
dotnet test src/ApiHost/ApiHost.IntegrationTests
```

## Test types

### Unit tests

| Project | Focus |
|---------|-------|
| `BuildingBlocks.Application.Tests` | `Result`, `ResultStatusMapper`, business exceptions |
| `BuildingBlocks.Infrastructure.Tests` | Cache, monitoring sanitizers/tag matching, trace ID resolver |
| `BuildingBlocks.Web.Tests` | `BaseApiController` response helpers, `ApiExceptionResponseFactory`, `GlobalExceptionHandlingMiddleware` |
| `Identity.Application.Tests` | Login, refresh token invalid, inactive user blocked, permission cache post-commit |
| `Categories.Infrastructure.Tests` | Category CRUD rules, pagination, cache invalidation on create/update/delete, activity log enqueue |
| `Files.Application.Tests` | Upload validation rules |
| `Files.Infrastructure.Tests` | Upload service, download not found, mark permanent idempotency, soft delete, cache invalidation, compensation, temporary file cleanup |
| `Notifications.Application.Tests` | Template rendering |
| `Notifications.Infrastructure.Tests` | Notification ownership / IDOR, email template inactive guard, TestMode, send failure history, email retry batch logic |
| `BackgroundJobs.Application.Tests` | Manual job trigger success/failure semantics |
| `BackgroundJobs.Infrastructure.Tests` | Hangfire job attributes |
| `Monitoring.Infrastructure.Tests` | Redis/PostgreSQL config health checks |

### Integration tests

| Project | Focus |
|---------|-------|
| `ApiHost.IntegrationTests` | `/health/live`, `/health/ready`, monitoring auth, exception trace ID, business HTTP contracts (skipped when PostgreSQL/JWT required) |

Integration tests use `WebApplicationFactory<Program>` with:

- `ASPNETCORE_ENVIRONMENT=IntegrationTesting` (see `appsettings.IntegrationTesting.json`)
- In-memory config overrides (`BackgroundJobs:Enabled=false`, `Cache:Provider=Memory`)
- Startup migrations skipped (no PostgreSQL required for host boot)
- Hangfire hosted services removed from DI when present

They do **not** require Redis or PostgreSQL to start. `/health/ready` may return **503** when PostgreSQL is unavailable, but still includes dependency entries in the JSON payload.

## Shared test utilities

Reusable fakes live in `src/BuildingBlocks/BuildingBlocks.Testing/`:

- `FakeCurrentUserService`
- `FakeActivityLogService`
- `FixedDateTimeProvider`
- `RecordingCacheInvalidationBuffer`
- `RecordingCacheService`
- `TestDataFactory`

Module-local fakes (used only within one module) remain in module test projects:

- `Files.Infrastructure.Tests/Fakes/FakeFileStorageProvider`
- `Notifications.Infrastructure.Tests/Fakes/FakeEmailSender`

Prefer shared fakes over duplicating helpers inside module tests.

## Skipped tests

| Test | Reason |
|------|--------|
| `CategoryServiceTests.GetListAsync_WithKeyword_FiltersResults` | EF InMemory does not support PostgreSQL `ILike`; keyword search needs PostgreSQL or Testcontainers |
| `BusinessHttpIntegrationTests.CreateCategory_WhenDuplicateCode_Returns409` | Requires PostgreSQL, seeded admin user, and JWT |
| `BusinessHttpIntegrationTests.GetNotificationById_WhenCrossUserWithoutViewAll_Returns403` | Requires PostgreSQL, seeded users, and JWT |
| `BusinessHttpIntegrationTests.RunEmailRetryJob_WhenJobFails_Returns200WithFailedStatus` | Requires PostgreSQL, BackgroundJobs enabled, admin JWT, and job permissions |

Handler/service-level unit tests cover the same business rules without external dependencies.

## External dependencies

| Dependency | Required for | Notes |
|------------|--------------|-------|
| PostgreSQL | Full app runtime / skipped integration scenarios | Not required for most unit tests or host boot integration tests |
| Redis | Production cache | Tests use Memory/NoCache fakes |
| SMTP | Production email | Never used in tests; fake senders only |
| Hangfire server | Production background jobs | Disabled in integration test factory |

## Architecture notes

- `GlobalExceptionHandlingMiddleware` lives in `BuildingBlocks.Web` so `BuildingBlocks.Web.Tests` no longer references `ApiHost`.
- `BuildingBlocks.Web.Tests` tests middleware and controller helpers directly without booting the full host graph.

## Known remaining gaps

- PostgreSQL-backed integration suite (Testcontainers) for business HTTP contracts and DB unique-violation mapping (`Category.CodeAlreadyExists`).
- Category keyword search integration test (PostgreSQL `ILike`).
- Full auth login/refresh HTTP flow (handler-level tests cover core behavior).
- Activity log post-commit flush verification for Categories (enqueue is tested; flush hook not fully asserted).
- Unread-count cache invalidation for Notifications.
- BackgroundJobs `LogCleanupJob` logic.
- End-to-end Hangfire job execution (service-level job logic is unit-tested).

## Smoke checklist before Docker/CI-CD

Automated (run locally or in CI):

```bash
dotnet build ModularMonolith.sln
dotnet test ModularMonolith.sln
docker build -t modular-monolith-api:local .
```

Expected: build succeeds, **151 passed**, **4 skipped**, **0 failed**.

Docker local stack: see [DOCKER.md](./DOCKER.md). CI pipeline: see [CI.md](./CI.md).

Manual (when PostgreSQL and JWT are available):

- Login + refresh token against real PostgreSQL
- Category duplicate create returns HTTP 409
- Notification cross-user read returns HTTP 403
- Manual background job trigger returns HTTP 200 with `data.status = Failed` when job fails
- Stop PostgreSQL and verify `/health/ready` reflects dependency failure

## Conventions

Use descriptive test names:

```
MethodName_State_ExpectedResult
```

Examples:

- `CreateCategory_WhenCodeExists_ReturnsCodeAlreadyExists`
- `GetHealthDetails_WithoutToken_ReturnsUnauthorized`
- `OnRollbackAsync_DeletesTrackedPhysicalFile`

## Safety rules

- No real email is sent during tests.
- No secrets are committed in test projects.
- Temporary files created in tests must be cleaned up (file compensation tests use in-memory fake storage).
