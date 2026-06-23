using System.Net;
using ApiHost.IntegrationTests;
using Xunit;

namespace ApiHost.IntegrationTests.Monitoring;

[Collection(ApiHostCollection.Name)]
public sealed class MonitoringAuthorizationIntegrationTests
{
    private readonly HttpClient _client;

    public MonitoringAuthorizationIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthDetails_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/monitoring/health/details");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
