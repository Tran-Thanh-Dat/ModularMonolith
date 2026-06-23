using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Converts <see cref="BusinessException"/> thrown by handlers/services into failed <see cref="Result"/> responses.
/// </summary>
public sealed class BusinessExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<BusinessExceptionBehavior<TRequest, TResponse>> _logger;

    public BusinessExceptionBehavior(ILogger<BusinessExceptionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (BusinessException exception)
        {
            _logger.LogWarning(
                exception,
                "Business rule violation for {RequestName}: {ErrorCode}",
                typeof(TRequest).Name,
                exception.Code);

            return (TResponse)ResultReflection.CreateFailure(typeof(TResponse), exception.Code, exception.Message);
        }
    }
}
