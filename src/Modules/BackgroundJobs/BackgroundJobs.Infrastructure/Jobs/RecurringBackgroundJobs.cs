using BackgroundJobs.Application.Abstractions;
using BackgroundJobs.Infrastructure.Persistence;
using Files.Infrastructure.Persistence;
using Hangfire;
using Microsoft.Extensions.Logging;
using Notifications.Infrastructure.Persistence;

namespace BackgroundJobs.Infrastructure.Jobs;

public sealed class EmailRetryJob
{
    private readonly IBackgroundJobRunner _runner;
    private readonly NotificationsUnitOfWork _notificationsUnitOfWork;
    private readonly BackgroundJobsUnitOfWork _backgroundJobsUnitOfWork;
    private readonly ILogger<EmailRetryJob> _logger;

    public EmailRetryJob(
        IBackgroundJobRunner runner,
        NotificationsUnitOfWork notificationsUnitOfWork,
        BackgroundJobsUnitOfWork backgroundJobsUnitOfWork,
        ILogger<EmailRetryJob> logger)
    {
        _runner = runner;
        _notificationsUnitOfWork = notificationsUnitOfWork;
        _backgroundJobsUnitOfWork = backgroundJobsUnitOfWork;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 60 * 30)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting recurring email retry job.");

        await _runner.RunEmailRetryAsync(null, null, cancellationToken);
        await _notificationsUnitOfWork.SaveChangesAsync(cancellationToken);
        await _backgroundJobsUnitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed recurring email retry job.");
    }
}

public sealed class TemporaryFileCleanupJob
{
    private readonly IBackgroundJobRunner _runner;
    private readonly FilesUnitOfWork _filesUnitOfWork;
    private readonly BackgroundJobsUnitOfWork _backgroundJobsUnitOfWork;
    private readonly ILogger<TemporaryFileCleanupJob> _logger;

    public TemporaryFileCleanupJob(
        IBackgroundJobRunner runner,
        FilesUnitOfWork filesUnitOfWork,
        BackgroundJobsUnitOfWork backgroundJobsUnitOfWork,
        ILogger<TemporaryFileCleanupJob> logger)
    {
        _runner = runner;
        _filesUnitOfWork = filesUnitOfWork;
        _backgroundJobsUnitOfWork = backgroundJobsUnitOfWork;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 60 * 30)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting recurring temporary file cleanup job.");

        await _runner.RunTemporaryFileCleanupAsync(null, null, cancellationToken);
        await _filesUnitOfWork.SaveChangesAsync(cancellationToken);
        await _backgroundJobsUnitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed recurring temporary file cleanup job.");
    }
}

public sealed class LogCleanupJob
{
    private readonly IBackgroundJobRunner _runner;
    private readonly BackgroundJobsUnitOfWork _backgroundJobsUnitOfWork;
    private readonly ILogger<LogCleanupJob> _logger;

    public LogCleanupJob(
        IBackgroundJobRunner runner,
        BackgroundJobsUnitOfWork backgroundJobsUnitOfWork,
        ILogger<LogCleanupJob> logger)
    {
        _runner = runner;
        _backgroundJobsUnitOfWork = backgroundJobsUnitOfWork;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 60 * 30)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting recurring log cleanup job.");

        await _runner.RunLogCleanupAsync(null, cancellationToken);
        await _backgroundJobsUnitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed recurring log cleanup job.");
    }
}
