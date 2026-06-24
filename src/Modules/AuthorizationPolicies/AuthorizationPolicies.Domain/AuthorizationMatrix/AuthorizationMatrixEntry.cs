using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Domain.Errors;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace AuthorizationPolicies.Domain.AuthorizationMatrix;

public sealed class AuthorizationMatrixEntry : SoftDeletableEntity
{
    private AuthorizationMatrixEntry()
    {
    }

    private AuthorizationMatrixEntry(
        Guid id,
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        string requiredPermissionCode,
        string? description,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        ModuleCode = moduleCode;
        ResourceType = resourceType;
        Action = action;
        Scope = scope;
        RequiredPermissionCode = requiredPermissionCode;
        Description = description;
        Metadata = metadata;
        IsEnabled = true;
        SetCreated(createdBy, createdAt);
    }

    public string ModuleCode { get; private set; } = default!;

    public string ResourceType { get; private set; } = default!;

    public string Action { get; private set; } = default!;

    public AuthorizationScope Scope { get; private set; }

    public string RequiredPermissionCode { get; private set; } = default!;

    public string? Description { get; private set; }

    public bool IsEnabled { get; private set; }

    public string? Metadata { get; private set; }

    public static AuthorizationMatrixEntry Create(
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        string requiredPermissionCode,
        string? description,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        ValidateRequired(moduleCode, resourceType, action, requiredPermissionCode);

        return new AuthorizationMatrixEntry(
            Guid.NewGuid(),
            moduleCode.Trim(),
            resourceType.Trim(),
            action.Trim(),
            scope,
            requiredPermissionCode.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            metadata,
            createdAt,
            createdBy);
    }

    public void Update(
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        string requiredPermissionCode,
        string? description,
        string? metadata,
        Guid? updatedBy,
        DateTimeOffset updatedAt)
    {
        ValidateRequired(moduleCode, resourceType, action, requiredPermissionCode);

        ModuleCode = moduleCode.Trim();
        ResourceType = resourceType.Trim();
        Action = action.Trim();
        Scope = scope;
        RequiredPermissionCode = requiredPermissionCode.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Metadata = metadata;
        SetUpdated(updatedBy, updatedAt);
    }

    public void Enable()
    {
        if (IsEnabled)
        {
            throw new DomainException("Matrix entry is already enabled.", AuthorizationMatrixErrors.AlreadyEnabled);
        }

        IsEnabled = true;
    }

    public void Disable()
    {
        if (!IsEnabled)
        {
            throw new DomainException("Matrix entry is already disabled.", AuthorizationMatrixErrors.AlreadyDisabled);
        }

        IsEnabled = false;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Matrix entry is already deleted.", AuthorizationMatrixErrors.AlreadyDeleted);
        }

        MarkDeleted(deletedBy, deletedAt);
    }

    private static void ValidateRequired(
        string moduleCode,
        string resourceType,
        string action,
        string requiredPermissionCode)
    {
        if (string.IsNullOrWhiteSpace(moduleCode))
        {
            throw new DomainException("Module code is required.", "AuthorizationMatrix.InvalidModuleCode");
        }

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new DomainException("Resource type is required.", "AuthorizationMatrix.InvalidResourceType");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainException("Action is required.", "AuthorizationMatrix.InvalidAction");
        }

        if (string.IsNullOrWhiteSpace(requiredPermissionCode))
        {
            throw new DomainException("Required permission code is required.", "AuthorizationMatrix.InvalidPermissionCode");
        }
    }
}
