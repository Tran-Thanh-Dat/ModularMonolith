using System.Diagnostics;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IUnitOfWork> _unitOfWorks;
    private readonly IEnumerable<IPostCommitHook> _postCommitHooks;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IEnumerable<IUnitOfWork> unitOfWorks,
        IEnumerable<IPostCommitHook> postCommitHooks,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWorks = unitOfWorks;
        _postCommitHooks = postCommitHooks;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand())
        {
            return await next();
        }

        var unitOfWorks = _unitOfWorks.ToList();
        if (unitOfWorks.Count == 0)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting transaction behavior for {RequestName} with {UnitOfWorkCount} unit of work instances",
            requestName,
            unitOfWorks.Count);

        try
        {
            var response = await next();

            if (ResultReflection.IsFailedResult(response))
            {
                await RollbackPostCommitHooksAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Transaction behavior skipped save for failed {RequestName} in {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
                return response;
            }

            foreach (var unitOfWork in unitOfWorks)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await CommitPostCommitHooksAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Transaction behavior completed for {RequestName} in {ElapsedMilliseconds}ms with {UnitOfWorkCount} unit of work instances",
                requestName,
                stopwatch.ElapsedMilliseconds,
                unitOfWorks.Count);

            return response;
        }
        catch (Exception exception)
        {
            await RollbackPostCommitHooksAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogError(
                exception,
                "Transaction behavior failed for {RequestName} after {ElapsedMilliseconds}ms with {UnitOfWorkCount} unit of work instances",
                requestName,
                stopwatch.ElapsedMilliseconds,
                unitOfWorks.Count);

            throw;
        }
    }

    private async Task CommitPostCommitHooksAsync(CancellationToken cancellationToken)
    {
        foreach (var hook in _postCommitHooks)
        {
            await hook.OnCommittedAsync(cancellationToken);
        }
    }

    private async Task RollbackPostCommitHooksAsync(CancellationToken cancellationToken)
    {
        foreach (var hook in _postCommitHooks)
        {
            await hook.OnRollbackAsync(cancellationToken);
        }
    }

    private static bool IsCommand()
    {
        var requestType = typeof(TRequest);

        return typeof(ICommand).IsAssignableFrom(requestType) ||
               requestType.GetInterfaces().Any(i =>
                   i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
    }
}
