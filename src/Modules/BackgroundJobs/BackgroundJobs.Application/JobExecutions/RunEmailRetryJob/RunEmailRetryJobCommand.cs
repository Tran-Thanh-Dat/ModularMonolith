using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using BackgroundJobs.Application.Abstractions;
using BackgroundJobs.Application.Options;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace BackgroundJobs.Application.JobExecutions.RunEmailRetryJob;

public sealed record RunEmailRetryJobCommand(int? BatchSize = null) : ICommand<RunBackgroundJobResponse>;

public sealed class RunEmailRetryJobCommandValidator : AbstractValidator<RunEmailRetryJobCommand>
{
    public RunEmailRetryJobCommandValidator(IOptions<BackgroundJobsOptions> options)
    {
        var maxBatchSize = options.Value.EmailRetry.BatchSize;

        RuleFor(command => command.BatchSize)
            .InclusiveBetween(1, maxBatchSize)
            .When(command => command.BatchSize.HasValue);
    }
}

public sealed class RunEmailRetryJobCommandHandler
    : IRequestHandler<RunEmailRetryJobCommand, Result<RunBackgroundJobResponse>>
{
    private readonly IBackgroundJobRunner _runner;
    private readonly ICurrentUserService _currentUserService;
    private readonly IActivityLogService _activityLogService;

    public RunEmailRetryJobCommandHandler(
        IBackgroundJobRunner runner,
        ICurrentUserService currentUserService,
        IActivityLogService activityLogService)
    {
        _runner = runner;
        _currentUserService = currentUserService;
        _activityLogService = activityLogService;
    }

    public async Task<Result<RunBackgroundJobResponse>> Handle(
        RunEmailRetryJobCommand request,
        CancellationToken cancellationToken)
    {
        var response = await _runner.RunEmailRetryAsync(
            request.BatchSize,
            _currentUserService.UserName,
            cancellationToken);

        return await ManualJobTriggerSupport.CompleteAsync(
            response,
            _activityLogService,
            "Manual email retry job triggered.",
            cancellationToken);
    }
}
