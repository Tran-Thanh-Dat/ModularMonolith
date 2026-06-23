using AuditLogs.Domain.Constants;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace AuditLogs.Domain.AuditLogs;

public sealed class AuditLog : Entity
{
    private AuditLog()
    {
    }

    private AuditLog(
        Guid id,
        Guid? userId,
        string? userName,
        string action,
        string? entityName,
        string? entityId,
        string moduleName,
        string? oldValues,
        string? newValues,
        string? changedColumns,
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
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        ModuleName = moduleName;
        OldValues = oldValues;
        NewValues = newValues;
        ChangedColumns = changedColumns;
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

    public string Action { get; private set; } = default!;

    public string? EntityName { get; private set; }

    public string? EntityId { get; private set; }

    public string ModuleName { get; private set; } = default!;

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }

    public string? ChangedColumns { get; private set; }

    public string? RequestPath { get; private set; }

    public string? HttpMethod { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string Status { get; private set; } = default!;

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditLog Create(
        Guid? userId,
        string? userName,
        string action,
        string moduleName,
        DateTimeOffset createdAt,
        string status = AuditLogStatus.Success,
        string? entityName = null,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? changedColumns = null,
        string? requestPath = null,
        string? httpMethod = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainException("Action is required.", "AuditLog.InvalidAction");
        }

        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new DomainException("Module name is required.", "AuditLog.InvalidModuleName");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new DomainException("Status is required.", "AuditLog.InvalidStatus");
        }

        return new AuditLog(
            Guid.NewGuid(),
            userId,
            userName,
            action.Trim(),
            entityName,
            entityId,
            moduleName.Trim(),
            oldValues,
            newValues,
            changedColumns,
            requestPath,
            httpMethod,
            ipAddress,
            userAgent,
            status.Trim(),
            errorMessage,
            createdAt);
    }
}
