using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using BuildingBlocks.Application.Logging;
using BuildingBlocks.Application.Monitoring;
using BuildingBlocks.Web.Constants;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace ApiHost.Middlewares;

public sealed class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly LoggingOptions _loggingOptions;
    private readonly MonitoringOptions _monitoringOptions;
    private readonly SensitiveDataMasker _sensitiveDataMasker;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IOptions<LoggingOptions> loggingOptions,
        IOptions<MonitoringOptions> monitoringOptions,
        SensitiveDataMasker sensitiveDataMasker)
    {
        _next = next;
        _logger = logger;
        _loggingOptions = loggingOptions.Value;
        _monitoringOptions = monitoringOptions.Value;
        _sensitiveDataMasker = sensitiveDataMasker;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;
        var correlationId = ResolveCorrelationId(context);
        var userId = ResolveUserId(context);
        var stopwatch = Stopwatch.StartNew();

        var shouldLogBody = ShouldLogBody(context, path);
        var logRequestBody = shouldLogBody && (_monitoringOptions.IncludeRequestBody || _loggingOptions.EnableRequestBodyLogging);
        var logResponseBody = shouldLogBody && (_monitoringOptions.IncludeResponseBody || _loggingOptions.EnableResponseBodyLogging);
        var originalResponseBody = context.Response.Body;

        using (LogContext.PushProperty("Method", method))
        using (LogContext.PushProperty("Path", path))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        {
            string? requestBody = null;
            if (logRequestBody)
            {
                requestBody = await TryReadRequestBodyAsync(context);
            }

            MemoryStream? responseBodyStream = null;

            try
            {
                if (logResponseBody)
                {
                    responseBodyStream = new MemoryStream();
                    context.Response.Body = responseBodyStream;
                }

                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                if (responseBodyStream is not null)
                {
                    await TryLogAndCopyResponseBodyAsync(context, responseBodyStream, originalResponseBody);
                }
                else
                {
                    context.Response.Body = originalResponseBody;
                }

                var elapsedMs = stopwatch.ElapsedMilliseconds;
                if (elapsedMs >= _monitoringOptions.SlowRequestThresholdMs)
                {
                    _logger.LogWarning(
                        "Slow HTTP request {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds}ms (threshold {SlowRequestThresholdMs}ms) CorrelationId {CorrelationId} UserId {UserId}",
                        method,
                        path,
                        context.Response.StatusCode,
                        elapsedMs,
                        _monitoringOptions.SlowRequestThresholdMs,
                        correlationId,
                        userId);
                }
                else
                {
                    _logger.LogInformation(
                        "HTTP {Method} {Path} {StatusCode} {ElapsedMilliseconds}ms CorrelationId {CorrelationId} UserId {UserId}",
                        method,
                        path,
                        context.Response.StatusCode,
                        elapsedMs,
                        correlationId,
                        userId);
                }

                if (requestBody is not null)
                {
                    _logger.LogInformation(
                        "HTTP request body {Method} {Path}: {RequestBody}",
                        method,
                        path,
                        _sensitiveDataMasker.MaskJson(requestBody));
                }
            }
        }
    }

    private async Task<string?> TryReadRequestBodyAsync(HttpContext context)
    {
        try
        {
            if (IsMultipart(context.Request.ContentType))
            {
                return null;
            }

            context.Request.EnableBuffering();

            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (body.Length > _loggingOptions.MaxBodyLogSize)
            {
                return body[.._loggingOptions.MaxBodyLogSize] + "...(truncated)";
            }

            return body;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to read request body for logging.");
            return null;
        }
    }

    private async Task TryLogAndCopyResponseBodyAsync(
        HttpContext context,
        MemoryStream responseBodyStream,
        Stream originalResponseBody)
    {
        try
        {
            if (IsMultipart(context.Response.ContentType))
            {
                responseBodyStream.Position = 0;
                await responseBodyStream.CopyToAsync(originalResponseBody);
                return;
            }

            responseBodyStream.Position = 0;

            using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                if (body.Length > _loggingOptions.MaxBodyLogSize)
                {
                    body = body[.._loggingOptions.MaxBodyLogSize] + "...(truncated)";
                }

                _logger.LogInformation(
                    "HTTP response body {Method} {Path}: {ResponseBody}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    _sensitiveDataMasker.MaskJson(body));
            }

            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalResponseBody);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to read response body for logging.");
        }
        finally
        {
            context.Response.Body = originalResponseBody;
            await responseBodyStream.DisposeAsync();
        }
    }

    private bool ShouldLogBody(HttpContext context, string path)
    {
        if (_loggingOptions.ExcludedPaths.Any(excluded =>
                path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (context.Request.Headers.Keys.Any(header =>
                SensitiveHeaderGuard.IsSensitiveHeader(header) ||
                _loggingOptions.SensitiveHeaders.Any(sensitiveHeader =>
                    string.Equals(sensitiveHeader, header, StringComparison.OrdinalIgnoreCase))))
        {
            return false;
        }

        return true;
    }

    private static bool IsMultipart(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) &&
        contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase);

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue("CorrelationId", out var value) && value is string correlationId)
        {
            return correlationId;
        }

        if (context.Request.Headers.TryGetValue(HeaderNames.XCorrelationId, out var headerValue))
        {
            return headerValue.ToString();
        }

        return string.Empty;
    }

    private static Guid? ResolveUserId(HttpContext context)
    {
        var userId = context.User.FindFirstValue("sub")
                     ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }
}
