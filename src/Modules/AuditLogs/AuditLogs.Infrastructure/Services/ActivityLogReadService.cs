using AuditLogs.Application.Abstractions;
using AuditLogs.Application.ActivityLogs.GetActivityLogById;
using AuditLogs.Application.ActivityLogs.GetActivityLogs;
using AuditLogs.Infrastructure.Persistence;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AuditLogs.Infrastructure.Services;

public sealed class ActivityLogReadService : IActivityLogReadService
{
    private readonly AuditLogsDbContext _dbContext;

    public ActivityLogReadService(AuditLogsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ActivityLogListItemResponse>> GetActivityLogsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _dbContext.ActivityLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(activityLog =>
                EF.Functions.ILike(activityLog.UserName ?? string.Empty, pattern) ||
                EF.Functions.ILike(activityLog.ModuleName, pattern) ||
                EF.Functions.ILike(activityLog.ActivityType, pattern) ||
                EF.Functions.ILike(activityLog.Description, pattern) ||
                EF.Functions.ILike(activityLog.RequestPath ?? string.Empty, pattern));
        }

        if (!string.IsNullOrWhiteSpace(activityType))
        {
            query = query.Where(activityLog => activityLog.ActivityType == activityType);
        }

        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            query = query.Where(activityLog => activityLog.ModuleName == moduleName);
        }

        if (userId.HasValue)
        {
            query = query.Where(activityLog => activityLog.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var userNamePattern = $"%{userName.Trim()}%";
            query = query.Where(activityLog =>
                EF.Functions.ILike(activityLog.UserName ?? string.Empty, userNamePattern));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(activityLog => activityLog.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(activityLog => activityLog.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(activityLog => activityLog.CreatedAt <= toDate.Value);
        }

        var projected = query
            .OrderByDescending(activityLog => activityLog.CreatedAt)
            .Select(activityLog => new ActivityLogListItemResponse
            {
                Id = activityLog.Id,
                UserId = activityLog.UserId,
                UserName = activityLog.UserName,
                ActivityType = activityLog.ActivityType,
                Description = activityLog.Description,
                ModuleName = activityLog.ModuleName,
                Status = activityLog.Status,
                CreatedAt = activityLog.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    public async Task<ActivityLogDetailResponse?> GetActivityLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var activityLog = await _dbContext.ActivityLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);

        if (activityLog is null)
        {
            return null;
        }

        return new ActivityLogDetailResponse
        {
            Id = activityLog.Id,
            UserId = activityLog.UserId,
            UserName = activityLog.UserName,
            ActivityType = activityLog.ActivityType,
            Description = activityLog.Description,
            ModuleName = activityLog.ModuleName,
            RequestPath = activityLog.RequestPath,
            HttpMethod = activityLog.HttpMethod,
            IpAddress = activityLog.IpAddress,
            UserAgent = activityLog.UserAgent,
            Status = activityLog.Status,
            ErrorMessage = activityLog.ErrorMessage,
            CreatedAt = activityLog.CreatedAt
        };
    }
}
