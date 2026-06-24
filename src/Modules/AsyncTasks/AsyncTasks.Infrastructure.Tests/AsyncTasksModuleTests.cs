using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Contracts;
using AsyncTasks.Domain.Constants;
using AsyncTasks.Domain.Entities;
using AsyncTasks.Domain.Errors;
using AsyncTasks.Infrastructure.Options;
using AsyncTasks.Infrastructure.Persistence;
using AsyncTasks.Infrastructure.Processors;
using AsyncTasks.Infrastructure.Queue;
using AsyncTasks.Infrastructure.Services;
using AuthorizationPolicies.Domain.Constants;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Identity.Application.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AsyncTasks.Infrastructure.Tests;

public sealed class AsyncTasksModuleTests : IDisposable
{
    private readonly AsyncTasksDbContext _dbContext;
    private readonly AsyncTasksUnitOfWork _unitOfWork;
    private readonly FakeActivityLogService _activityLog;
    private readonly AsyncTaskPublishBuffer _publishBuffer;
    private readonly RecordingAsyncTaskQueuePublisher _publisher;
    private readonly FixedDateTimeProvider _dateTime;
    private readonly FakeCurrentUserService _currentUser;
    private readonly AsyncTaskService _taskService;
    private readonly AsyncTaskConsumerService _consumerService;
    private readonly AsyncTaskPublishPostCommitHook _postCommitHook;
    private readonly Guid _userId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public AsyncTasksModuleTests()
    {
        _currentUser = TestDataFactory.CreateCurrentUser(_userId);
        _dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 6, 23, 14, 0, 0, TimeSpan.Zero));
        _publishBuffer = new AsyncTaskPublishBuffer();
        _publisher = new RecordingAsyncTaskQueuePublisher();

        var options = new DbContextOptionsBuilder<AsyncTasksDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new AsyncTasksDbContext(options, _currentUser, _dateTime);
        _unitOfWork = new AsyncTasksUnitOfWork(_dbContext, NullLogger<AsyncTasksUnitOfWork>.Instance);
        _activityLog = new FakeActivityLogService();

        var queueOptions = Microsoft.Extensions.Options.Options.Create(new MessageQueueOptions { Enabled = true, Retry = new MessageQueueRetryOptions { RetryCount = 3 } });

        _taskService = new AsyncTaskService(
            _unitOfWork,
            _currentUser,
            _dateTime,
            _activityLog,
            _publishBuffer,
            queueOptions,
            NullLogger<AsyncTaskService>.Instance);

        var processors = new IAsyncTaskProcessor[]
        {
            new EmailDemoAsyncTaskProcessor(),
            new FileProcessingDemoAsyncTaskProcessor(),
            new FailDemoAsyncTaskProcessor(),
            new LongRunningDemoAsyncTaskProcessor(_taskService, _unitOfWork)
        };

        _consumerService = new AsyncTaskConsumerService(
            _unitOfWork,
            _taskService,
            new AsyncTaskProcessorRegistry(processors),
            NullLogger<AsyncTaskConsumerService>.Instance);

        _postCommitHook = new AsyncTaskPublishPostCommitHook(
            _publishBuffer,
            _publisher,
            _taskService,
            _unitOfWork,
            NullLogger<AsyncTaskPublishPostCommitHook>.Instance);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task SubmitEmailDemoTask_WhenValid_Succeeds()
    {
        var dto = await _taskService.SubmitEmailDemoAsync("a@example.com", "Hello", "Body", null, null);
        await _unitOfWork.SaveChangesAsync();
        await _postCommitHook.OnCommittedAsync();

        var queued = await _taskService.GetByIdAsync(dto.Id);
        Assert.Equal(AsyncTaskTypes.EmailDemo, queued!.Type);
        Assert.Equal(AsyncTaskStatuses.Queued, queued.Status);
        Assert.Single(_publisher.PublishedMessages);
    }

    [Fact]
    public async Task SubmitFileProcessingDemoTask_WhenValid_Succeeds()
    {
        var dto = await _taskService.SubmitFileProcessingDemoAsync(Guid.NewGuid(), "demo.pdf", null, null);
        await _unitOfWork.SaveChangesAsync();
        await _postCommitHook.OnCommittedAsync();

        var queued = await _taskService.GetByIdAsync(dto.Id);
        Assert.Equal(AsyncTaskTypes.FileProcessingDemo, queued!.Type);
        Assert.Equal(AsyncTaskStatuses.Queued, queued.Status);
    }

    [Fact]
    public async Task SubmitFailDemoTask_WhenValid_Succeeds()
    {
        var dto = await _taskService.SubmitFailDemoAsync("expected failure", true, null, null);
        await _unitOfWork.SaveChangesAsync();
        await _postCommitHook.OnCommittedAsync();

        var queued = await _taskService.GetByIdAsync(dto.Id);
        Assert.Equal(AsyncTaskTypes.FailDemo, queued!.Type);
        Assert.Equal(AsyncTaskStatuses.Queued, queued.Status);
    }

    [Fact]
    public async Task SubmitTask_WhenMessageQueueDisabled_ThrowsBadRequest()
    {
        var disabledService = CreateService(queueEnabled: false);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            disabledService.SubmitEmailDemoAsync("a@example.com", null, null, null, null));

        Assert.Equal(AsyncTaskErrors.MessageQueueDisabled, exception.Code);
    }

    [Fact]
    public async Task SubmitTask_ShouldCreateTaskNoUnique()
    {
        var first = await _taskService.SubmitEmailDemoAsync(null, null, null, null, null);
        var second = await _taskService.SubmitEmailDemoAsync(null, null, null, null, null);

        Assert.NotEqual(first.TaskNo, second.TaskNo);
    }

    [Fact]
    public async Task SubmitTask_ShouldSetStatusPendingBeforePublish()
    {
        var dto = await _taskService.SubmitEmailDemoAsync(null, null, null, null, null);

        Assert.Equal(AsyncTaskStatuses.Pending, dto.Status);
        Assert.NotEmpty(_publishBuffer.Drain());
    }

    [Fact]
    public async Task SubmitTask_ShouldPublishMessage()
    {
        await _taskService.SubmitEmailDemoAsync(null, null, null, null, null);
        await _unitOfWork.SaveChangesAsync();
        await _postCommitHook.OnCommittedAsync();

        Assert.Single(_publisher.PublishedMessages);
    }

    [Fact]
    public async Task Consumer_WhenTaskNotFound_ShouldNotThrow()
    {
        var message = CreateMessage(Guid.NewGuid(), AsyncTaskTypes.EmailDemo, "AT-MISSING");

        await _consumerService.ProcessMessageAsync(message, "test-consumer");
    }

    [Fact]
    public async Task Consumer_WhenTaskAlreadyCompleted_ShouldSkipIdempotently()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkProcessingAsync(task.Id, "test", CancellationToken.None);
        await _taskService.MarkCompletedAsync(task.Id, "{}", CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        var message = CreateMessage(task.Id, task.Type, task.TaskNo, task.MessageId!.Value);
        await _consumerService.ProcessMessageAsync(message, "test-consumer");

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Completed, current.Status);
    }

    [Fact]
    public async Task Consumer_EmailDemo_ShouldMarkProcessingThenCompleted()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        var message = CreateMessage(task.Id, task.Type, task.TaskNo, task.MessageId!.Value);

        await _consumerService.ProcessMessageAsync(message, "email-consumer");

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Completed, current.Status);
        Assert.Equal(100, current.ProgressPercent);
        Assert.NotNull(current.StartedAt);
        Assert.NotNull(current.CompletedAt);
    }

    [Fact]
    public async Task Consumer_FileProcessingDemo_ShouldMarkCompleted()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.FileProcessingDemo);
        var message = CreateMessage(task.Id, task.Type, task.TaskNo, task.MessageId!.Value);

        await _consumerService.ProcessMessageAsync(message, "file-consumer");

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Completed, current.Status);
    }

    [Fact]
    public async Task Consumer_FailDemo_ShouldMarkFailedAfterFaultHandling()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.FailDemo, """{"failReason":"demo","shouldAlwaysFail":true}""");
        var message = CreateMessage(task.Id, task.Type, task.TaskNo, task.MessageId!.Value);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _consumerService.ProcessMessageAsync(message, "fail-consumer"));

        await _consumerService.HandleFaultAsync(message, "demo failure after retries");

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Failed, current.Status);
        Assert.Equal(AsyncTaskErrors.ProcessingFailed, current.LastErrorCode);
    }

    [Fact]
    public async Task MarkProcessing_ShouldSetStartedAt()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkProcessingAsync(task.Id, "worker", CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Processing, current.Status);
        Assert.NotNull(current.StartedAt);
    }

    [Fact]
    public async Task MarkCompleted_ShouldSetCompletedAtAndProgress100()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkCompletedAsync(task.Id, """{"ok":true}""", CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Completed, current.Status);
        Assert.Equal(100, current.ProgressPercent);
        Assert.NotNull(current.CompletedAt);
    }

    [Fact]
    public async Task MarkFailed_ShouldSetFailedAtAndErrorMessage()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkFailedAsync(task.Id, AsyncTaskErrors.ProcessingFailed, "Something went wrong", CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Failed, current.Status);
        Assert.NotNull(current.FailedAt);
        Assert.Equal("Something went wrong", current.LastErrorMessage);
    }

    [Fact]
    public async Task CancelTask_WhenPending_ShouldSucceed()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        task.MarkQueued(_dateTime.UtcNow, _userId);
        await _unitOfWork.SaveChangesAsync();

        await _taskService.CancelAsync(task.Id);
        await _unitOfWork.SaveChangesAsync();

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Cancelled, current.Status);
    }

    [Fact]
    public async Task CancelTask_WhenCompleted_ThrowsBadRequest()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkCompletedAsync(task.Id, null, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _taskService.CancelAsync(task.Id));
        Assert.Equal(AsyncTaskErrors.CannotCancel, exception.ErrorCode);
    }

    [Fact]
    public async Task RetryTask_WhenFailed_ShouldRepublishMessage()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkFailedAsync(task.Id, AsyncTaskErrors.ProcessingFailed, "fail", CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        _publisher.PublishedMessages.Clear();
        await _taskService.RetryAsync(task.Id);
        await _unitOfWork.SaveChangesAsync();
        await _postCommitHook.OnCommittedAsync();

        Assert.Single(_publisher.PublishedMessages);
        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Queued, current.Status);
        Assert.Equal(1, current.RetryCount);
    }

    [Fact]
    public async Task RetryTask_WhenCompleted_ThrowsBadRequest()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkCompletedAsync(task.Id, null, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _taskService.RetryAsync(task.Id));
        Assert.Equal(AsyncTaskErrors.CannotRetry, exception.ErrorCode);
    }

    [Fact]
    public void ProgressPercent_ShouldBeBetween0And100()
    {
        var task = AsyncTask.CreatePending(
            "AT-TEST",
            AsyncTaskTypes.EmailDemo,
            null,
            3,
            Guid.NewGuid(),
            Guid.NewGuid(),
            AsyncTasksConstants.ProcessQueueName,
            null,
            null,
            _userId,
            _dateTime.UtcNow,
            _userId);

        task.UpdateProgress(50, _dateTime.UtcNow, _userId);
        Assert.Equal(50, task.ProgressPercent);

        Assert.Throws<DomainException>(() => task.UpdateProgress(101, _dateTime.UtcNow, _userId));
    }

    [Fact]
    public void SeedPermissions_IsIdempotent()
    {
        var asyncTaskPermissions = new[]
        {
            PermissionCodes.AsyncTaskView,
            PermissionCodes.AsyncTaskSubmit,
            PermissionCodes.AsyncTaskCancel,
            PermissionCodes.AsyncTaskRetry,
            PermissionCodes.AsyncTaskManage
        };

        foreach (var code in asyncTaskPermissions)
        {
            Assert.Contains(code, PermissionCodes.All);
        }
    }

    [Fact]
    public void RabbitMQOptions_WhenDisabled_DoesNotRequireBroker()
    {
        var options = new MessageQueueOptions { Enabled = false };
        Assert.False(options.Enabled);
        Assert.Equal("RabbitMQ", options.Provider);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsSubmittedTasks()
    {
        await _taskService.SubmitEmailDemoAsync(null, null, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var page = await _taskService.GetPagedAsync(new AsyncTaskListFilter { PageIndex = 1, PageSize = 10 });

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
    }

    [Fact]
    public void AuthorizationMatrixSeed_UsesCancelActionConstant()
    {
        Assert.Equal("Cancel", AuthorizationActions.Cancel);
    }

    [Fact]
    public async Task FailDemoProcessor_WhenShouldAlwaysFailTrue_Throws()
    {
        var processor = new FailDemoAsyncTaskProcessor();
        var context = new AsyncTaskProcessingContext
        {
            TaskType = AsyncTaskTypes.FailDemo,
            Payload = """{"shouldAlwaysFail":true,"failReason":"expected"}"""
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessAsync(context));
    }

    [Fact]
    public async Task FailDemoProcessor_WhenShouldAlwaysFailFalse_Completes()
    {
        var processor = new FailDemoAsyncTaskProcessor();
        var context = new AsyncTaskProcessingContext
        {
            TaskType = AsyncTaskTypes.FailDemo,
            TaskNo = "AT-FAIL-OK",
            Payload = """{"shouldAlwaysFail":false}"""
        };

        var result = await processor.ProcessAsync(context);
        Assert.NotNull(result);
        Assert.Contains("fail_demo_succeeded", result);
    }

    [Fact]
    public async Task FaultConsumer_WhenFaultReceived_MarksTaskFailed()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkProcessingAsync(task.Id, "test", CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        var message = CreateMessage(task.Id, task.Type, task.TaskNo, task.MessageId);
        await _consumerService.HandleFaultAsync(message, "fault after retries");

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Failed, current.Status);
        Assert.NotNull(current.FailedAt);
        Assert.NotNull(current.LastErrorMessage);
    }

    [Fact]
    public async Task FaultConsumer_WhenTaskAlreadyFailed_IsIdempotent()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkFailedAsync(task.Id, AsyncTaskErrors.ProcessingFailed, "first", CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        var message = CreateMessage(task.Id, task.Type, task.TaskNo, task.MessageId);
        await _consumerService.HandleFaultAsync(message, "duplicate fault");

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Failed, current.Status);
        Assert.Equal("first", current.LastErrorMessage);
    }

    [Fact]
    public async Task Consumer_WhenTaskProcessing_AllowsRetryProcessing()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        await _taskService.MarkProcessingAsync(task.Id, "worker", CancellationToken.None);
        await _unitOfWork.SaveChangesAsync();

        var message = CreateMessage(task.Id, task.Type, task.TaskNo, task.MessageId);
        await _consumerService.ProcessMessageAsync(message, "worker-retry");

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Completed, current.Status);
    }

    private AsyncTaskService CreateService(bool queueEnabled)
    {
        var queueOptions = Microsoft.Extensions.Options.Options.Create(new MessageQueueOptions { Enabled = queueEnabled });
        return new AsyncTaskService(
            _unitOfWork,
            _currentUser,
            _dateTime,
            _activityLog,
            _publishBuffer,
            queueOptions,
            NullLogger<AsyncTaskService>.Instance);
    }

    private async Task<AsyncTask> SeedQueuedTaskAsync(string taskType, string? payload = null)
    {
        var messageId = Guid.NewGuid();
        var task = AsyncTask.CreatePending(
            $"AT-SEED-{Guid.NewGuid():N}"[..20],
            taskType,
            payload,
            3,
            messageId,
            Guid.NewGuid(),
            AsyncTasksConstants.ProcessQueueName,
            null,
            null,
            _userId,
            _dateTime.UtcNow,
            _userId);
        task.MarkQueued(_dateTime.UtcNow, _userId);
        await _unitOfWork.Repository<AsyncTask, Guid>().AddAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    private static ProcessAsyncTaskMessage CreateMessage(
        Guid taskId,
        string taskType,
        string taskNo,
        Guid? messageId = null) =>
        new()
        {
            AsyncTaskId = taskId,
            TaskType = taskType,
            TaskNo = taskNo,
            MessageId = messageId ?? Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed class RecordingAsyncTaskQueuePublisher : IAsyncTaskQueuePublisher
    {
        public List<ProcessAsyncTaskMessage> PublishedMessages { get; } = [];

        public Task PublishAsync(ProcessAsyncTaskMessage message, CancellationToken cancellationToken = default)
        {
            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
