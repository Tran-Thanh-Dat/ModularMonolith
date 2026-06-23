using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using BackgroundJobs.Application.Abstractions;
using BackgroundJobs.Application.JobExecutions;
using MediatR;

namespace BackgroundJobs.Application.JobExecutions.RunLogCleanupJob;

public sealed record RunLogCleanupJobCommand : ICommand<RunBackgroundJobResponse>;

public sealed class RunLogCleanupJobCommandHandler
    : IRequestHandler<RunLogCleanupJobCommand, Result<RunBackgroundJobResponse>>
{
    private readonly IBackgroundJobRunner _runner;
    private readonly ICurrentUserService _currentUserService;
    private readonly IActivityLogService _activityLogService;

    public RunLogCleanupJobCommandHandler(
        IBackgroundJobRunner runner,
        ICurrentUserService currentUserService,
        IActivityLogService activityLogService)
    {
        _runner = runner;
        _currentUserService = currentUserService;
        _activityLogService = activityLogService;
    }

    public async Task<Result<RunBackgroundJobResponse>> Handle(
        RunLogCleanupJobCommand request,
        CancellationToken cancellationToken)
    {
        var response = await _runner.RunLogCleanupAsync(
            _currentUserService.UserName,
            cancellationToken);

        return await ManualJobTriggerSupport.CompleteAsync(
            response,
            _activityLogService,
            "Manual log cleanup triggered.",
            cancellationToken);
    }
}
