using System.Collections.Concurrent;
using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Contracts;

namespace AsyncTasks.Infrastructure.Queue;

public sealed class AsyncTaskPublishBuffer : IAsyncTaskPublishBuffer
{
    private readonly ConcurrentQueue<ProcessAsyncTaskMessage> _queue = new();

    public void Enqueue(ProcessAsyncTaskMessage message) => _queue.Enqueue(message);

    public IReadOnlyList<ProcessAsyncTaskMessage> Drain()
    {
        var items = new List<ProcessAsyncTaskMessage>();
        while (_queue.TryDequeue(out var message))
        {
            items.Add(message);
        }

        return items;
    }
}
