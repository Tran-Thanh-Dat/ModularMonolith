using System.Net;
using System.Text.Json;
using ApiHost.IntegrationTests;
using Xunit;

namespace ApiHost.IntegrationTests.Health;

[Collection(ApiHostCollection.Name)]
public sealed class HealthEndpointIntegrationTests
{
    private readonly HttpClient _client;

    public HealthEndpointIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthLive_ReturnsOkWithSelfEntry()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("entries");

        Assert.True(entries.TryGetProperty("self", out _));
    }

    [Fact]
    public async Task GetHealthReady_ReturnsNonEmptyEntries()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Unexpected status code: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("entries");

        Assert.True(entries.EnumerateObject().Any());
    }
}
