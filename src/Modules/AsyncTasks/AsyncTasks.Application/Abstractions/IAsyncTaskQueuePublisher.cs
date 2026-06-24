using AsyncTasks.Application.Contracts;

namespace AsyncTasks.Application.Abstractions;

public interface IAsyncTaskQueuePublisher
{
    Task PublishAsync(ProcessAsyncTaskMessage message, CancellationToken cancellationToken = default);
}

public interface IAsyncTaskPublishBuffer
{
    void Enqueue(ProcessAsyncTaskMessage message);

    IReadOnlyList<ProcessAsyncTaskMessage> Drain();
}
