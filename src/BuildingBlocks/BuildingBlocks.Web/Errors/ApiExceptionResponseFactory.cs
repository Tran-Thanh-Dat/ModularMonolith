using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Web.Responses;
using DomainException = BuildingBlocks.Domain.Exceptions.DomainException;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Web.Errors;

public static class ApiExceptionResponseFactory
{
    public static ApiResponse<object> Create(
        Exception exception,
        int statusCode,
        string traceId,
        bool isDevelopment)
    {
        return exception switch
        {
            ValidationException validationException => ApiResponse<object>.Fail(
                validationException.Code,
                validationException.Message,
                validationException.Errors.Select(error => new ApiError
                {
                    Code = error.Code,
                    Message = error.Message,
                    Field = error.Field
                }).ToList(),
                traceId),

            BusinessException businessException => ApiResponse<object>.Fail(
                businessException.Code,
                businessException.Message,
                traceId: traceId),

            UnauthorizedAccessException unauthorizedException => ApiResponse<object>.Fail(
                CommonErrors.Unauthorized,
                unauthorizedException.Message,
                traceId: traceId),

            DomainException domainException => ApiResponse<object>.Fail(
                domainException.ErrorCode ?? CommonErrors.BadRequest,
                domainException.Message,
                traceId: traceId),

            _ when statusCode >= StatusCodes.Status500InternalServerError => ApiResponse<object>.Fail(
                CommonErrors.UnknownError,
                isDevelopment ? exception.Message : ClientSafeErrorMessages.UnexpectedError,
                traceId: traceId),

            _ => ApiResponse<object>.Fail(
                CommonErrors.UnknownError,
                isDevelopment ? exception.Message : ClientSafeErrorMessages.UnexpectedError,
                traceId: traceId)
        };
    }
}
