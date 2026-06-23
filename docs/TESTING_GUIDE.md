# Testing Guide

Developer guide for running and extending tests. Root reference: [../TESTING.md](../TESTING.md).

## Current baseline

Run `dotnet test ModularMonolith.sln` — expect **151 passed**, **4 skipped**, **0 failed** across **13 test projects**.

| Metric | Value |
|--------|-------|
| Test projects | 13 |
| Skipped | 4 (documented below) |
| External services | Not required for default CI run |

> Update counts after adding tests. CI gate: build + test must pass.

## Run all tests

```bash
dotnet test ModularMonolith.sln
```

Release verification:

```bash
dotnet build ModularMonolith.sln -c Release
dotnet test ModularMonolith.sln -c Release --no-build
```

## Module-specific tests

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

## Unit vs integration

| Type | Location | Needs PostgreSQL? |
|------|----------|-------------------|
| Unit | Module + BuildingBlocks `*.Tests` | No |
| Integration | `ApiHost.IntegrationTests` | No for host boot; yes for skipped business HTTP tests |

Integration factory uses `ASPNETCORE_ENVIRONMENT=IntegrationTesting`, in-memory cache, Hangfire disabled.

## Skipped tests (why)

| Test | Reason |
|------|--------|
| `CategoryServiceTests.GetListAsync_WithKeyword_FiltersResults` | EF InMemory lacks PostgreSQL `ILike` |
| `CreateCategory_WhenDuplicateCode_Returns409` | Needs PostgreSQL + JWT |
| `GetNotificationById_WhenCrossUserWithoutViewAll_Returns403` | Needs PostgreSQL + seeded users + JWT |
| `RunEmailRetryJob_WhenJobFails_Returns200WithFailedStatus` | Needs PostgreSQL + Hangfire + admin JWT |

**Backlog:** Testcontainers PostgreSQL suite to enable these.

## Enabling PostgreSQL-dependent tests later

1. Add Testcontainers PostgreSQL fixture to integration project
2. Set real connection string in test factory
3. Seed admin user and obtain JWT in test setup
4. Remove `[Fact(Skip = ...)]` attributes

## Fake services

Shared fakes: `src/BuildingBlocks/BuildingBlocks.Testing/`

- `FakeCurrentUserService`
- `FakeActivityLogService`
- `RecordingCacheInvalidationBuffer`
- `RecordingCacheService`

Module fakes stay in module test projects (e.g. `FakeEmailSender`, `FakeFileStorageProvider`).

## No real SMTP policy

Tests **never** send real email. Use `FakeEmailSender` or infrastructure fakes.

## CI test gate

GitHub Actions runs `dotnet build` + `dotnet test` — see [../CI.md](../CI.md).

## Conventions

Test naming: `MethodName_State_ExpectedResult`

Example: `CreateCategory_WhenCodeExists_ReturnsCodeAlreadyExists`
