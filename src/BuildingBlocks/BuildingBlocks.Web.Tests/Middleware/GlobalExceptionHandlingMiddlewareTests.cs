using System.Text.Json;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Web.Middleware;
using BuildingBlocks.Web.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingBlocks.Web.Tests.Middleware;

public sealed class GlobalExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UsesCorrelationIdInApiResponseTraceId()
    {
        const string correlationId = "client-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = correlationId;
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new NotFoundException(
            CategoryErrors.NotFound,
            "Category missing.");

        var middleware = new GlobalExceptionHandlingMiddleware(
            next,
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance,
            new TestHostEnvironment());

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(response);
        Assert.Equal(correlationId, response!.TraceId);
        Assert.Equal(correlationId, context.Response.Headers["X-Correlation-Id"].ToString());
        Assert.Equal(404, context.Response.StatusCode);
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;

        public string WebRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
    }
}
