using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace AsyncTasks.Infrastructure.Queue;

public sealed class MassTransitAsyncTaskQueuePublisher : IAsyncTaskQueuePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitAsyncTaskQueuePublisher> _logger;

    public MassTransitAsyncTaskQueuePublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitAsyncTaskQueuePublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync(ProcessAsyncTaskMessage message, CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(message, cancellationToken);
        _logger.LogInformation(
            "Published async task message {MessageId} for task {TaskNo}",
            message.MessageId,
            message.TaskNo);
    }
}

public sealed class NoOpAsyncTaskQueuePublisher : IAsyncTaskQueuePublisher
{
    public Task PublishAsync(ProcessAsyncTaskMessage message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
