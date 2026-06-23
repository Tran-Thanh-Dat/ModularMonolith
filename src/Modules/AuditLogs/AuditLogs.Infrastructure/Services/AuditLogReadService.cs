using AuditLogs.Application.Abstractions;
using AuditLogs.Application.AuditLogs.GetAuditLogById;
using AuditLogs.Application.AuditLogs.GetAuditLogs;
using AuditLogs.Infrastructure.Persistence;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AuditLogs.Infrastructure.Services;

public sealed class AuditLogReadService : IAuditLogReadService
{
    private readonly AuditLogsDbContext _dbContext;

    public AuditLogReadService(AuditLogsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AuditLogListItemResponse>> GetAuditLogsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(auditLog =>
                EF.Functions.ILike(auditLog.UserName ?? string.Empty, pattern) ||
                EF.Functions.ILike(auditLog.ModuleName, pattern) ||
                EF.Functions.ILike(auditLog.Action, pattern) ||
                EF.Functions.ILike(auditLog.EntityName ?? string.Empty, pattern) ||
                EF.Functions.ILike(auditLog.EntityId ?? string.Empty, pattern) ||
                EF.Functions.ILike(auditLog.RequestPath ?? string.Empty, pattern));
        }

        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            query = query.Where(auditLog => auditLog.ModuleName == moduleName);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(auditLog => auditLog.Action == action);
        }

        if (userId.HasValue)
        {
            query = query.Where(auditLog => auditLog.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var userNamePattern = $"%{userName.Trim()}%";
            query = query.Where(auditLog =>
                EF.Functions.ILike(auditLog.UserName ?? string.Empty, userNamePattern));
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(auditLog => auditLog.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(auditLog => auditLog.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(auditLog => auditLog.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(auditLog => auditLog.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(auditLog => auditLog.CreatedAt <= toDate.Value);
        }

        var projected = query
            .OrderByDescending(auditLog => auditLog.CreatedAt)
            .Select(auditLog => new AuditLogListItemResponse
            {
                Id = auditLog.Id,
                UserId = auditLog.UserId,
                UserName = auditLog.UserName,
                Action = auditLog.Action,
                EntityName = auditLog.EntityName,
                EntityId = auditLog.EntityId,
                ModuleName = auditLog.ModuleName,
                Status = auditLog.Status,
                CreatedAt = auditLog.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    public async Task<AuditLogDetailResponse?> GetAuditLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var auditLog = await _dbContext.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);

        if (auditLog is null)
        {
            return null;
        }

        return new AuditLogDetailResponse
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            UserName = auditLog.UserName,
            Action = auditLog.Action,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            ModuleName = auditLog.ModuleName,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            ChangedColumns = auditLog.ChangedColumns,
            RequestPath = auditLog.RequestPath,
            HttpMethod = auditLog.HttpMethod,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            Status = auditLog.Status,
            ErrorMessage = auditLog.ErrorMessage,
            CreatedAt = auditLog.CreatedAt
        };
    }
}
