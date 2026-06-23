# Post-Commit Hooks

Post-commit hooks implement `IPostCommitHook` (`BuildingBlocks.Application`).

## Why they exist

Some side effects must **not** run if the database transaction rolls back:

- Writing activity logs that imply success
- Applying cache invalidation
- Confirming file upload compensation (or rolling back orphaned files)

Handlers enqueue work during the request; hooks execute **after** all `IUnitOfWork.SaveChangesAsync` calls succeed.

## Registered hooks

| Hook | Module | On commit | On rollback |
|------|--------|-----------|-------------|
| `CacheInvalidationPostCommitHook` | BuildingBlocks | Flush cache set/remove operations | Clear buffer |
| `ActivityLogPostCommitHook` | AuditLogs | Flush pending activity logs | Clear pending |
| `FileStorageCompensationPostCommitHook` | Files | Clear compensation tracker | Delete orphaned uploaded files |

Registration: each module's `DependencyInjection.cs` + BuildingBlocks caching DI.

## TransactionBehavior flow

```
Handler executes
  → if Result.IsFailure → OnRollbackAsync (all hooks) → return failure (no save)
  → SaveChangesAsync on each IUnitOfWork
  → OnCommittedAsync (all hooks)
  → return success Result
```

On exception: `OnRollbackAsync` then rethrow.

## Before commit (inside transaction)

- Entity changes via EF
- Enqueue activity logs (`EnqueuePostCommitAsync`)
- Enqueue cache operations (`ICacheOperationBuffer`)
- Register file paths in compensation buffer

## After commit (in hooks)

- Persist activity logs
- Apply Redis/memory cache updates
- Finalize upload compensation state

## Must NOT happen inside transaction

- Sending email/SMTP (use post-commit or background job if needed after persist)
- Calling external APIs that cannot be rolled back
- Cache writes that should not appear if DB fails (use buffer + hook)

## Adding a new hook

1. Implement `IPostCommitHook` in appropriate Infrastructure project
2. Register as singleton or scoped (match existing hooks — typically scoped)
3. Use a buffer/enqueue pattern if work is collected during handler
4. Keep hook logic idempotent where possible
5. Add tests for commit vs rollback behavior

## Common pitfalls

| Pitfall | Consequence |
|---------|-------------|
| `LogImmediateAsync` for events tied to DB success | Activity log exists even if transaction fails |
| Direct cache remove in handler before save | Cache cleared but DB rollback leaves stale reads |
| Missing rollback handler | Orphan files or stale pending logs |
| Long-running work in hook | Blocks HTTP request — prefer Hangfire for heavy work |

## Related

- [ARCHITECTURE.md](./ARCHITECTURE.md) — TransactionBehavior
- [CACHING.md](./CACHING.md) — cache buffer
- [FILES.md](./FILES.md) — upload compensation
