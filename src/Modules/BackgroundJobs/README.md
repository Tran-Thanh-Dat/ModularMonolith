# BackgroundJobs Module

Reusable background job foundation using **Hangfire** with PostgreSQL storage, job execution history, and integration with Notifications (email retry) and Files (temporary file cleanup).

## Structure

```
BackgroundJobs/
├── BackgroundJobs.Domain/          # BackgroundJobExecution entity, constants
├── BackgroundJobs.Application/     # CQRS, options, abstractions, permissions
├── BackgroundJobs.Infrastructure/  # Hangfire, DbContext, jobs, services
└── BackgroundJobs.Api/             # REST API for executions and manual triggers
```

## Hangfire Setup

- Storage: **PostgreSQL** (`hangfire` schema), same connection string as the app
- Server: registered only when `BackgroundJobs:Enabled = true`
- Recurring jobs registered at startup via `AddOrUpdate` with stable IDs

### Recurring Job IDs

| ID | Job | Default cron |
|----|-----|--------------|
| `background-jobs:email-retry` | Email retry | `*/5 * * * *` |
| `background-jobs:temporary-file-cleanup` | Temporary file cleanup | `0 * * * *` |
| `background-jobs:log-cleanup` | Log cleanup (only if enabled) | `0 2 * * *` |

## Configuration

```json
"BackgroundJobs": {
  "Enabled": true,
  "Dashboard": {
    "Enabled": true,
    "Path": "/hangfire",
    "BasicAuth": {
      "Enabled": false,
      "UserName": "",
      "Password": ""
    }
  },
  "Schedules": {
    "EmailRetryCron": "*/5 * * * *",
    "TemporaryFileCleanupCron": "0 * * * *",
    "LogCleanupCron": "0 2 * * *"
  },
  "EmailRetry": {
    "BatchSize": 50,
    "MaxRetryCount": 5
  },
  "TemporaryFiles": {
    "BatchSize": 100,
    "DeletePhysicalFiles": true
  },
  "LogCleanup": {
    "Enabled": false,
    "RetentionDays": 90
  }
}
```

Set `Enabled: false` to disable Hangfire server, dashboard, and recurring registration.

## Dashboard Security

The Hangfire dashboard is **not** public:

1. **JWT + permission**: Authenticated user with `BackgroundJob.Dashboard` claim (browser access requires sending `Authorization: Bearer` header, e.g. via extension or API client).
2. **Optional Basic Auth**: Enable `Dashboard.BasicAuth` for browser access in production without JWT in the browser.

Do not expose `/hangfire` without one of these protections in production.

## Permissions

| Code | Purpose |
|------|---------|
| `BackgroundJob.View` | List/view job execution history |
| `BackgroundJob.Run` | Manual job triggers |
| `BackgroundJob.Dashboard` | Hangfire dashboard access |

Seeded via `PermissionCodes.All` and assigned to Admin / SuperAdmin roles.

## Unit of Work

- Services inject **`BackgroundJobsUnitOfWork`** (not plain `IUnitOfWork`).
- Registered as `IUnitOfWork` for `TransactionBehavior`.
- Hangfire jobs run **outside MediatR** and call `SaveChangesAsync` explicitly on affected module UoWs after each run.

## Job Execution History

`BackgroundJobExecution` records every run:

- Status: `Running`, `Succeeded`, `Failed`, `Skipped`
- Types: `EmailRetry`, `TemporaryFileCleanup`, `AuditLogCleanup`, etc.
- `Parameters` stored as JSONB; `ErrorDetails` for admin/debug (not exposed in list API)

## Email Retry Job

Processes `EmailMessage` rows where:

- `Status` is `Pending` or `Failed`
- `RetryCount < MaxRetryCount`
- `NextRetryAt` is null or `<= now`
- `IsDeleted = false`

On failure: exponential backoff (5 min base, capped at 120 min). Respects Email `TestMode` (skips when enabled without `RedirectTo`). Uses `IEmailRetryService` in Notifications module.

## Temporary File Cleanup Job

Processes `FileResource` where:

- `IsTemporary = true`
- `ExpiresAt <= now`
- `IsDeleted = false`

Soft-deletes metadata; optionally deletes physical files when `DeletePhysicalFiles = true` (best-effort). Never touches permanent files.

## Log Cleanup (Prepared, Disabled by Default)

`LogCleanup:Enabled` defaults to **false**. When enabled, the recurring job is registered but purge logic is a **skeleton** in this phase—no audit/activity logs are deleted until a future implementation.

## Manual Trigger APIs

All under `/api/v1/background-jobs`, JWT Bearer, `BaseApiController` responses:

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/executions` | `BackgroundJob.View` |
| GET | `/executions/{id}` | `BackgroundJob.View` |
| POST | `/email-retry/run` | `BackgroundJob.Run` |
| POST | `/temporary-files-cleanup/run` | `BackgroundJob.Run` |
| POST | `/log-cleanup/run` | `BackgroundJob.Run` |

Manual runs enqueue post-commit activity logs (`ModuleName: BackgroundJobs`).

### Trigger failure vs job execution failure

| Scenario | HTTP | `ApiResponse.success` | Execution record |
|----------|------|----------------------|------------------|
| **Trigger failure** — validation error, permission denied | 400/403 | `false` | Not created |
| **Job execution failure** — job ran and was marked Failed | 200 | `true` | Persisted (`Status = Failed`) |
| **Job skipped** — disabled config, log cleanup off | 200 | `true` | Persisted (`Status = Skipped`) |
| **Job succeeded** | 200 | `true` | Persisted (`Status = Succeeded`) |

When the trigger completes and an execution record is created, the API returns **`Result.Success`** so `TransactionBehavior` commits all unit-of-work changes. The job outcome is in `data.status` (`Succeeded`, `Failed`, or `Skipped`), not in `ApiResponse.success`.

Example when the job logic failed but history was saved:

```json
{
  "success": true,
  "code": "Common.Success",
  "message": "Success",
  "data": {
    "executionId": "...",
    "jobName": "Email Retry",
    "status": "Failed",
    "resultMessage": "Email retry job failed.",
    "errorMessage": "Email retry job failed."
  }
}
```

`ErrorDetails` and raw stack traces are **never** returned in manual trigger API responses.

### Disabled background jobs (manual trigger)

When `BackgroundJobs.Enabled = false`, manual triggers create a **Skipped** execution record with message `"Background jobs are disabled."` and return HTTP 200 with `data.status = Skipped`. This gives operators visibility in execution history.

Recurring Hangfire jobs also skip with a Skipped record when disabled (via the same runner logic).

## Physical File Purge (Future Work)

`PhysicalFilePurge` is defined as a job type constant but **not implemented** in this phase.

- **Implemented:** `TemporaryFileCleanup` — removes expired temporary files (`IsTemporary = true`, `ExpiresAt <= now`).
- **Deferred:** Physical purge of soft-deleted **permanent** files after a retention period. Do not enable until a dedicated job, config flag, and safety review are added.

Concurrent overlapping runs are prevented by Hangfire `[DisableConcurrentExecution]` on recurring jobs. App-level `BackgroundJob.AlreadyRunning` is reserved for a future phase if needed.

## Activity / Audit

- **Audit**: `BackgroundJobExecution` changes tracked via audit interceptor (`Modules.BackgroundJobs`).
- **Activity**: Post-commit on successful manual triggers; immediate `SystemError` on job infrastructure failures.
- Background jobs have no `HttpContext`; `CurrentUserService` falls back to system user.

## Migrations

```bash
dotnet ef migrations add AddBackgroundJobsModule \
  --project src/Modules/BackgroundJobs/BackgroundJobs.Infrastructure \
  --startup-project src/ApiHost \
  --context BackgroundJobsDbContext

dotnet ef database update \
  --project src/Modules/BackgroundJobs/BackgroundJobs.Infrastructure \
  --startup-project src/ApiHost \
  --context BackgroundJobsDbContext
```

Hangfire creates its own tables in the `hangfire` schema on first run.

## Smoke Test Checklist

- [ ] Solution builds (`dotnet build`)
- [ ] Migrations apply (Identity + BackgroundJobs + Hangfire schema)
- [ ] API starts with `BackgroundJobs:Enabled = true`
- [ ] Login as admin; permissions include `BackgroundJob.*`
- [ ] `GET /api/v1/background-jobs/executions` returns paged list
- [ ] `POST /api/v1/background-jobs/email-retry/run` — Succeeded creates execution record
- [ ] `POST /api/v1/background-jobs/email-retry/run` — Failed job returns HTTP 200, `data.status = Failed`, execution visible in list
- [ ] `POST /api/v1/background-jobs/temporary-files-cleanup/run` creates execution record
- [ ] `POST /api/v1/background-jobs/log-cleanup/run` with log cleanup disabled returns HTTP 200, `data.status = Skipped`
- [ ] Manual trigger with `BackgroundJobs.Enabled = false` returns Skipped execution record
- [ ] Execution list shows Failed and Skipped records
- [ ] `/hangfire` returns 401/403 without auth; accessible with `BackgroundJob.Dashboard` or Basic Auth
- [ ] Recurring jobs visible in Hangfire dashboard
- [ ] Existing Auth/Categories/Files/Notifications APIs still work

## Production Notes

- Keep `LogCleanup.Enabled = false` until purge is fully implemented.
- Use strong secrets for `Dashboard.BasicAuth` or rely on JWT + reverse-proxy restrictions.
- Tune cron and batch sizes for SMTP rate limits and storage I/O.
- Monitor `job_executions` for repeated failures.
- Do not expose SMTP passwords or file storage paths in API responses.
