using System.Net;
using System.Net.Http.Json;
using ApiHost.IntegrationTests;
using Xunit;

namespace ApiHost.IntegrationTests.Business;

[Collection(ApiHostCollection.Name)]
public sealed class BusinessHttpIntegrationTests
{
    private readonly HttpClient _client;

    public BusinessHttpIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(Skip = "Requires PostgreSQL, seeded admin user, and JWT token. See TESTING.md.")]
    public async Task CreateCategory_WhenDuplicateCode_Returns409()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/categories",
            new { code = "CAT-DUP", name = "Duplicate", description = (string?)null, sortOrder = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(Skip = "Requires PostgreSQL, seeded users, and JWT token. See TESTING.md.")]
    public async Task GetNotificationById_WhenCrossUserWithoutViewAll_Returns403()
    {
        var response = await _client.GetAsync($"/api/v1/notifications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(Skip = "Requires PostgreSQL, BackgroundJobs enabled, admin JWT, and job permissions. See TESTING.md.")]
    public async Task RunEmailRetryJob_WhenJobFails_Returns200WithFailedStatus()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/background-jobs/email-retry/run",
            new { batchSize = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Failed\"", json, StringComparison.OrdinalIgnoreCase);
    }
}
