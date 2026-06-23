using System.Security.Claims;
using System.Text.Json;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Infrastructure.Http;
using BuildingBlocks.Web.Constants;
using BuildingBlocks.Web.Errors;
using BuildingBlocks.Web.Responses;
using DomainException = BuildingBlocks.Domain.Exceptions.DomainException;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Web.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    exception,
                    "The response has already started. Unable to write ApiResponse.");

                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = HttpContextTraceIdResolver.Resolve(context);
        var userId = ResolveUserId(context);
        var statusCode = MapStatusCode(exception);

        LogException(context, exception, traceId, userId, statusCode);

        var apiResponse = ApiExceptionResponseFactory.Create(
            exception,
            statusCode,
            traceId,
            _environment.IsDevelopment());

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            context.Response.Headers[HeaderNames.XCorrelationId] = traceId;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(apiResponse, JsonOptions));
    }

    private void LogException(
        HttpContext context,
        Exception exception,
        string traceId,
        Guid? userId,
        int statusCode)
    {
        _logger.LogError(
            exception,
            "Unhandled exception occurred. {ExceptionType} {ExceptionMessage} {CorrelationId} {UserId} {Method} {Path} {StatusCode} {IpAddress} {UserAgent}",
            exception.GetType().Name,
            exception.Message,
            traceId,
            userId,
            context.Request.Method,
            context.Request.Path.Value,
            statusCode,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString());
    }

    private static int MapStatusCode(Exception exception) =>
        exception switch
        {
            ValidationException validationException => ResultStatusMapper.MapStatusCode(validationException.Code),
            BusinessException businessException => ResultStatusMapper.MapStatusCode(businessException.Code),
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            DomainException => StatusCodes.Status400BadRequest,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            DbUpdateException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

    private static Guid? ResolveUserId(HttpContext context)
    {
        var userId = context.User.FindFirstValue("sub")
                     ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }
}
