using AuditLogs.Application.Abstractions;
using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.Constants;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.AuthorizationMatrix;
using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Domain.Errors;
using AuthorizationPolicies.Infrastructure.Persistence;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthorizationPolicies.Infrastructure.Services;

public sealed class AuthorizationMatrixEntryService : IAuthorizationMatrixEntryService
{
    private readonly AuthorizationPoliciesUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<AuthorizationMatrixEntryService> _logger;

    public AuthorizationMatrixEntryService(
        AuthorizationPoliciesUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<AuthorizationMatrixEntryService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        string requiredPermissionCode,
        string? description,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        if (await EntryExistsAsync(moduleCode, resourceType, action, scope, null, cancellationToken))
        {
            throw new ConflictException(AuthorizationMatrixErrors.AlreadyExists, "Authorization matrix entry already exists.");
        }

        var entry = AuthorizationMatrixEntry.Create(
            moduleCode, resourceType, action, scope, requiredPermissionCode, description, metadata,
            _dateTimeProvider.UtcNow, _currentUserService.UserId);

        await _unitOfWork.Repository<AuthorizationMatrixEntry, Guid>().AddAsync(entry, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.AuthorizationMatrixEntryCreated,
            $"Created authorization matrix entry: {entry.ModuleCode}/{entry.ResourceType}/{entry.Action}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created matrix entry {EntryId}", entry.Id);
        return entry.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        string requiredPermissionCode,
        string? description,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetEntityAsync(id, cancellationToken);

        if (await EntryExistsAsync(moduleCode, resourceType, action, scope, id, cancellationToken))
        {
            throw new ConflictException(AuthorizationMatrixErrors.AlreadyExists, "Authorization matrix entry already exists.");
        }

        entry.Update(moduleCode, resourceType, action, scope, requiredPermissionCode, description, metadata,
            _currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.AuthorizationMatrixEntryUpdated,
            $"Updated authorization matrix entry: {entry.ModuleCode}/{entry.ResourceType}/{entry.Action}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntityAsync(id, cancellationToken);
        entry.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.AuthorizationMatrixEntryDeleted,
            $"Deleted authorization matrix entry: {entry.ModuleCode}/{entry.ResourceType}/{entry.Action}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task EnableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntityAsync(id, cancellationToken);
        entry.Enable();

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.AuthorizationMatrixEntryEnabled,
            $"Enabled authorization matrix entry: {entry.ModuleCode}/{entry.ResourceType}/{entry.Action}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task DisableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await GetEntityAsync(id, cancellationToken);
        entry.Disable();

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.AuthorizationMatrixEntryDisabled,
            $"Disabled authorization matrix entry: {entry.ModuleCode}/{entry.ResourceType}/{entry.Action}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task<AuthorizationMatrixEntryDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _unitOfWork.Repository<AuthorizationMatrixEntry, Guid>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        return entry is null ? null : MapDetail(entry);
    }

    public async Task<PagedResult<AuthorizationMatrixEntryListItemResponse>> GetListAsync(
        string? moduleCode,
        string? resourceType,
        string? action,
        AuthorizationScope? scope,
        string? requiredPermissionCode,
        bool? isEnabled,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<AuthorizationMatrixEntry, Guid>()
            .Query()
            .AsNoTracking()
            .Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(moduleCode))
        {
            query = query.Where(e => e.ModuleCode == moduleCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            query = query.Where(e => e.ResourceType == resourceType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(e => e.Action == action.Trim());
        }

        if (scope.HasValue)
        {
            query = query.Where(e => e.Scope == scope.Value);
        }

        if (!string.IsNullOrWhiteSpace(requiredPermissionCode))
        {
            query = query.Where(e => e.RequiredPermissionCode == requiredPermissionCode.Trim());
        }

        if (isEnabled.HasValue)
        {
            query = query.Where(e => e.IsEnabled == isEnabled.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.ModuleCode)
            .ThenBy(e => e.ResourceType)
            .ThenBy(e => e.Action)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapListItem(e))
            .ToListAsync(cancellationToken);

        return PagedResult<AuthorizationMatrixEntryListItemResponse>.Create(items, pageIndex, pageSize, total);
    }

    private async Task<AuthorizationMatrixEntry> GetEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _unitOfWork.Repository<AuthorizationMatrixEntry, Guid>()
            .Query()
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (entry is null)
        {
            throw new NotFoundException(AuthorizationMatrixErrors.NotFound, "Authorization matrix entry not found.");
        }

        return entry;
    }

    private async Task<bool> EntryExistsAsync(
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<AuthorizationMatrixEntry, Guid>()
            .Query()
            .AnyAsync(e => !e.IsDeleted &&
                           e.ModuleCode == moduleCode.Trim() &&
                           e.ResourceType == resourceType.Trim() &&
                           e.Action == action.Trim() &&
                           e.Scope == scope &&
                           (!excludeId.HasValue || e.Id != excludeId.Value),
                cancellationToken);

    private static AuthorizationMatrixEntryListItemResponse MapListItem(AuthorizationMatrixEntry e) => new()
    {
        Id = e.Id,
        ModuleCode = e.ModuleCode,
        ResourceType = e.ResourceType,
        Action = e.Action,
        Scope = e.Scope,
        RequiredPermissionCode = e.RequiredPermissionCode,
        IsEnabled = e.IsEnabled,
        CreatedAt = e.CreatedAt
    };

    private static AuthorizationMatrixEntryDetailResponse MapDetail(AuthorizationMatrixEntry e) => new()
    {
        Id = e.Id,
        ModuleCode = e.ModuleCode,
        ResourceType = e.ResourceType,
        Action = e.Action,
        Scope = e.Scope,
        RequiredPermissionCode = e.RequiredPermissionCode,
        Description = e.Description,
        Metadata = e.Metadata,
        IsEnabled = e.IsEnabled,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
