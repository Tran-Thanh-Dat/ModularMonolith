using AuditLogs.Application.Abstractions;

using AuditLogs.Domain.ActivityLogs;

using AuditLogs.Domain.Constants;

using AuditLogs.Infrastructure.Persistence;

using BuildingBlocks.Application.Abstractions;

using Microsoft.Extensions.Hosting;

using Microsoft.Extensions.Logging;



namespace AuditLogs.Infrastructure.Services;



/// <summary>

/// Activity logging is non-blocking by design. Change this behavior if activity logging is compliance-critical.

/// </summary>

public sealed class ActivityLogService : IActivityLogService

{

    private readonly AuditLogsDbContext _dbContext;

    private readonly IRequestContextService _requestContext;

    private readonly ICurrentUserService _currentUserService;

    private readonly IActivityLogBuffer _buffer;

    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly ILogger<ActivityLogService> _logger;

    private readonly IHostEnvironment _hostEnvironment;



    public ActivityLogService(

        AuditLogsDbContext dbContext,

        IRequestContextService requestContext,

        ICurrentUserService currentUserService,

        IActivityLogBuffer buffer,

        IDateTimeProvider dateTimeProvider,

        ILogger<ActivityLogService> logger,

        IHostEnvironment hostEnvironment)

    {

        _dbContext = dbContext;

        _requestContext = requestContext;

        _currentUserService = currentUserService;

        _buffer = buffer;

        _dateTimeProvider = dateTimeProvider;

        _logger = logger;

        _hostEnvironment = hostEnvironment;

    }



    public Task LogImmediateAsync(

        string activityType,

        string description,

        string moduleName,

        string status,

        string? errorMessage = null,

        Guid? userId = null,

        string? userName = null,

        CancellationToken cancellationToken = default) =>

        PersistImmediateAsync(

            userId ?? _requestContext.UserId,

            userName ?? _requestContext.UserName,

            activityType,

            description,

            moduleName,

            status,

            errorMessage,

            cancellationToken);



    public Task EnqueuePostCommitAsync(

        string activityType,

        string description,

        string moduleName,

        Guid? userId = null,

        string? userName = null,

        CancellationToken cancellationToken = default)

    {

        _buffer.Enqueue(new PendingActivityLogEntry(

            userId ?? _requestContext.UserId,

            userName ?? _requestContext.UserName,

            activityType,

            description,

            moduleName,

            AuditLogStatus.Success,

            null));



        return Task.CompletedTask;

    }



    public async Task FlushPendingAsync(CancellationToken cancellationToken = default)

    {

        var entries = _buffer.TakeAll();

        if (entries.Count == 0)

        {

            return;

        }



        try

        {

            var activityLogs = entries

                .Select(entry => ActivityLog.Create(

                    entry.UserId,

                    entry.UserName,

                    entry.ActivityType,

                    entry.Description,

                    entry.ModuleName,

                    _dateTimeProvider.UtcNow,

                    entry.Status,

                    _requestContext.RequestPath,

                    _requestContext.HttpMethod,

                    _requestContext.IpAddress,

                    _requestContext.UserAgent,

                    entry.ErrorMessage))

                .ToList();



            await _dbContext.ActivityLogs.AddRangeAsync(activityLogs, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

        }

        catch (Exception exception)

        {

            LogPersistenceFailure(exception, "pending batch", entries.Count);

        }

    }



    public Task ClearPendingAsync(CancellationToken cancellationToken = default)

    {

        _buffer.Clear();

        return Task.CompletedTask;

    }



    public Task LogLoginSuccessAsync(

        Guid userId,

        string userName,

        CancellationToken cancellationToken = default) =>

        EnqueuePostCommitAsync(

            ActivityTypes.Login,

            $"User '{userName}' logged in successfully.",

            AuditLogConstants.Modules.Identity,

            userId,

            userName,

            cancellationToken);



    public Task LogLoginFailedAsync(

        string usernameOrEmail,

        string reason,

        CancellationToken cancellationToken = default) =>

        LogImmediateAsync(

            ActivityTypes.FailedLogin,

            $"Login failed for '{usernameOrEmail}'.",

            AuditLogConstants.Modules.Identity,

            AuditLogStatus.Failed,

            reason,

            null,

            usernameOrEmail,

            cancellationToken);



    public Task LogLogoutAsync(

        Guid userId,

        string? userName,

        CancellationToken cancellationToken = default)

    {

        var resolvedUserName = ResolveUserName(userId, userName);



        return EnqueuePostCommitAsync(

            ActivityTypes.Logout,

            $"User '{resolvedUserName}' logged out.",

            AuditLogConstants.Modules.Identity,

            userId,

            resolvedUserName,

            cancellationToken);

    }



    public Task LogRefreshTokenAsync(

        Guid? userId,

        string? userName,

        string status,

        string? errorMessage = null,

        CancellationToken cancellationToken = default)

    {

        var resolvedUserName = userName ?? userId?.ToString() ?? "unknown";

        var description = status == AuditLogStatus.Success

            ? $"Refresh token rotated for user '{resolvedUserName}'."

            : "Refresh token failed.";



        if (status == AuditLogStatus.Failed)

        {

            return LogImmediateAsync(

                ActivityTypes.RefreshToken,

                description,

                AuditLogConstants.Modules.Identity,

                status,

                errorMessage,

                userId,

                userName,

                cancellationToken);

        }



        return EnqueuePostCommitAsync(

            ActivityTypes.RefreshToken,

            description,

            AuditLogConstants.Modules.Identity,

            userId,

            userName,

            cancellationToken);

    }



    public Task LogAuthorizationFailedAsync(

        Guid? userId,

        string? userName,

        string permission,

        CancellationToken cancellationToken = default) =>

        LogImmediateAsync(

            ActivityTypes.AuthorizationFailed,

            $"Authorization failed for permission '{permission}'.",

            AuditLogConstants.Modules.Identity,

            AuditLogStatus.Failed,

            null,

            userId,

            userName,

            cancellationToken);



    public Task LogErrorAsync(

        string description,

        string moduleName,

        string errorMessage,

        CancellationToken cancellationToken = default) =>

        LogImmediateAsync(

            ActivityTypes.SystemError,

            description,

            moduleName,

            AuditLogStatus.Failed,

            errorMessage,

            cancellationToken: cancellationToken);



    private async Task PersistImmediateAsync(

        Guid? userId,

        string? userName,

        string activityType,

        string description,

        string moduleName,

        string status,

        string? errorMessage,

        CancellationToken cancellationToken)

    {

        try

        {

            var activityLog = ActivityLog.Create(

                userId,

                userName,

                activityType,

                description,

                moduleName,

                _dateTimeProvider.UtcNow,

                status,

                _requestContext.RequestPath,

                _requestContext.HttpMethod,

                _requestContext.IpAddress,

                _requestContext.UserAgent,

                errorMessage);



            await _dbContext.ActivityLogs.AddAsync(activityLog, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

        }

        catch (Exception exception)

        {

            LogPersistenceFailure(exception, activityType);

        }

    }



    private string ResolveUserName(Guid userId, string? userName)

    {

        if (!string.IsNullOrWhiteSpace(userName))

        {

            return userName;

        }



        if (!string.IsNullOrWhiteSpace(_currentUserService.UserName))

        {

            return _currentUserService.UserName;

        }



        if (_currentUserService.UserId == userId &&

            !string.IsNullOrWhiteSpace(_requestContext.UserName) &&

            !string.Equals(_requestContext.UserName, "system", StringComparison.OrdinalIgnoreCase))

        {

            return _requestContext.UserName!;

        }



        return userId.ToString();

    }



    private void LogPersistenceFailure(Exception exception, string context, int? batchCount = null)

    {

        if (batchCount.HasValue)

        {

            _logger.LogError(

                exception,

                "Failed to persist {Count} post-commit activity log entries",

                batchCount.Value);

        }

        else

        {

            _logger.LogError(

                exception,

                "Failed to create activity log {ActivityType} for module",

                context);

        }



        if (_hostEnvironment.IsDevelopment())

        {

            _logger.LogWarning(

                "Activity log persistence failed in Development ({Context}). Ensure AuditLogs migrations are applied.",

                context);

        }

    }

}

