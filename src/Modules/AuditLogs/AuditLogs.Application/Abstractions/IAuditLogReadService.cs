using BuildingBlocks.Application.Pagination;

namespace AuditLogs.Application.Abstractions;

public interface IAuditLogReadService
{
    Task<PagedResult<AuditLogs.GetAuditLogs.AuditLogListItemResponse>> GetAuditLogsAsync(
        string? keyword,
        string? moduleName,
        string? action,
        Guid? userId,
        string? userName,
        string? entityName,
        string? entityId,
        string? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AuditLogs.GetAuditLogById.AuditLogDetailResponse?> GetAuditLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
