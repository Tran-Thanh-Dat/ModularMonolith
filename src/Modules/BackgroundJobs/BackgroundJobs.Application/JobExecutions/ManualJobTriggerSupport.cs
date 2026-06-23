using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BackgroundJobs.Application.JobExecutions;
using BackgroundJobs.Domain.Constants;
using BuildingBlocks.Application.Results;

namespace BackgroundJobs.Application.JobExecutions;

internal static class ManualJobTriggerSupport
{
    public static async Task<Result<RunBackgroundJobResponse>> CompleteAsync(
        RunBackgroundJobResponse response,
        IActivityLogService activityLogService,
        string activityDescription,
        CancellationToken cancellationToken)
    {
        await activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            activityDescription,
            AuditLogConstants.Modules.BackgroundJobs,
            cancellationToken: cancellationToken);

        return Result<RunBackgroundJobResponse>.Success(response);
    }
}
