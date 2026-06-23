using System.Net;
using System.Text.Json;
using ApiHost.IntegrationTests;
using Xunit;

namespace ApiHost.IntegrationTests.Diagnostics;

[Collection(ApiHostCollection.Name)]
public sealed class ExceptionTraceIdIntegrationTests
{
    private readonly HttpClient _client;

    public ExceptionTraceIdIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DiagnosticsNotFound_ReturnsApiResponseWithCorrelationId()
    {
        const string correlationId = "integration-correlation-id";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/diagnostics/not-found");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var traceId = document.RootElement.GetProperty("traceId").GetString();

        Assert.Equal(correlationId, traceId);
    }
}
