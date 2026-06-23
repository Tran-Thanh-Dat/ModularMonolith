using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Caching;

internal sealed class CacheInvalidationPostCommitHook : IPostCommitHook
{
    private readonly ICacheOperationBuffer _buffer;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationPostCommitHook> _logger;

    public CacheInvalidationPostCommitHook(
        ICacheOperationBuffer buffer,
        ICacheService cacheService,
        ILogger<CacheInvalidationPostCommitHook> logger)
    {
        _buffer = buffer;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task OnCommittedAsync(CancellationToken cancellationToken = default)
    {
        var entries = _buffer.TakeAll();
        if (entries.Count == 0)
        {
            return;
        }

        foreach (var entry in entries)
        {
            try
            {
                await CacheOperationExecutor.ExecuteAsync(_cacheService, entry, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Post-commit cache operation failed. Operation={CacheOperation}, Target={CacheTarget}",
                    entry.OperationType,
                    entry.OperationType == CacheOperationType.RemoveByPrefix
                        ? entry.Prefix
                        : entry.Key);
            }
        }
    }

    public Task OnRollbackAsync(CancellationToken cancellationToken = default)
    {
        _buffer.Clear();
        return Task.CompletedTask;
    }
}
