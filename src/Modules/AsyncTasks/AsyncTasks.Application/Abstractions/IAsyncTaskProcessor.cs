namespace AsyncTasks.Application.Abstractions;

using AsyncTasks.Application.Contracts;

public sealed class AsyncTaskProcessingContext
{
    public Guid AsyncTaskId { get; init; }

    public string TaskNo { get; init; } = default!;

    public string TaskType { get; init; } = default!;

    public string? Payload { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }

    public Guid? RequestedByUserId { get; init; }

    public Guid CorrelationId { get; init; }

    public Guid MessageId { get; init; }
}

public interface IAsyncTaskProcessor
{
    string TaskType { get; }

    bool CanProcess(string taskType);

    Task<string?> ProcessAsync(AsyncTaskProcessingContext context, CancellationToken cancellationToken = default);
}

public interface IAsyncTaskProcessorRegistry
{
    IAsyncTaskProcessor GetProcessor(string taskType);
}

public interface IAsyncTaskConsumerService
{
    Task ProcessMessageAsync(ProcessAsyncTaskMessage message, string consumerName, CancellationToken cancellationToken = default);

    Task HandleFaultAsync(ProcessAsyncTaskMessage message, string errorMessage, CancellationToken cancellationToken = default);
}
