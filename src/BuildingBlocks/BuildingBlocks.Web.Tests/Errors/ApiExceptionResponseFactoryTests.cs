using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Web.Errors;
using Xunit;

namespace BuildingBlocks.Web.Tests.Errors;

public sealed class ApiExceptionResponseFactoryTests
{
    [Fact]
    public void Create_NotFoundException_MapsTo404PayloadWithTraceId()
    {
        const string traceId = "corr-123";
        var exception = new NotFoundException(CategoryErrors.NotFound, "Not found.");

        var response = ApiExceptionResponseFactory.Create(exception, 404, traceId, isDevelopment: false);

        Assert.False(response.Success);
        Assert.Equal(CategoryErrors.NotFound, response.Code);
        Assert.Equal(traceId, response.TraceId);
    }

    [Fact]
    public void Create_ConflictException_MapsTo409PayloadWithTraceId()
    {
        const string traceId = "corr-456";
        var exception = new ConflictException(CategoryErrors.CodeAlreadyExists, "Duplicate.");

        var response = ApiExceptionResponseFactory.Create(exception, 409, traceId, isDevelopment: false);

        Assert.Equal(CategoryErrors.CodeAlreadyExists, response.Code);
        Assert.Equal(traceId, response.TraceId);
    }

    [Fact]
    public void Create_UnauthorizedException_MapsTo401PayloadWithTraceId()
    {
        const string traceId = "corr-unauthorized";
        var exception = new UnauthorizedException(CommonErrors.Unauthorized, "Unauthorized.");

        var response = ApiExceptionResponseFactory.Create(exception, 401, traceId, isDevelopment: false);

        Assert.Equal(CommonErrors.Unauthorized, response.Code);
        Assert.Equal(traceId, response.TraceId);
    }

    [Fact]
    public void Create_ForbiddenException_MapsTo403PayloadWithTraceId()
    {
        const string traceId = "corr-789";
        var exception = new ForbiddenException(CommonErrors.Forbidden, "Forbidden.");

        var response = ApiExceptionResponseFactory.Create(exception, 403, traceId, isDevelopment: false);

        Assert.Equal(CommonErrors.Forbidden, response.Code);
        Assert.Equal(traceId, response.TraceId);
    }

    [Fact]
    public void Create_Unhandled500_InProduction_DoesNotExposeInternalMessage()
    {
        const string traceId = "corr-500";
        var exception = new InvalidOperationException("Npgsql connection refused at db.internal:5432");

        var response = ApiExceptionResponseFactory.Create(exception, 500, traceId, isDevelopment: false);

        Assert.Equal(CommonErrors.UnknownError, response.Code);
        Assert.Equal(ClientSafeErrorMessages.UnexpectedError, response.Message);
        Assert.Equal(traceId, response.TraceId);
    }

    [Fact]
    public void Create_Unhandled500_InDevelopment_ExposesInternalMessage()
    {
        const string traceId = "corr-dev";
        var exception = new InvalidOperationException("Detailed internal failure");

        var response = ApiExceptionResponseFactory.Create(exception, 500, traceId, isDevelopment: true);

        Assert.Equal("Detailed internal failure", response.Message);
    }
}
