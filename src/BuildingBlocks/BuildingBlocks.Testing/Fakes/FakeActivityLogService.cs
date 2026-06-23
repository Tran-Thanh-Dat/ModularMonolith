using AuditLogs.Application.Abstractions;

namespace BuildingBlocks.Testing.Fakes;

public sealed class FakeActivityLogService : IActivityLogService
{
    public List<(string ActivityType, string Description, string ModuleName)> PostCommitEntries { get; } = [];

    public List<(string ActivityType, string Description, string ModuleName, string Status)> ImmediateEntries { get; } = [];

    public int LoginFailedCount { get; private set; }

    public int LoginSuccessCount { get; private set; }

    public Task EnqueuePostCommitAsync(
        string activityType,
        string description,
        string moduleName,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        PostCommitEntries.Add((activityType, description, moduleName));
        return Task.CompletedTask;
    }

    public Task FlushPendingAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ClearPendingAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task LogImmediateAsync(
        string activityType,
        string description,
        string moduleName,
        string status,
        string? errorMessage = null,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        ImmediateEntries.Add((activityType, description, moduleName, status));
        return Task.CompletedTask;
    }

    public Task LogLoginSuccessAsync(
        Guid userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        LoginSuccessCount++;
        return Task.CompletedTask;
    }

    public Task LogLoginFailedAsync(
        string usernameOrEmail,
        string reason,
        CancellationToken cancellationToken = default)
    {
        LoginFailedCount++;
        return Task.CompletedTask;
    }

    public Task LogLogoutAsync(
        Guid userId,
        string? userName,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task LogRefreshTokenAsync(
        Guid? userId,
        string? userName,
        string status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task LogAuthorizationFailedAsync(
        Guid? userId,
        string? userName,
        string permission,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task LogErrorAsync(
        string description,
        string moduleName,
        string errorMessage,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
