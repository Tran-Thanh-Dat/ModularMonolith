using System.Diagnostics;
using System.Text.Json;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int MaxPayloadLogLength = 4096;

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly SensitiveDataMasker _sensitiveDataMasker;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService,
        ICorrelationIdAccessor correlationIdAccessor,
        SensitiveDataMasker sensitiveDataMasker)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _correlationIdAccessor = correlationIdAccessor;
        _sensitiveDataMasker = sensitiveDataMasker;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestFullName = typeof(TRequest).FullName ?? requestName;
        var requestType = ResolveRequestType();
        var correlationId = _correlationIdAccessor.CorrelationId;
        var userId = _currentUserService.UserId;
        var userName = _currentUserService.UserName;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Handling {RequestType} {RequestName} with CorrelationId {CorrelationId}",
            requestType,
            requestName,
            correlationId);

        LogRequestPayloadAtDebug(request, requestType, requestName, correlationId);

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestType} {RequestName} successfully in {ElapsedMilliseconds}ms with CorrelationId {CorrelationId} {UserId} {UserName} {RequestFullName} {Success}",
                requestType,
                requestName,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                userId,
                userName,
                requestFullName,
                true);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            _logger.LogError(
                exception,
                "Failed handling {RequestType} {RequestName} after {ElapsedMilliseconds}ms with CorrelationId {CorrelationId} {UserId} {UserName} {RequestFullName} {Success} {ExceptionType} {ExceptionMessage}",
                requestType,
                requestName,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                userId,
                userName,
                requestFullName,
                false,
                exception.GetType().Name,
                exception.Message);

            throw;
        }
    }

    private void LogRequestPayloadAtDebug(
        TRequest request,
        string requestType,
        string requestName,
        string correlationId)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        try
        {
            var payload = JsonSerializer.Serialize(request);
            if (payload.Length > MaxPayloadLogLength)
            {
                payload = payload[..MaxPayloadLogLength] + "...(truncated)";
            }

            var maskedPayload = _sensitiveDataMasker.MaskJson(payload);

            _logger.LogDebug(
                "Handling {RequestType} {RequestName} payload with CorrelationId {CorrelationId}: {RequestPayload}",
                requestType,
                requestName,
                correlationId,
                maskedPayload);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to serialize request payload for {RequestName} with CorrelationId {CorrelationId}",
                requestName,
                correlationId);
        }
    }

    private static string ResolveRequestType()
    {
        var requestType = typeof(TRequest);

        if (typeof(ICommand).IsAssignableFrom(requestType) ||
            requestType.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)))
        {
            return "Command";
        }

        if (requestType.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
        {
            return "Query";
        }

        return "Request";
    }
}
