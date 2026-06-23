using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Exceptions;
using DomainException = BuildingBlocks.Domain.Exceptions.DomainException;

namespace ApiHost.Errors;

public static class ApiProblemDetailsFactory
{
    public static object Create(
        HttpContext context,
        Exception exception,
        int statusCode,
        string correlationId,
        string traceId,
        IWebHostEnvironment environment)
    {
        var instance = context.Request.Path.Value;
        var type = $"https://httpstatuses.com/{statusCode}";

        if (exception is ValidationException validationException)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = type,
                ["title"] = "Validation Error",
                ["status"] = statusCode,
                ["detail"] = validationException.Message,
                ["instance"] = instance,
                ["errors"] = validationException.Errors,
                ["traceId"] = traceId,
                ["correlationId"] = correlationId
            };
        }

        var title = GetTitle(statusCode, exception);
        var detail = GetDetail(exception, statusCode, environment);

        var problem = new Dictionary<string, object?>
        {
            ["type"] = type,
            ["title"] = title,
            ["status"] = statusCode,
            ["detail"] = detail,
            ["instance"] = instance,
            ["traceId"] = traceId,
            ["correlationId"] = correlationId
        };

        if (exception is DomainException domainException && !string.IsNullOrWhiteSpace(domainException.ErrorCode))
        {
            problem["errorCode"] = domainException.ErrorCode;
        }

        if (environment.IsDevelopment() && statusCode >= StatusCodes.Status500InternalServerError)
        {
            problem["exception"] = exception.GetType().Name;
            problem["exceptionDetail"] = exception.Message;
        }

        return problem;
    }

    private static string GetTitle(int statusCode, Exception exception) =>
        exception switch
        {
            BadRequestException => "Bad Request",
            UnauthorizedAccessException => "Unauthorized",
            ForbiddenException => "Forbidden",
            NotFoundException => "Not Found",
            ConflictException => "Conflict",
            DomainException => "Domain Error",
            TimeoutException => "Request Timeout",
            _ => statusCode switch
            {
                StatusCodes.Status500InternalServerError => "Internal Server Error",
                _ => "Error"
            }
        };

    private static string GetDetail(Exception exception, int statusCode, IWebHostEnvironment environment)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError && !environment.IsDevelopment())
        {
            return "An unexpected error occurred.";
        }

        return exception.Message;
    }
}
