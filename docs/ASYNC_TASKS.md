# Async Tasks (Phase 27) — Message Queue Demo Module

> **Important:** This module is a **learning/demo module for RabbitMQ + MassTransit**. It does **not** replace [Hangfire](./BACKGROUND_JOBS.md). Existing scheduled/recurring jobs remain on Hangfire.

> **Scope:** Demo tasks only — simulated email, file processing, intentional failure, optional long-running progress. No complex real business logic.

> **Startup:** The API **must start when `MessageQueue:Enabled=false`** (default in `appsettings.json` and explicit in `appsettings.Production.json`). RabbitMQ is optional for local/dev.

## Purpose

Observe end-to-end async flow:

```
API submit → AsyncTask row (PENDING) → DB commit → publish message → QUEUED
→ consumer (PROCESSING) → demo processor → COMPLETED / FAILED
```

Best learning experience: open **RabbitMQ Management UI** (`http://localhost:15672`, guest/guest) while submitting tasks.

## Technology

| Component | Choice |
|-----------|--------|
| Broker | RabbitMQ (optional) |
| Integration | MassTransit 8.x |
| Process queue | `async-tasks.process` |
| Fault queue | `async-tasks.fault` (backup fault consumer) |
| Schema | `async_tasks.async_tasks` |
| API prefix | `/api/v1/async-tasks` |

## MassTransit endpoint topology (Phase 27.1)

**Pattern:** explicit receive endpoints only — **no** `ConfigureEndpoints(context)` (avoids duplicate consumer bindings).

| Endpoint | Consumer | Notes |
|----------|----------|-------|
| `async-tasks.process` | `ProcessAsyncTaskConsumer` | `UseMessageRetry` **before** `ConfigureConsumer` |
| `async-tasks.fault` | `ProcessAsyncTaskFaultConsumer` | Backup: handles published `Fault<ProcessAsyncTaskMessage>` |

**Failure strategy (defense in depth):**

1. Processor throws → MassTransit retries per config (`RetryCount` × `IntervalSeconds`).
2. On final retry attempt (`GetRetryAttempt() >= RetryCount`), consumer calls `HandleFaultAsync` → status **FAILED** (message acked, no stuck PROCESSING).
3. If MassTransit still publishes a `Fault<T>`, `ProcessAsyncTaskFaultConsumer` marks FAILED (idempotent skip if already terminal).

## Configuration

```json
"MessageQueue": {
  "Enabled": false,
  "Provider": "RabbitMQ",
  "RabbitMQ": {
    "Host": "localhost",
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "Port": 5672,
    "UseSsl": false
  },
  "Retry": {
    "RetryCount": 3,
    "IntervalSeconds": 5
  }
}
```

- **`Enabled: false`** — No MassTransit bus; `NoOpAsyncTaskQueuePublisher`; submit returns `400` with `AsyncTask.MessageQueueDisabled`.
- **`Enabled: true`** — MassTransit consumer registered; `/health/ready` includes `rabbitmq` check.
- **Production** — `appsettings.Production.json` sets `MessageQueue:Enabled: false` explicitly. Do not use guest/guest in production; use environment/secrets if enabling RabbitMQ.
- **Development** — `appsettings.Development.json` may set `Enabled: true` for local RabbitMQ.

## Local smoke test (required for Phase 27 PASS)

**Automated (no RabbitMQ):** MassTransit in-memory harness tests in `AsyncTasksMassTransitIntegrationTests`.

**Broker E2E (RabbitMQ):** run script after starting ApiHost (Development):

```powershell
docker compose up -d rabbitmq
dotnet run --project src/ApiHost
# separate terminal:
.\scripts\smoke-async-tasks.ps1
```

### Manual steps

```bash
docker compose up -d rabbitmq
```

Management UI: http://localhost:15672 (guest/guest)

### 2. Start API

Ensure `MessageQueue:Enabled=true` (Development profile) and PostgreSQL is running. Start ApiHost (F5 or `dotnet run --project src/ApiHost`).

### 3. Health checks

```http
GET /health/live    → 200
GET /health/ready   → 200 (rabbitmq healthy when broker up)
```

### 4. Email demo (success path)

```http
POST /api/v1/async-tasks/email-demo
Authorization: Bearer {token with AsyncTask.Submit}
Content-Type: application/json

{ "emailTo": "demo@example.com", "subject": "Hello", "body": "Test" }
```

Poll `GET /api/v1/async-tasks/{id}` until status: **QUEUED → PROCESSING → COMPLETED**.

RabbitMQ UI: queue `async-tasks.process` shows publish/consume.

### 5. Fail demo (retry → FAILED)

```http
POST /api/v1/async-tasks/fail-demo
{ "shouldAlwaysFail": true, "failReason": "demo failure" }
```

Wait ~`RetryCount × IntervalSeconds` (default 3 × 5s ≈ 15s after first attempt). Poll until status **FAILED**, `failedAt` and `lastErrorMessage` set.

### 6. Fail demo (success path)

```http
POST /api/v1/async-tasks/fail-demo
{ "shouldAlwaysFail": false }
```

Expected: **COMPLETED** (processor skips throw).

### 7. Queue disabled

Set `MessageQueue:Enabled=false`, restart app.

- App must start.
- Submit task → `400` / `AsyncTask.MessageQueueDisabled`.

### 8. RabbitMQ down + Enabled=true

Stop RabbitMQ container. App should start (per convention); `/health/ready` → unhealthy/degraded for `rabbitmq`.

## Demo task types

| Type | Behavior |
|------|----------|
| `EMAIL_DEMO` | Simulated email send (~1s delay) → COMPLETED |
| `FILE_PROCESSING_DEMO` | Simulated file processing (~2s) → COMPLETED |
| `FAIL_DEMO` | `shouldAlwaysFail:true` → retry → FAILED; `false` → COMPLETED |
| `LONG_RUNNING_DEMO` | Updates progress 0–100 → COMPLETED |

## Status lifecycle

`PENDING` → `QUEUED` (after publish) → `PROCESSING` → `COMPLETED` | `FAILED` | `CANCELLED`

## Producer flow

1. API command creates `AsyncTask` with status **PENDING**.
2. Message enqueued in `IAsyncTaskPublishBuffer` (same request scope).
3. After **transaction commit**, `AsyncTaskPublishPostCommitHook` publishes via MassTransit.
4. On success → `MarkQueued`; on publish failure → `MarkFailed` (`AsyncTask.PublishFailed`).

## Consumer flow

1. `ProcessAsyncTaskConsumer` receives `ProcessAsyncTaskMessage`.
2. **Idempotency:** skip if task is `COMPLETED`, `CANCELLED`, or `FAILED`.
3. `MarkProcessing` + dispatch to processor registry.
4. Success → `MarkCompleted`; processor exception → retry or final `HandleFaultAsync`.
5. Task in `PROCESSING` on retry is **re-processed** (by design for MassTransit retry).

## Permissions

| Permission | Use |
|------------|-----|
| `AsyncTask.View` | List / detail |
| `AsyncTask.Submit` | Submit demo tasks |
| `AsyncTask.Cancel` | Cancel pending/queued/processing |
| `AsyncTask.Retry` | Retry failed task |
| `AsyncTask.Manage` | Admin (reserved) |

## Hangfire coexistence

| Concern | Hangfire | AsyncTasks |
|---------|----------|------------|
| Purpose | Production background jobs | Queue learning demo |
| Required at startup | Per `BackgroundJobs:Enabled` | Only when `MessageQueue:Enabled=true` |

## Phase 28 readiness

Message contract, post-commit publish, status tracking, retry/failure, enable/disable config — foundation for Kafka integration events in Phase 28.
