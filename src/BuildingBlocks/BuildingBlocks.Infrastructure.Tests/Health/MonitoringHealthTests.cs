using BuildingBlocks.Application.Monitoring;
using BuildingBlocks.Infrastructure.Health;
using Xunit;

namespace BuildingBlocks.Infrastructure.Tests.Health;

public sealed class HealthCheckTagMatcherTests
{
    [Fact]
    public void ResolveReadyMatchTags_IncludesReadyAndDependencyTags()
    {
        var tagOptions = new HealthCheckTagOptions
        {
            Ready = ["ready"],
            Dependency = ["dependency"]
        };

        var matchTags = HealthCheckTagMatcher.ResolveReadyMatchTags(tagOptions);

        Assert.Contains("ready", matchTags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("dependency", matchTags, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void MatchesAnyTag_ReturnsTrueWhenDependencyCheckMatchesReadyEndpointTags()
    {
        var tagOptions = new HealthCheckTagOptions
        {
            Ready = ["ready"],
            Dependency = ["dependency"]
        };

        var readyMatchTags = HealthCheckTagMatcher.ResolveReadyMatchTags(tagOptions);
        var registrationTags = new[] { "dependency" };

        Assert.True(HealthCheckTagMatcher.MatchesAnyTag(registrationTags, readyMatchTags));
    }

    [Fact]
    public void MatchesAnyTag_LiveEndpointMatchesOnlyLiveTaggedChecks()
    {
        var tagOptions = new HealthCheckTagOptions
        {
            Live = ["live"],
            Dependency = ["dependency"]
        };

        var liveTags = HealthCheckTagMatcher.ResolveLiveTags(tagOptions);

        Assert.True(HealthCheckTagMatcher.MatchesAnyTag(["live"], liveTags));
        Assert.False(HealthCheckTagMatcher.MatchesAnyTag(["dependency"], liveTags));
    }
}

public sealed class HealthCheckDescriptionSanitizerTests
{
    [Theory]
    [InlineData("SMTP configuration present for host 'localhost:587'.")]
    [InlineData("Connection string Host=localhost;Password=secret")]
    [InlineData("Hangfire storage reachable with 2 server(s) registered.")]
    [InlineData(@"File path C:\uploads\probe.tmp failed")]
    public void SanitizeForPublicResponse_RedactsOperationalDetails(string description)
    {
        var sanitized = HealthCheckDescriptionSanitizer.SanitizeForPublicResponse(description);

        Assert.Equal("Check completed.", sanitized);
    }

    [Fact]
    public void SanitizeForPublicResponse_AllowsGenericHealthyMessage()
    {
        var sanitized = HealthCheckDescriptionSanitizer.SanitizeForPublicResponse(
            "Application process is running.");

        Assert.Equal("Application process is running.", sanitized);
    }

    [Fact]
    public void SanitizeForAdminResponse_RedactsSecretsButKeepsOperationalMessage()
    {
        var sanitized = HealthCheckDescriptionSanitizer.SanitizeForAdminResponse(
            "Hangfire storage is reachable with active servers.");

        Assert.Equal("Hangfire storage is reachable with active servers.", sanitized);
    }
}
