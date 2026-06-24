using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Contracts;
using AsyncTasks.Domain.Constants;
using AsyncTasks.Domain.Entities;
using AsyncTasks.Infrastructure.Consumers;
using AsyncTasks.Infrastructure.Options;
using AsyncTasks.Infrastructure.Persistence;
using AsyncTasks.Infrastructure.Processors;
using AsyncTasks.Infrastructure.Queue;
using AsyncTasks.Infrastructure.Services;
using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Testing.Fakes;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsyncTasks.Infrastructure.Tests;

/// <summary>
/// In-process MassTransit test harness — verifies consumer + retry + FAILED without RabbitMQ broker.
/// Full broker smoke: scripts/smoke-async-tasks.ps1
/// </summary>
public sealed class AsyncTasksMassTransitIntegrationTests : IAsyncLifetime
{
    private readonly Guid _userId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private ITestHarness _harness = null!;
    private AsyncTasksDbContext _dbContext = null!;
    private AsyncTasksUnitOfWork _unitOfWork = null!;

    public async Task InitializeAsync()
    {
        var currentUser = TestDataFactory.CreateCurrentUser(_userId);
        var dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero));

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        var dbName = Guid.NewGuid().ToString("N");
        services.AddDbContext<AsyncTasksDbContext>(o =>
            o.UseInMemoryDatabase(dbName));
        services.AddScoped<AsyncTasksUnitOfWork>();

        services.AddSingleton<IDateTimeProvider>(dateTime);
        services.AddSingleton<ICurrentUserService>(currentUser);
        services.AddSingleton<IActivityLogService, FakeActivityLogService>();

        services.Configure<MessageQueueOptions>(o =>
        {
            o.Enabled = true;
            o.Retry = new MessageQueueRetryOptions { RetryCount = 3, IntervalSeconds = 1 };
        });

        services.AddScoped<IAsyncTaskPublishBuffer, AsyncTaskPublishBuffer>();
        services.AddScoped<IAsyncTaskService, AsyncTaskService>();
        services.AddScoped<IAsyncTaskConsumerService, AsyncTaskConsumerService>();
        services.AddScoped<IAsyncTaskProcessor, EmailDemoAsyncTaskProcessor>();
        services.AddScoped<IAsyncTaskProcessor, FileProcessingDemoAsyncTaskProcessor>();
        services.AddScoped<IAsyncTaskProcessor, FailDemoAsyncTaskProcessor>();
        services.AddScoped<IAsyncTaskProcessorRegistry, AsyncTaskProcessorRegistry>();

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<ProcessAsyncTaskConsumer>();

            x.UsingInMemory((context, cfg) =>
            {
                cfg.ReceiveEndpoint(AsyncTasksConstants.ProcessQueueName, endpoint =>
                {
                    endpoint.UseMessageRetry(retry => retry.Immediate(3));
                    endpoint.ConfigureConsumer<ProcessAsyncTaskConsumer>(context);
                });
            });
        });

        _provider = services.BuildServiceProvider(true);
        _scope = _provider.CreateScope();
        _harness = _provider.GetRequiredService<ITestHarness>();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AsyncTasksDbContext>();
        _unitOfWork = _scope.ServiceProvider.GetRequiredService<AsyncTasksUnitOfWork>();

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness is not null)
        {
            await _harness.Stop();
        }

        _scope.Dispose();
        await _dbContext.DisposeAsync();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task MassTransit_EmailDemo_PublishedMessage_MarksCompleted()
    {
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.EmailDemo);
        var message = BuildMessage(task);

        await _harness.Bus.Publish(message);
        await WaitForConsumptionAsync(task.Id, TimeSpan.FromSeconds(15));

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Completed, current.Status);
        Assert.Equal(100, current.ProgressPercent);
    }

    [Fact]
    public async Task MassTransit_FailDemo_ShouldAlwaysFailTrue_MarksFailedAfterRetries()
    {
        var payload = """{"shouldAlwaysFail":true,"failReason":"harness fail test"}""";
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.FailDemo, payload);
        var message = BuildMessage(task);

        await _harness.Bus.Publish(message);
        await WaitForConsumptionAsync(task.Id, TimeSpan.FromSeconds(15));

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Failed, current.Status);
        Assert.NotNull(current.FailedAt);
        Assert.NotNull(current.LastErrorMessage);
        Assert.Contains("harness fail test", current.LastErrorMessage);
    }

    [Fact]
    public async Task MassTransit_FailDemo_ShouldAlwaysFailFalse_MarksCompleted()
    {
        var payload = """{"shouldAlwaysFail":false}""";
        var task = await SeedQueuedTaskAsync(AsyncTaskTypes.FailDemo, payload);
        var message = BuildMessage(task);

        await _harness.Bus.Publish(message);
        await WaitForConsumptionAsync(task.Id, TimeSpan.FromSeconds(10));

        var current = await _dbContext.AsyncTasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(AsyncTaskStatuses.Completed, current.Status);
    }

    private async Task WaitForConsumptionAsync(Guid taskId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await _harness.Consumed.Any<ProcessAsyncTaskMessage>(c => c.Context.Message.AsyncTaskId == taskId))
            {
                await Task.Delay(100);
                return;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Message for task {taskId} was not consumed within {timeout.TotalSeconds}s.");
    }

    private async Task<AsyncTask> SeedQueuedTaskAsync(string taskType, string? payload = null)
    {
        var messageId = Guid.NewGuid();
        var now = _scope.ServiceProvider.GetRequiredService<IDateTimeProvider>().UtcNow;
        var task = AsyncTask.CreatePending(
            $"AT-HARNESS-{Guid.NewGuid():N}"[..22],
            taskType,
            payload,
            3,
            messageId,
            Guid.NewGuid(),
            AsyncTasksConstants.ProcessQueueName,
            null,
            null,
            _userId,
            now,
            _userId);
        task.MarkQueued(now, _userId);
        await _unitOfWork.Repository<AsyncTask, Guid>().AddAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    private static ProcessAsyncTaskMessage BuildMessage(AsyncTask task) =>
        new()
        {
            MessageId = task.MessageId ?? Guid.NewGuid(),
            CorrelationId = task.CorrelationId ?? Guid.NewGuid(),
            AsyncTaskId = task.Id,
            TaskNo = task.TaskNo,
            TaskType = task.Type,
            Payload = task.Payload,
            RequestedByUserId = task.RequestedByUserId,
            CreatedAt = task.CreatedAt
        };
}
