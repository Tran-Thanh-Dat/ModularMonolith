using AuditLogs.Application.ActivityLogs.GetActivityLogById;
using AuditLogs.Application.ActivityLogs.GetActivityLogs;
using BuildingBlocks.Application.Pagination;

namespace AuditLogs.Application.Abstractions;

public interface IActivityLogReadService
{
    Task<PagedResult<ActivityLogListItemResponse>> GetActivityLogsAsync(
        string? keyword,
        string? activityType,
        string? moduleName,
        Guid? userId,
        string? userName,
        string? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ActivityLogDetailResponse?> GetActivityLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
