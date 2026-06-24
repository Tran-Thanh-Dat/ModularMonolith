using System.Net;
using System.Net.Http.Json;
using ApiHost.IntegrationTests;
using Xunit;

namespace ApiHost.IntegrationTests.AsyncTasks;

[Collection(ApiHostCollection.Name)]
public sealed class AsyncTasksMessageQueueIntegrationTests
{
    private readonly HttpClient _client;

    public AsyncTasksMessageQueueIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MessageQueueDisabled_AppHostStarts_AndHealthLiveReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "Requires PostgreSQL, seeded users, and JWT token. See TESTING.md.")]
    public async Task SubmitWithoutAsyncTaskSubmitPermission_ShouldReturn403()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/async-tasks/email-demo",
            new { emailTo = "demo@example.com" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
