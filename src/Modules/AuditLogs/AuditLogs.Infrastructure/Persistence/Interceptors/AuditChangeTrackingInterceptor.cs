using System.Text.Json;
using AuditLogs.Domain.AuditLogs;
using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using AuditLogs.Infrastructure.Persistence.Interceptors;
using AuditLogs.Infrastructure.Services;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Logging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuditLogs.Infrastructure.Persistence.Interceptors;

public sealed class AuditChangeTrackingInterceptor : SaveChangesInterceptor
{
    private readonly IRequestContextService _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly SensitiveDataMasker _sensitiveDataMasker;
    private readonly IAuditChangeBuffer _changeBuffer;
    private readonly AuditLogService _auditLogService;

    public AuditChangeTrackingInterceptor(
        IRequestContextService requestContext,
        IDateTimeProvider dateTimeProvider,
        SensitiveDataMasker sensitiveDataMasker,
        IAuditChangeBuffer changeBuffer,
        AuditLogService auditLogService)
    {
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _sensitiveDataMasker = sensitiveDataMasker;
        _changeBuffer = changeBuffer;
        _auditLogService = auditLogService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        HandleSavedChanges(eventData.Context, result);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await HandleSavedChangesAsync(eventData.Context, result, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void HandleSavedChanges(DbContext? context, int result)
    {
        if (result > 0 &&
            context is not null &&
            !string.Equals(context.GetType().Name, nameof(AuditLogsDbContext), StringComparison.Ordinal))
        {
            var auditLogs = _changeBuffer.TakePendingEntries();
            _auditLogService.CreateManyAsync(auditLogs, CancellationToken.None).GetAwaiter().GetResult();
        }
        else
        {
            _changeBuffer.TakePendingEntries();
        }
    }

    private async Task HandleSavedChangesAsync(
        DbContext? context,
        int result,
        CancellationToken cancellationToken)
    {
        if (result > 0 &&
            context is not null &&
            !string.Equals(context.GetType().Name, nameof(AuditLogsDbContext), StringComparison.Ordinal))
        {
            var auditLogs = _changeBuffer.TakePendingEntries();
            await _auditLogService.CreateManyAsync(auditLogs, cancellationToken);
        }
        else
        {
            _changeBuffer.TakePendingEntries();
        }
    }

    private void CaptureChanges(DbContext? context)
    {
        if (context is null || string.Equals(context.GetType().Name, nameof(AuditLogsDbContext), StringComparison.Ordinal))
        {
            return;
        }

        var auditLogs = BuildAuditLogs(context.ChangeTracker.Entries());
        _changeBuffer.SetPendingEntries(auditLogs);
    }

    private IReadOnlyCollection<AuditLog> BuildAuditLogs(IEnumerable<EntityEntry> entries)
    {
        var auditLogs = new List<AuditLog>();
        var createdAt = _dateTimeProvider.UtcNow;

        foreach (var entry in entries)
        {
            if (!ShouldAudit(entry))
            {
                continue;
            }

            var action = ResolveAction(entry);
            if (action is null)
            {
                continue;
            }

            var (oldValues, newValues, changedColumns) = ExtractValues(entry, action);

            auditLogs.Add(AuditLog.Create(
                _requestContext.UserId,
                _requestContext.UserName,
                action,
                ResolveModuleName(entry),
                createdAt,
                AuditLogStatus.Success,
                entry.Entity.GetType().Name,
                ResolveEntityId(entry),
                oldValues,
                newValues,
                changedColumns,
                _requestContext.RequestPath,
                _requestContext.HttpMethod,
                _requestContext.IpAddress,
                _requestContext.UserAgent));
        }

        return auditLogs;
    }

    private static bool ShouldAudit(EntityEntry entry)
    {
        if (entry.Entity is AuditLog or Domain.ActivityLogs.ActivityLog)
        {
            return false;
        }

        var entityName = entry.Entity.GetType().Name;
        if (AuditLogConstants.ExcludedEntityTypes.Names.Contains(entityName))
        {
            return false;
        }

        return entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
    }

    private static string? ResolveAction(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return AuditLogConstants.Actions.Create;
        }

        if (entry.State == EntityState.Deleted)
        {
            return AuditLogConstants.Actions.Delete;
        }

        if (entry.State == EntityState.Modified)
        {
            if (entry.Entity is SoftDeleteEntity or SoftDeletableEntity)
            {
                var isDeletedProperty = entry.Properties.FirstOrDefault(property => property.Metadata.Name == nameof(SoftDeleteEntity.IsDeleted));
                if (isDeletedProperty?.IsModified == true &&
                    isDeletedProperty.OriginalValue is false &&
                    isDeletedProperty.CurrentValue is true)
                {
                    return AuditLogConstants.Actions.Delete;
                }
            }

            var hasMeaningfulChanges = entry.Properties.Any(property =>
                property.IsModified &&
                !SensitiveAuditFields.IsSensitive(property.Metadata.Name) &&
                !property.Metadata.IsPrimaryKey());

            return hasMeaningfulChanges ? AuditLogConstants.Actions.Update : null;
        }

        return null;
    }

    private (string? OldValues, string? NewValues, string? ChangedColumns) ExtractValues(
        EntityEntry entry,
        string action)
    {
        var properties = entry.Properties
            .Where(property => !property.Metadata.IsPrimaryKey())
            .Where(property => !property.Metadata.IsShadowProperty())
            .Where(property => !SensitiveAuditFields.IsSensitive(property.Metadata.Name))
            .ToList();

        if (action == AuditLogConstants.Actions.Create)
        {
            var newValues = properties.ToDictionary(
                property => property.Metadata.Name,
                property => property.CurrentValue);

            return (null, MaskJson(newValues), null);
        }

        if (action == AuditLogConstants.Actions.Delete)
        {
            var oldValues = properties.ToDictionary(
                property => property.Metadata.Name,
                property => entry.State == EntityState.Deleted
                    ? property.OriginalValue
                    : property.CurrentValue ?? property.OriginalValue);

            return (MaskJson(oldValues), null, null);
        }

        var changed = properties
            .Where(property => property.IsModified)
            .ToList();

        if (changed.Count == 0)
        {
            return (null, null, null);
        }

        var oldDict = changed.ToDictionary(
            property => property.Metadata.Name,
            property => property.OriginalValue);

        var newDict = changed.ToDictionary(
            property => property.Metadata.Name,
            property => property.CurrentValue);

        var changedColumnNames = changed.Select(property => property.Metadata.Name).ToArray();

        return (MaskJson(oldDict), MaskJson(newDict), MaskJson(changedColumnNames));
    }

    private string? MaskJson(object value)
    {
        var json = JsonSerializer.Serialize(value);
        var masked = _sensitiveDataMasker.MaskJson(json);

        return masked.Length <= AuditLogValueLimits.MaxJsonLength
            ? masked
            : masked[..AuditLogValueLimits.MaxJsonLength];
    }

    private static string? ResolveEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return null;
        }

        var values = key.Properties
            .Select(property => entry.Property(property.Name).CurrentValue ?? entry.Property(property.Name).OriginalValue)
            .Where(value => value is not null)
            .Select(value => value!.ToString());

        return string.Join(",", values);
    }

    private static string ResolveModuleName(EntityEntry entry)
    {
        var namespaceName = entry.Entity.GetType().Namespace ?? string.Empty;

        if (namespaceName.Contains("Identity", StringComparison.OrdinalIgnoreCase))
        {
            return AuditLogConstants.Modules.Identity;
        }

        if (namespaceName.Contains("Users", StringComparison.OrdinalIgnoreCase))
        {
            return AuditLogConstants.Modules.Users;
        }

        if (namespaceName.Contains("Categories", StringComparison.OrdinalIgnoreCase))
        {
            return AuditLogConstants.Modules.Categories;
        }

        if (namespaceName.Contains("Files", StringComparison.OrdinalIgnoreCase))
        {
            return AuditLogConstants.Modules.Files;
        }

        if (namespaceName.Contains("Notifications", StringComparison.OrdinalIgnoreCase))
        {
            return AuditLogConstants.Modules.Notifications;
        }

        if (namespaceName.Contains("BackgroundJobs", StringComparison.OrdinalIgnoreCase))
        {
            return AuditLogConstants.Modules.BackgroundJobs;
        }

        if (namespaceName.Contains("Settings", StringComparison.OrdinalIgnoreCase))
        {
            return AuditLogConstants.Modules.Settings;
        }

        if (namespaceName.Contains("Organizations", StringComparison.OrdinalIgnoreCase))
        {
            return AuditLogConstants.Modules.Organizations;
        }

        return namespaceName.Split('.').ElementAtOrDefault(1) ?? "System";
    }
}
