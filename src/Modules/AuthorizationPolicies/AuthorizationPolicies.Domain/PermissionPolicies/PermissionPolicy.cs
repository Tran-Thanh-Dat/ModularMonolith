using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Domain.Errors;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace AuthorizationPolicies.Domain.PermissionPolicies;

public sealed class PermissionPolicy : SoftDeletableEntity
{
    private PermissionPolicy()
    {
    }

    private PermissionPolicy(
        Guid id,
        string code,
        string name,
        string? description,
        string permissionCode,
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        AuthorizationEffect effect,
        int priority,
        string? conditions,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        PermissionCode = permissionCode;
        ModuleCode = moduleCode;
        ResourceType = resourceType;
        Action = action;
        Scope = scope;
        Effect = effect;
        Priority = priority;
        IsActive = true;
        Conditions = conditions;
        SetCreated(createdBy, createdAt);
    }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public string PermissionCode { get; private set; } = default!;

    public string ModuleCode { get; private set; } = default!;

    public string ResourceType { get; private set; } = default!;

    public string Action { get; private set; } = default!;

    public AuthorizationScope Scope { get; private set; }

    public AuthorizationEffect Effect { get; private set; }

    public int Priority { get; private set; }

    public bool IsActive { get; private set; }

    public string? Conditions { get; private set; }

    public static PermissionPolicy Create(
        string code,
        string name,
        string? description,
        string permissionCode,
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        AuthorizationEffect effect,
        int priority,
        string? conditions,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        ValidateRequired(code, name, permissionCode, moduleCode, resourceType, action);

        return new PermissionPolicy(
            Guid.NewGuid(),
            NormalizeCode(code),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            permissionCode.Trim(),
            moduleCode.Trim(),
            resourceType.Trim(),
            action.Trim(),
            scope,
            effect,
            priority,
            conditions,
            createdAt,
            createdBy);
    }

    public void Update(
        string name,
        string? description,
        string permissionCode,
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        AuthorizationEffect effect,
        int priority,
        string? conditions,
        Guid? updatedBy,
        DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Policy name is required.", "PermissionPolicy.InvalidName");
        }

        ValidateRequired(Code, name, permissionCode, moduleCode, resourceType, action);

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        PermissionCode = permissionCode.Trim();
        ModuleCode = moduleCode.Trim();
        ResourceType = resourceType.Trim();
        Action = action.Trim();
        Scope = scope;
        Effect = effect;
        Priority = priority;
        Conditions = conditions;
        SetUpdated(updatedBy, updatedAt);
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Policy is already active.", PermissionPolicyErrors.AlreadyActive);
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Policy is already inactive.", PermissionPolicyErrors.AlreadyInactive);
        }

        IsActive = false;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Policy is already deleted.", PermissionPolicyErrors.AlreadyDeleted);
        }

        MarkDeleted(deletedBy, deletedAt);
    }

    private static void ValidateRequired(
        string code,
        string name,
        string permissionCode,
        string moduleCode,
        string resourceType,
        string action)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Policy code is required.", "PermissionPolicy.InvalidCode");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Policy name is required.", "PermissionPolicy.InvalidName");
        }

        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new DomainException("Permission code is required.", "PermissionPolicy.InvalidPermissionCode");
        }

        if (string.IsNullOrWhiteSpace(moduleCode))
        {
            throw new DomainException("Module code is required.", "PermissionPolicy.InvalidModuleCode");
        }

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new DomainException("Resource type is required.", "PermissionPolicy.InvalidResourceType");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainException("Action is required.", "PermissionPolicy.InvalidAction");
        }
    }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
