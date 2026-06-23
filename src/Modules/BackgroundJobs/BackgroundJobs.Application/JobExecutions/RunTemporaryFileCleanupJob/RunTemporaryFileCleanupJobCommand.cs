using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using BackgroundJobs.Application.Abstractions;
using BackgroundJobs.Application.Options;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace BackgroundJobs.Application.JobExecutions.RunTemporaryFileCleanupJob;

public sealed record RunTemporaryFileCleanupJobCommand(int? BatchSize = null) : ICommand<RunBackgroundJobResponse>;

public sealed class RunTemporaryFileCleanupJobCommandValidator : AbstractValidator<RunTemporaryFileCleanupJobCommand>
{
    public RunTemporaryFileCleanupJobCommandValidator(IOptions<BackgroundJobsOptions> options)
    {
        var maxBatchSize = options.Value.TemporaryFiles.BatchSize;

        RuleFor(command => command.BatchSize)
            .InclusiveBetween(1, maxBatchSize)
            .When(command => command.BatchSize.HasValue);
    }
}

public sealed class RunTemporaryFileCleanupJobCommandHandler
    : IRequestHandler<RunTemporaryFileCleanupJobCommand, Result<RunBackgroundJobResponse>>
{
    private readonly IBackgroundJobRunner _runner;
    private readonly ICurrentUserService _currentUserService;
    private readonly IActivityLogService _activityLogService;

    public RunTemporaryFileCleanupJobCommandHandler(
        IBackgroundJobRunner runner,
        ICurrentUserService currentUserService,
        IActivityLogService activityLogService)
    {
        _runner = runner;
        _currentUserService = currentUserService;
        _activityLogService = activityLogService;
    }

    public async Task<Result<RunBackgroundJobResponse>> Handle(
        RunTemporaryFileCleanupJobCommand request,
        CancellationToken cancellationToken)
    {
        var response = await _runner.RunTemporaryFileCleanupAsync(
            request.BatchSize,
            _currentUserService.UserName,
            cancellationToken);

        return await ManualJobTriggerSupport.CompleteAsync(
            response,
            _activityLogService,
            "Manual temporary file cleanup triggered.",
            cancellationToken);
    }
}
