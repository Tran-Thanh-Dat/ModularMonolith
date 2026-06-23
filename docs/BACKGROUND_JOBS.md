# Background Jobs

Hangfire-based recurring and manual background jobs.

## Setup

- Storage: PostgreSQL schema `hangfire`
- Execution history: `background_jobs.job_executions` (`BackgroundJobExecution`)
- Config section: `BackgroundJobs`

```json
{
  "BackgroundJobs": {
    "Enabled": true,
    "Dashboard": {
      "Enabled": false,
      "Path": "/hangfire"
    },
    "Schedules": {
      "EmailRetryCron": "*/5 * * * *",
      "TemporaryFileCleanupCron": "0 * * * *",
      "LogCleanupCron": "0 2 * * *"
    },
    "LogCleanup": {
      "Enabled": false
    }
  }
}
```

Set `BackgroundJobs:Enabled=false` in integration tests (no Hangfire server).

## Recurring job IDs

| ID | Job | Default schedule |
|----|-----|------------------|
| `background-jobs:email-retry` | `EmailRetryJob` | Every 5 minutes |
| `background-jobs:temporary-file-cleanup` | `TemporaryFileCleanupJob` | Hourly |
| `background-jobs:log-cleanup` | `LogCleanupJob` | Daily 02:00 (only if enabled) |

When `LogCleanup.Enabled=false`, the recurring registration is removed.

## Future / backlog job types

`BackgroundJobTypes.PhysicalFilePurge` exists as a constant but **no recurring or manual job is registered** for it today. Physical file deletion is handled within `TemporaryFileCleanupJob` when `DeletePhysicalFiles` is enabled — not as a separate purge job.

## Dashboard

- Path: `BackgroundJobs:Dashboard:Path` (default `/hangfire`)
- Disabled by default — enable in Docker/Dev with Basic Auth for non-Production
- Requires permission `BackgroundJob.Dashboard` via `HangfireDashboardAuthorizationFilter`
- **Production:** keep disabled or protect with auth + network restrictions — see [../PRODUCTION.md](../PRODUCTION.md)

## Manual trigger APIs

Base route: `/api/v1/background-jobs`

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/executions` | `BackgroundJob.View` |
| GET | `/executions/{id}` | `BackgroundJob.View` |
| POST | `/email-retry/run` | `BackgroundJob.Run` |
| POST | `/temporary-files-cleanup/run` | `BackgroundJob.Run` |
| POST | `/log-cleanup/run` | `BackgroundJob.Run` |

Manual runs create `BackgroundJobExecution` records with `TriggerSource=Manual`.

## Job failure vs trigger failure

- **Trigger API success (HTTP 200)** — the API accepted and started the job; response includes execution id/status.
- **Job failure** — recorded on `BackgroundJobExecution` with `status=Failed`; HTTP may still be 200 with `data.status=Failed`.
- **Trigger failure** — validation/permission errors return 400/403 before job starts.

## Docker behavior

Docker profile enables Hangfire when PostgreSQL is available. Dashboard may be enabled in `appsettings.Docker.json` — verify before exposing publicly.

## Related modules

- **Notifications** — email retry batch processing
- **Files** — temporary file cleanup, optional physical purge
- **AuditLogs** — log cleanup (disabled by default)

See [FILES.md](./FILES.md), [NOTIFICATIONS.md](./NOTIFICATIONS.md), [MONITORING.md](./MONITORING.md).
