using AuditLogs.Application.Abstractions;
using BackgroundJobs.Application.Abstractions;
using BackgroundJobs.Application.JobExecutions;
using BackgroundJobs.Application.JobExecutions.RunEmailRetryJob;
using BackgroundJobs.Application.JobExecutions.RunLogCleanupJob;
using BackgroundJobs.Application.JobExecutions.RunTemporaryFileCleanupJob;
using BackgroundJobs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Testing.Fakes;
using Xunit;

namespace BackgroundJobs.Application.Tests;

public sealed class ManualJobTriggerCommandHandlerTests
{
    [Fact]
    public async Task RunEmailRetryJob_ReturnsSuccess_WhenJobExecutionFailed()
    {
        var executionId = Guid.NewGuid();
        var runner = new FakeBackgroundJobRunner
        {
            EmailRetryResponse = new RunBackgroundJobResponse
            {
                ExecutionId = executionId,
                JobName = "Email Retry",
                Status = BackgroundJobStatuses.Failed,
                ResultMessage = "Email retry job failed.",
                ErrorMessage = "Email retry job failed."
            }
        };

        var handler = new RunEmailRetryJobCommandHandler(
            runner,
            new FakeCurrentUserService(userName: "admin"),
            new FakeActivityLogService());

        var result = await handler.Handle(new RunEmailRetryJobCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(BackgroundJobStatuses.Failed, result.Data!.Status);
        Assert.Equal(executionId, result.Data.ExecutionId);
        Assert.Equal("Email retry job failed.", result.Data.ErrorMessage);
    }

    [Fact]
    public async Task RunEmailRetryJob_ReturnsSuccess_WhenJobExecutionSkipped()
    {
        var executionId = Guid.NewGuid();
        var runner = new FakeBackgroundJobRunner
        {
            EmailRetryResponse = new RunBackgroundJobResponse
            {
                ExecutionId = executionId,
                JobName = "Email Retry",
                Status = BackgroundJobStatuses.Skipped,
                ResultMessage = "Background jobs are disabled.",
                SkippedCount = 1
            }
        };

        var handler = new RunEmailRetryJobCommandHandler(
            runner,
            new FakeCurrentUserService(userName: "admin"),
            new FakeActivityLogService());

        var result = await handler.Handle(new RunEmailRetryJobCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BackgroundJobStatuses.Skipped, result.Data!.Status);
        Assert.Equal(executionId, result.Data.ExecutionId);
    }

    [Fact]
    public async Task RunTemporaryFileCleanupJob_ReturnsSuccess_WhenJobExecutionFailed()
    {
        var runner = new FakeBackgroundJobRunner
        {
            TemporaryFileCleanupResponse = new RunBackgroundJobResponse
            {
                ExecutionId = Guid.NewGuid(),
                JobName = "Temporary File Cleanup",
                Status = BackgroundJobStatuses.Failed,
                ResultMessage = "Temporary file cleanup job failed.",
                ErrorMessage = "Temporary file cleanup job failed."
            }
        };

        var handler = new RunTemporaryFileCleanupJobCommandHandler(
            runner,
            new FakeCurrentUserService(userName: "admin"),
            new FakeActivityLogService());

        var result = await handler.Handle(new RunTemporaryFileCleanupJobCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BackgroundJobStatuses.Failed, result.Data!.Status);
    }

    [Fact]
    public async Task RunLogCleanupJob_ReturnsSuccess_WhenJobExecutionSkipped()
    {
        var runner = new FakeBackgroundJobRunner
        {
            LogCleanupResponse = new RunBackgroundJobResponse
            {
                ExecutionId = Guid.NewGuid(),
                JobName = "Log Cleanup",
                Status = BackgroundJobStatuses.Skipped,
                ResultMessage = "Log cleanup is disabled by configuration.",
                SkippedCount = 1
            }
        };

        var handler = new RunLogCleanupJobCommandHandler(
            runner,
            new FakeCurrentUserService(userName: "admin"),
            new FakeActivityLogService());

        var result = await handler.Handle(new RunLogCleanupJobCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BackgroundJobStatuses.Skipped, result.Data!.Status);
    }

    private sealed class FakeBackgroundJobRunner : IBackgroundJobRunner
    {
        public RunBackgroundJobResponse EmailRetryResponse { get; init; } = default!;

        public RunBackgroundJobResponse TemporaryFileCleanupResponse { get; init; } = default!;

        public RunBackgroundJobResponse LogCleanupResponse { get; init; } = default!;

        public Task<RunBackgroundJobResponse> RunEmailRetryAsync(
            int? batchSize,
            string? triggeredBy,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(EmailRetryResponse);

        public Task<RunBackgroundJobResponse> RunTemporaryFileCleanupAsync(
            int? batchSize,
            string? triggeredBy = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(TemporaryFileCleanupResponse);

        public Task<RunBackgroundJobResponse> RunLogCleanupAsync(
            string? triggeredBy = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LogCleanupResponse);
    }
}
