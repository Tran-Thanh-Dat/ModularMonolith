using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.Abstractions;

namespace AuditLogs.Infrastructure.Services;

public sealed class ActivityLogPostCommitHook : IPostCommitHook
{
    private readonly IActivityLogService _activityLogService;

    public ActivityLogPostCommitHook(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    public Task OnCommittedAsync(CancellationToken cancellationToken = default) =>
        _activityLogService.FlushPendingAsync(cancellationToken);

    public Task OnRollbackAsync(CancellationToken cancellationToken = default)
    {
        _activityLogService.ClearPendingAsync(cancellationToken);
        return Task.CompletedTask;
    }
}
