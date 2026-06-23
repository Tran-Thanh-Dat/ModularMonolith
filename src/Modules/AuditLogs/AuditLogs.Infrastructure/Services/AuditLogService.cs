using System.Text.Json;
using AuditLogs.Application.Abstractions;
using AuditLogs.Application.AuditLogs.CreateAuditLog;
using AuditLogs.Domain.AuditLogs;
using AuditLogs.Domain.Constants;
using AuditLogs.Infrastructure.Persistence;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuditLogs.Infrastructure.Services;

/// <summary>
/// Audit logging is non-blocking by design. Change this behavior if audit logging is compliance-critical.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly AuditLogsDbContext _dbContext;
    private readonly IRequestContextService _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly SensitiveDataMasker _sensitiveDataMasker;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public AuditLogService(
        AuditLogsDbContext dbContext,
        IRequestContextService requestContext,
        IDateTimeProvider dateTimeProvider,
        SensitiveDataMasker sensitiveDataMasker,
        ILogger<AuditLogService> logger,
        IHostEnvironment hostEnvironment)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _sensitiveDataMasker = sensitiveDataMasker;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    public async Task CreateAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = AuditLog.Create(
                entry.UserId ?? _requestContext.UserId,
                entry.UserName ?? _requestContext.UserName,
                entry.Action,
                entry.ModuleName,
                _dateTimeProvider.UtcNow,
                entry.Status,
                entry.EntityName,
                entry.EntityId,
                SerializeValues(entry.OldValues),
                SerializeValues(entry.NewValues),
                SerializeValues(entry.ChangedColumns),
                entry.RequestPath ?? _requestContext.RequestPath,
                entry.HttpMethod ?? _requestContext.HttpMethod,
                entry.IpAddress ?? _requestContext.IpAddress,
                entry.UserAgent ?? _requestContext.UserAgent,
                TruncateErrorMessage(entry.ErrorMessage));

            await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to create audit log for {ModuleName} {Action} {EntityName} {EntityId}",
                entry.ModuleName,
                entry.Action,
                entry.EntityName,
                entry.EntityId);

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogWarning(
                    "Audit log persistence failed in Development for {ModuleName} {Action}. Ensure AuditLogs migrations are applied.",
                    entry.ModuleName,
                    entry.Action);
            }
        }
    }

    internal async Task CreateManyAsync(
        IReadOnlyCollection<AuditLog> auditLogs,
        CancellationToken cancellationToken = default)
    {
        if (auditLogs.Count == 0)
        {
            return;
        }

        try
        {
            await _dbContext.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist {Count} automatic audit log entries",
                auditLogs.Count);

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogWarning(
                    "Audit log persistence failed in Development. Ensure AuditLogs migrations are applied.");
            }
        }
    }

    private string? SerializeValues(object? values)
    {
        if (values is null)
        {
            return null;
        }

        if (values is string stringValue)
        {
            return TruncateJson(_sensitiveDataMasker.MaskJson(stringValue));
        }

        var json = JsonSerializer.Serialize(values);
        var masked = _sensitiveDataMasker.MaskJson(json);
        return TruncateJson(masked);
    }

    private static string? TruncateJson(string? json)
    {
        if (string.IsNullOrEmpty(json) || json.Length <= AuditLogValueLimits.MaxJsonLength)
        {
            return json;
        }

        return json[..AuditLogValueLimits.MaxJsonLength];
    }

    private static string? TruncateErrorMessage(string? errorMessage) =>
        string.IsNullOrWhiteSpace(errorMessage)
            ? null
            : errorMessage.Length <= 2000
                ? errorMessage
                : errorMessage[..2000];
}
