using BuildingBlocks.Application.Monitoring;

namespace BuildingBlocks.Infrastructure.Health;

public static class HealthCheckTagMatcher
{
    public static string[] ResolveLiveTags(HealthCheckTagOptions tagOptions) =>
        tagOptions.Live.Length > 0 ? tagOptions.Live : [HealthCheckTags.Live];

    public static string[] ResolveDependencyTags(HealthCheckTagOptions tagOptions) =>
        tagOptions.Dependency.Length > 0
            ? tagOptions.Dependency
            : [HealthCheckTags.Dependency, HealthCheckTags.Ready];

    /// <summary>
    /// Ready endpoint matches checks tagged as ready OR dependency.
    /// </summary>
    public static string[] ResolveReadyMatchTags(HealthCheckTagOptions tagOptions)
    {
        var ready = tagOptions.Ready.Length > 0 ? tagOptions.Ready : [HealthCheckTags.Ready];
        var dependency = tagOptions.Dependency.Length > 0 ? tagOptions.Dependency : [HealthCheckTags.Dependency];

        return ready
            .Concat(dependency)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool MatchesAnyTag(IEnumerable<string> registrationTags, IEnumerable<string> matchTags) =>
        registrationTags.Any(tag => matchTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
}
