using System.Reflection;
using BackgroundJobs.Infrastructure.Jobs;
using Hangfire;
using Xunit;

namespace BackgroundJobs.Infrastructure.Tests.Jobs;

public sealed class BackgroundJobAttributesTests
{
    [Theory]
    [InlineData(typeof(EmailRetryJob), nameof(EmailRetryJob.ExecuteAsync))]
    [InlineData(typeof(TemporaryFileCleanupJob), nameof(TemporaryFileCleanupJob.ExecuteAsync))]
    [InlineData(typeof(LogCleanupJob), nameof(LogCleanupJob.ExecuteAsync))]
    public void ExecuteAsync_HasDisableConcurrentExecutionAttribute(Type jobType, string methodName)
    {
        var method = jobType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var attribute = method!.GetCustomAttribute<DisableConcurrentExecutionAttribute>();
        Assert.NotNull(attribute);
    }
}
