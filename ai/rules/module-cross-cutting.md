# Cross-Cutting (Post-Commit)

**Sau commit (hooks):** ActivityLog flush, cache invalidation, file compensation clear.

```csharp
// ✅ Activity — enqueue, không log success trước commit
await _activityLogService.EnqueuePostCommitAsync(...);

// ✅ Cache — buffer, không remove trước commit
_cacheOperationBuffer.EnqueueRemove(CacheKeys.ProductDetail(id));
_cacheOperationBuffer.EnqueueRemoveByPrefix(CacheKeys.ProductListPrefix);

// ❌ Trước commit
await _cache.RemoveAsync(key);
await _activityLogService.LogImmediateAsync(...);
```

- AuditLog: EF change tracking trên entity chính
- ErrorCodes: `{Module}.{Reason}` trong `ErrorCodes.cs` — không hardcode string
- FluentValidation cho mọi write request

**Files:** dùng Files module; `ModuleName` / `ReferenceType` / `ReferenceId`; không trả `StoragePath`. Files hiện **permission-based only** — ownership enforce ở business layer nếu cần.

**Security:** không log/expose password, token, secret; enforce ownership/IDOR nếu entity thuộc user/tenant.

**Tests:** không real SMTP; default test không cần PostgreSQL/Redis.
