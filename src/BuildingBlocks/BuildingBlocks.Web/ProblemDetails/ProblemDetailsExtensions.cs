using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Web.ProblemDetails;

public static class ProblemDetailsExtensions
{
    public const string CorrelationIdKey = "correlationId";

    public static ValidationProblemDetails WithCorrelationId(
        this ValidationProblemDetails problemDetails,
        string? correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            problemDetails.Extensions[CorrelationIdKey] = correlationId;
        }

        return problemDetails;
    }

    public static Microsoft.AspNetCore.Mvc.ProblemDetails WithCorrelationId(
        this Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails,
        string? correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            problemDetails.Extensions[CorrelationIdKey] = correlationId;
        }

        return problemDetails;
    }

    public static Microsoft.AspNetCore.Mvc.ProblemDetails WithErrorCode(
        this Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails,
        string errorCode)
    {
        problemDetails.Extensions["errorCode"] = errorCode;
        return problemDetails;
    }

    public static Microsoft.AspNetCore.Mvc.ProblemDetails WithValidationErrors(
        this Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails,
        IDictionary<string, string[]> errors)
    {
        problemDetails.Extensions["errors"] = errors;
        return problemDetails;
    }

    public static ValidationProblemDetails CreateValidationProblemDetails(
        string title,
        IDictionary<string, string[]> errors,
        string? correlationId = null,
        int statusCode = StatusCodes.Status400BadRequest)
    {
        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = statusCode,
            Title = title,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        return problemDetails.WithCorrelationId(correlationId);
    }
}
