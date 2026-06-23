namespace AuditLogs.Application.Abstractions;



public interface IActivityLogService

{

    Task LogImmediateAsync(

        string activityType,

        string description,

        string moduleName,

        string status,

        string? errorMessage = null,

        Guid? userId = null,

        string? userName = null,

        CancellationToken cancellationToken = default);



    Task EnqueuePostCommitAsync(

        string activityType,

        string description,

        string moduleName,

        Guid? userId = null,

        string? userName = null,

        CancellationToken cancellationToken = default);



    Task FlushPendingAsync(CancellationToken cancellationToken = default);



    Task ClearPendingAsync(CancellationToken cancellationToken = default);



    Task LogLoginSuccessAsync(

        Guid userId,

        string userName,

        CancellationToken cancellationToken = default);



    Task LogLoginFailedAsync(

        string usernameOrEmail,

        string reason,

        CancellationToken cancellationToken = default);



    Task LogLogoutAsync(

        Guid userId,

        string? userName,

        CancellationToken cancellationToken = default);



    Task LogRefreshTokenAsync(

        Guid? userId,

        string? userName,

        string status,

        string? errorMessage = null,

        CancellationToken cancellationToken = default);



    Task LogAuthorizationFailedAsync(

        Guid? userId,

        string? userName,

        string permission,

        CancellationToken cancellationToken = default);



    Task LogErrorAsync(

        string description,

        string moduleName,

        string errorMessage,

        CancellationToken cancellationToken = default);

}

