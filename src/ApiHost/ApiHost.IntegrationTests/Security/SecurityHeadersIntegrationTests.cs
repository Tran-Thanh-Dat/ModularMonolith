using System.Net;
using Xunit;

namespace ApiHost.IntegrationTests.Security;

[Collection(ApiHostCollection.Name)]
public sealed class SecurityHeadersIntegrationTests
{
    private readonly HttpClient _client;

    public SecurityHeadersIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_IncludesSecurityHeaders()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }
}
