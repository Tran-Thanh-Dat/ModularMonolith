using AuditLogs.Domain.Constants;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace AuditLogs.Domain.ActivityLogs;

public sealed class ActivityLog : Entity
{
    private ActivityLog()
    {
    }

    private ActivityLog(
        Guid id,
        Guid? userId,
        string? userName,
        string activityType,
        string description,
        string moduleName,
        string? requestPath,
        string? httpMethod,
        string? ipAddress,
        string? userAgent,
        string status,
        string? errorMessage,
        DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        UserName = userName;
        ActivityType = activityType;
        Description = description;
        ModuleName = moduleName;
        RequestPath = requestPath;
        HttpMethod = httpMethod;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Status = status;
        ErrorMessage = errorMessage;
        CreatedAt = createdAt;
    }

    public Guid? UserId { get; private set; }

    public string? UserName { get; private set; }

    public string ActivityType { get; private set; } = default!;

    public string Description { get; private set; } = default!;

    public string ModuleName { get; private set; } = default!;

    public string? RequestPath { get; private set; }

    public string? HttpMethod { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string Status { get; private set; } = default!;

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ActivityLog Create(
        Guid? userId,
        string? userName,
        string activityType,
        string description,
        string moduleName,
        DateTimeOffset createdAt,
        string status = AuditLogStatus.Success,
        string? requestPath = null,
        string? httpMethod = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(activityType))
        {
            throw new DomainException("Activity type is required.", "ActivityLog.InvalidActivityType");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description is required.", "ActivityLog.InvalidDescription");
        }

        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new DomainException("Module name is required.", "ActivityLog.InvalidModuleName");
        }

        return new ActivityLog(
            Guid.NewGuid(),
            userId,
            userName,
            activityType.Trim(),
            Truncate(description.Trim(), AuditLogValueLimits.MaxDescriptionLength),
            moduleName.Trim(),
            requestPath,
            httpMethod,
            ipAddress,
            userAgent,
            status.Trim(),
            string.IsNullOrWhiteSpace(errorMessage)
                ? null
                : Truncate(errorMessage.Trim(), AuditLogValueLimits.MaxErrorMessageLength),
            createdAt);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
