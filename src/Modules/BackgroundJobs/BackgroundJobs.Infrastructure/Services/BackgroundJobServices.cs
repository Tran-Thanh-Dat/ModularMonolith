using System.Text.Json;
using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using Files.Application.Abstractions;
using Notifications.Application.Abstractions;
using BackgroundJobs.Application.Abstractions;
using BackgroundJobs.Application.JobExecutions;
using BackgroundJobs.Application.Options;
using BackgroundJobs.Domain.Constants;
using BackgroundJobs.Domain.JobExecutions;
using BackgroundJobs.Infrastructure.Persistence;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackgroundJobs.Infrastructure.Services;

public sealed class BackgroundJobExecutionService : IBackgroundJobExecutionService
{
    private readonly BackgroundJobsUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<BackgroundJobExecutionService> _logger;

    public BackgroundJobExecutionService(
        BackgroundJobsUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ILogger<BackgroundJobExecutionService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Guid> StartAsync(
        string jobName,
        string jobType,
        string? triggeredBy,
        string triggerSource,
        string? parameters,
        CancellationToken cancellationToken = default)
    {
        var execution = BackgroundJobExecution.Start(
            jobName,
            jobType,
            triggeredBy,
            triggerSource,
            parameters,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<BackgroundJobExecution, Guid>()
            .AddAsync(execution, cancellationToken);

        _logger.LogInformation(
            "Started background job execution {ExecutionId} {JobName}",
            execution.Id,
            execution.JobName);

        return execution.Id;
    }

    public async Task MarkSucceededAsync(
        Guid executionId,
        string? resultMessage,
        CancellationToken cancellationToken = default)
    {
        var execution = await GetExecutionForUpdateAsync(executionId, cancellationToken);
        execution.MarkSucceeded(resultMessage, _dateTimeProvider.UtcNow);
    }

    public async Task MarkFailedAsync(
        Guid executionId,
        string? errorMessage,
        string? errorDetails,
        CancellationToken cancellationToken = default)
    {
        var execution = await GetExecutionForUpdateAsync(executionId, cancellationToken);
        execution.MarkFailed(errorMessage, errorDetails, _dateTimeProvider.UtcNow);
    }

    public async Task MarkSkippedAsync(
        Guid executionId,
        string? resultMessage,
        CancellationToken cancellationToken = default)
    {
        var execution = await GetExecutionForUpdateAsync(executionId, cancellationToken);
        execution.MarkSkipped(resultMessage, _dateTimeProvider.UtcNow);
    }

    public async Task<BackgroundJobExecutionDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<BackgroundJobExecution, Guid>()
            .QueryReadOnly()
            .Where(execution => execution.Id == id && !execution.IsDeleted)
            .Select(execution => new BackgroundJobExecutionDetailResponse
            {
                Id = execution.Id,
                JobName = execution.JobName,
                JobType = execution.JobType,
                Status = execution.Status,
                StartedAt = execution.StartedAt,
                FinishedAt = execution.FinishedAt,
                DurationMs = execution.DurationMs,
                TriggeredBy = execution.TriggeredBy,
                TriggerSource = execution.TriggerSource,
                Parameters = execution.Parameters,
                ResultMessage = execution.ResultMessage,
                ErrorMessage = execution.ErrorMessage,
                CreatedAt = execution.CreatedAt,
                CreatedBy = execution.CreatedBy,
                UpdatedAt = execution.UpdatedAt,
                UpdatedBy = execution.UpdatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<BackgroundJobExecutionListItemResponse>> GetListAsync(
        string? keyword,
        string? jobName,
        string? jobType,
        string? status,
        string? triggerSource,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<BackgroundJobExecution, Guid>()
            .QueryReadOnly()
            .Where(execution => !execution.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(execution =>
                EF.Functions.ILike(execution.JobName, pattern) ||
                EF.Functions.ILike(execution.ResultMessage ?? string.Empty, pattern));
        }

        if (!string.IsNullOrWhiteSpace(jobName))
        {
            query = query.Where(execution => execution.JobName == jobName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(jobType))
        {
            query = query.Where(execution => execution.JobType == jobType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(execution => execution.Status == status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(triggerSource))
        {
            query = query.Where(execution => execution.TriggerSource == triggerSource.Trim());
        }

        if (fromDate.HasValue)
        {
            query = query.Where(execution => execution.StartedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(execution => execution.StartedAt <= toDate.Value);
        }

        var projected = query
            .OrderByDescending(execution => execution.StartedAt)
            .Select(execution => new BackgroundJobExecutionListItemResponse
            {
                Id = execution.Id,
                JobName = execution.JobName,
                JobType = execution.JobType,
                Status = execution.Status,
                StartedAt = execution.StartedAt,
                FinishedAt = execution.FinishedAt,
                DurationMs = execution.DurationMs,
                TriggeredBy = execution.TriggeredBy,
                TriggerSource = execution.TriggerSource,
                ResultMessage = execution.ResultMessage,
                CreatedAt = execution.CreatedAt
            });

        return await projected.ToPagedResultAsync(
            new PagedRequest { PageIndex = pageIndex, PageSize = pageSize },
            cancellationToken);
    }

    private async Task<BackgroundJobExecution> GetExecutionForUpdateAsync(
        Guid executionId,
        CancellationToken cancellationToken)
    {
        var execution = await _unitOfWork.Repository<BackgroundJobExecution, Guid>()
            .Query()
            .FirstOrDefaultAsync(item => item.Id == executionId && !item.IsDeleted, cancellationToken);

        if (execution is null)
        {
            throw new InvalidOperationException($"Background job execution '{executionId}' was not found.");
        }

        return execution;
    }
}

public sealed class BackgroundJobRunner : IBackgroundJobRunner
{
    private readonly IBackgroundJobExecutionService _executionService;
    private readonly IEmailRetryService _emailRetryService;
    private readonly ITemporaryFileCleanupService _temporaryFileCleanupService;
    private readonly ILogCleanupService _logCleanupService;
    private readonly IActivityLogService _activityLogService;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<BackgroundJobRunner> _logger;

    public BackgroundJobRunner(
        IBackgroundJobExecutionService executionService,
        IEmailRetryService emailRetryService,
        ITemporaryFileCleanupService temporaryFileCleanupService,
        ILogCleanupService logCleanupService,
        IActivityLogService activityLogService,
        IOptions<BackgroundJobsOptions> options,
        ILogger<BackgroundJobRunner> logger)
    {
        _executionService = executionService;
        _emailRetryService = emailRetryService;
        _temporaryFileCleanupService = temporaryFileCleanupService;
        _logCleanupService = logCleanupService;
        _activityLogService = activityLogService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RunBackgroundJobResponse> RunEmailRetryAsync(
        int? batchSize,
        string? triggeredBy,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await CreateSkippedResponseAsync(
                "Email Retry",
                BackgroundJobTypes.EmailRetry,
                triggeredBy,
                BackgroundJobTriggerSources.Manual,
                "Background jobs are disabled.",
                cancellationToken);
        }

        var effectiveBatchSize = batchSize ?? _options.EmailRetry.BatchSize;
        var parameters = JsonSerializer.Serialize(new
        {
            batchSize = effectiveBatchSize,
            maxRetryCount = _options.EmailRetry.MaxRetryCount
        });

        var executionId = await _executionService.StartAsync(
            "Email Retry",
            BackgroundJobTypes.EmailRetry,
            triggeredBy,
            string.IsNullOrWhiteSpace(triggeredBy)
                ? BackgroundJobTriggerSources.Recurring
                : BackgroundJobTriggerSources.Manual,
            parameters,
            cancellationToken);

        try
        {
            var result = await _emailRetryService.ProcessBatchAsync(
                effectiveBatchSize,
                _options.EmailRetry.MaxRetryCount,
                cancellationToken);

            await _executionService.MarkSucceededAsync(executionId, result.Summary, cancellationToken);

            return new RunBackgroundJobResponse
            {
                ExecutionId = executionId,
                JobName = "Email Retry",
                Status = BackgroundJobStatuses.Succeeded,
                ResultMessage = result.Summary,
                ProcessedCount = result.ProcessedCount,
                SuccessCount = result.SuccessCount,
                FailedCount = result.FailedCount,
                SkippedCount = result.SkippedCount
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Email retry job failed.");

            await _executionService.MarkFailedAsync(
                executionId,
                "Email retry job failed.",
                exception.ToString(),
                cancellationToken);

            await _activityLogService.LogImmediateAsync(
                ActivityTypes.SystemError,
                "Email retry job failed.",
                AuditLogConstants.Modules.BackgroundJobs,
                "Failed",
                "Email retry job failed.",
                cancellationToken: cancellationToken);

            return new RunBackgroundJobResponse
            {
                ExecutionId = executionId,
                JobName = "Email Retry",
                Status = BackgroundJobStatuses.Failed,
                ResultMessage = "Email retry job failed.",
                ErrorMessage = "Email retry job failed."
            };
        }
    }

    public async Task<RunBackgroundJobResponse> RunTemporaryFileCleanupAsync(
        int? batchSize,
        string? triggeredBy = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await CreateSkippedResponseAsync(
                "Temporary File Cleanup",
                BackgroundJobTypes.TemporaryFileCleanup,
                triggeredBy,
                string.IsNullOrWhiteSpace(triggeredBy)
                    ? BackgroundJobTriggerSources.Recurring
                    : BackgroundJobTriggerSources.Manual,
                "Background jobs are disabled.",
                cancellationToken);
        }

        var effectiveBatchSize = batchSize ?? _options.TemporaryFiles.BatchSize;
        var parameters = JsonSerializer.Serialize(new
        {
            batchSize = effectiveBatchSize,
            deletePhysicalFiles = _options.TemporaryFiles.DeletePhysicalFiles
        });

        var executionId = await _executionService.StartAsync(
            "Temporary File Cleanup",
            BackgroundJobTypes.TemporaryFileCleanup,
            triggeredBy,
            string.IsNullOrWhiteSpace(triggeredBy)
                ? BackgroundJobTriggerSources.Recurring
                : BackgroundJobTriggerSources.Manual,
            parameters,
            cancellationToken);

        try
        {
            var result = await _temporaryFileCleanupService.ProcessBatchAsync(
                effectiveBatchSize,
                _options.TemporaryFiles.DeletePhysicalFiles,
                cancellationToken);

            await _executionService.MarkSucceededAsync(executionId, result.Summary, cancellationToken);

            return new RunBackgroundJobResponse
            {
                ExecutionId = executionId,
                JobName = "Temporary File Cleanup",
                Status = BackgroundJobStatuses.Succeeded,
                ResultMessage = result.Summary,
                ProcessedCount = result.ProcessedCount,
                SuccessCount = result.SuccessCount,
                FailedCount = result.FailedCount,
                SkippedCount = result.SkippedCount
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Temporary file cleanup job failed.");

            await _executionService.MarkFailedAsync(
                executionId,
                "Temporary file cleanup job failed.",
                exception.ToString(),
                cancellationToken);

            await _activityLogService.LogImmediateAsync(
                ActivityTypes.SystemError,
                "Temporary file cleanup job failed.",
                AuditLogConstants.Modules.BackgroundJobs,
                "Failed",
                "Temporary file cleanup job failed.",
                cancellationToken: cancellationToken);

            return new RunBackgroundJobResponse
            {
                ExecutionId = executionId,
                JobName = "Temporary File Cleanup",
                Status = BackgroundJobStatuses.Failed,
                ResultMessage = "Temporary file cleanup job failed.",
                ErrorMessage = "Temporary file cleanup job failed."
            };
        }
    }

    public async Task<RunBackgroundJobResponse> RunLogCleanupAsync(
        string? triggeredBy = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await CreateSkippedResponseAsync(
                "Log Cleanup",
                BackgroundJobTypes.AuditLogCleanup,
                triggeredBy,
                string.IsNullOrWhiteSpace(triggeredBy)
                    ? BackgroundJobTriggerSources.Recurring
                    : BackgroundJobTriggerSources.Manual,
                "Background jobs are disabled.",
                cancellationToken);
        }

        var parameters = JsonSerializer.Serialize(new
        {
            enabled = _options.LogCleanup.Enabled,
            retentionDays = _options.LogCleanup.RetentionDays
        });

        var executionId = await _executionService.StartAsync(
            "Log Cleanup",
            BackgroundJobTypes.AuditLogCleanup,
            triggeredBy,
            string.IsNullOrWhiteSpace(triggeredBy)
                ? BackgroundJobTriggerSources.Recurring
                : BackgroundJobTriggerSources.Manual,
            parameters,
            cancellationToken);

        try
        {
            var result = await _logCleanupService.ProcessAsync(
                _options.LogCleanup.Enabled,
                _options.LogCleanup.RetentionDays,
                cancellationToken);

            if (result.WasSkipped)
            {
                await _executionService.MarkSkippedAsync(executionId, result.Summary, cancellationToken);

                return new RunBackgroundJobResponse
                {
                    ExecutionId = executionId,
                    JobName = "Log Cleanup",
                    Status = BackgroundJobStatuses.Skipped,
                    ResultMessage = result.Summary,
                    SkippedCount = result.SkippedCount
                };
            }

            await _executionService.MarkSucceededAsync(executionId, result.Summary, cancellationToken);

            return new RunBackgroundJobResponse
            {
                ExecutionId = executionId,
                JobName = "Log Cleanup",
                Status = BackgroundJobStatuses.Succeeded,
                ResultMessage = result.Summary,
                ProcessedCount = result.ProcessedCount,
                SkippedCount = result.SkippedCount
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Log cleanup job failed.");

            await _executionService.MarkFailedAsync(
                executionId,
                "Log cleanup job failed.",
                exception.ToString(),
                cancellationToken);

            await _activityLogService.LogImmediateAsync(
                ActivityTypes.SystemError,
                "Log cleanup job failed.",
                AuditLogConstants.Modules.BackgroundJobs,
                "Failed",
                "Log cleanup job failed.",
                cancellationToken: cancellationToken);

            return new RunBackgroundJobResponse
            {
                ExecutionId = executionId,
                JobName = "Log Cleanup",
                Status = BackgroundJobStatuses.Failed,
                ResultMessage = "Log cleanup job failed.",
                ErrorMessage = "Log cleanup job failed."
            };
        }
    }

    private async Task<RunBackgroundJobResponse> CreateSkippedResponseAsync(
        string jobName,
        string jobType,
        string? triggeredBy,
        string triggerSource,
        string message,
        CancellationToken cancellationToken)
    {
        var executionId = await _executionService.StartAsync(
            jobName,
            jobType,
            triggeredBy,
            triggerSource,
            null,
            cancellationToken);

        await _executionService.MarkSkippedAsync(executionId, message, cancellationToken);

        return new RunBackgroundJobResponse
        {
            ExecutionId = executionId,
            JobName = jobName,
            Status = BackgroundJobStatuses.Skipped,
            ResultMessage = message,
            SkippedCount = 1
        };
    }
}

public sealed class LogCleanupService : ILogCleanupService
{
    private readonly ILogger<LogCleanupService> _logger;

    public LogCleanupService(ILogger<LogCleanupService> logger)
    {
        _logger = logger;
    }

    public Task<LogCleanupBatchResult> ProcessAsync(
        bool enabled,
        int retentionDays,
        CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            _logger.LogInformation("Log cleanup skipped because it is disabled by configuration.");
            return Task.FromResult(new LogCleanupBatchResult
            {
                WasSkipped = true,
                SkippedCount = 1,
                Summary = "Log cleanup is disabled by configuration."
            });
        }

        _logger.LogWarning(
            "Log cleanup is enabled but purge is not implemented in this phase. RetentionDays={RetentionDays}",
            retentionDays);

        return Task.FromResult(new LogCleanupBatchResult
        {
            WasSkipped = true,
            SkippedCount = 1,
            Summary = "Log cleanup is enabled but purge is not implemented in this phase."
        });
    }
}
