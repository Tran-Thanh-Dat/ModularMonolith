using System.Text.Json;
using AsyncTasks.Application.Abstractions;
using AsyncTasks.Domain.Constants;

namespace AsyncTasks.Infrastructure.Processors;

public sealed class EmailDemoAsyncTaskProcessor : IAsyncTaskProcessor
{
    public string TaskType => AsyncTaskTypes.EmailDemo;

    public bool CanProcess(string taskType) =>
        string.Equals(taskType, TaskType, StringComparison.OrdinalIgnoreCase);

    public async Task<string?> ProcessAsync(AsyncTaskProcessingContext context, CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        return JsonSerializer.Serialize(new { simulated = true, action = "email_sent", taskNo = context.TaskNo });
    }
}

public sealed class FileProcessingDemoAsyncTaskProcessor : IAsyncTaskProcessor
{
    public string TaskType => AsyncTaskTypes.FileProcessingDemo;

    public bool CanProcess(string taskType) =>
        string.Equals(taskType, TaskType, StringComparison.OrdinalIgnoreCase);

    public async Task<string?> ProcessAsync(AsyncTaskProcessingContext context, CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        return JsonSerializer.Serialize(new { simulated = true, action = "file_processed", taskNo = context.TaskNo });
    }
}

public sealed class FailDemoAsyncTaskProcessor : IAsyncTaskProcessor
{
    public string TaskType => AsyncTaskTypes.FailDemo;

    public bool CanProcess(string taskType) =>
        string.Equals(taskType, TaskType, StringComparison.OrdinalIgnoreCase);

    public Task<string?> ProcessAsync(AsyncTaskProcessingContext context, CancellationToken cancellationToken = default)
    {
        var reason = "Demo failure";
        var shouldAlwaysFail = true;

        if (!string.IsNullOrWhiteSpace(context.Payload))
        {
            try
            {
                using var doc = JsonDocument.Parse(context.Payload);
                if (doc.RootElement.TryGetProperty("failReason", out var failReason) &&
                    failReason.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(failReason.GetString()))
                {
                    reason = failReason.GetString()!;
                }

                if (doc.RootElement.TryGetProperty("shouldAlwaysFail", out var shouldFailProp) &&
                    (shouldFailProp.ValueKind == JsonValueKind.True || shouldFailProp.ValueKind == JsonValueKind.False))
                {
                    shouldAlwaysFail = shouldFailProp.GetBoolean();
                }
            }
            catch (JsonException)
            {
                // use defaults
            }
        }

        if (!shouldAlwaysFail)
        {
            return Task.FromResult<string?>(
                JsonSerializer.Serialize(new { simulated = true, action = "fail_demo_succeeded", taskNo = context.TaskNo }));
        }

        throw new InvalidOperationException(reason);
    }
}

public sealed class LongRunningDemoAsyncTaskProcessor : IAsyncTaskProcessor
{
    private readonly IAsyncTaskService _taskService;
    private readonly Persistence.AsyncTasksUnitOfWork _unitOfWork;

    public LongRunningDemoAsyncTaskProcessor(
        IAsyncTaskService taskService,
        Persistence.AsyncTasksUnitOfWork unitOfWork)
    {
        _taskService = taskService;
        _unitOfWork = unitOfWork;
    }

    public string TaskType => AsyncTaskTypes.LongRunningDemo;

    public bool CanProcess(string taskType) =>
        string.Equals(taskType, TaskType, StringComparison.OrdinalIgnoreCase);

    public async Task<string?> ProcessAsync(AsyncTaskProcessingContext context, CancellationToken cancellationToken = default)
    {
        var durationSeconds = 5;
        var steps = 5;

        if (!string.IsNullOrWhiteSpace(context.Payload))
        {
            using var doc = JsonDocument.Parse(context.Payload);
            if (doc.RootElement.TryGetProperty("durationSeconds", out var duration))
            {
                durationSeconds = duration.GetInt32();
            }

            if (doc.RootElement.TryGetProperty("steps", out var stepProp))
            {
                steps = Math.Max(1, stepProp.GetInt32());
            }
        }

        var delayMs = Math.Max(100, durationSeconds * 1000 / steps);
        for (var step = 1; step <= steps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(delayMs, cancellationToken);
            var progress = (int)Math.Round(step * 100m / steps);
            await _taskService.UpdateProgressAsync(context.AsyncTaskId, progress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return JsonSerializer.Serialize(new { simulated = true, action = "long_running_completed", steps });
    }
}

public sealed class AsyncTaskProcessorRegistry : IAsyncTaskProcessorRegistry
{
    private readonly IReadOnlyDictionary<string, IAsyncTaskProcessor> _processors;

    public AsyncTaskProcessorRegistry(IEnumerable<IAsyncTaskProcessor> processors)
    {
        _processors = processors.ToDictionary(p => p.TaskType, StringComparer.OrdinalIgnoreCase);
    }

    public IAsyncTaskProcessor GetProcessor(string taskType)
    {
        if (_processors.TryGetValue(taskType, out var processor))
        {
            return processor;
        }

        throw new InvalidOperationException($"Processor not found for task type '{taskType}'.");
    }
}
