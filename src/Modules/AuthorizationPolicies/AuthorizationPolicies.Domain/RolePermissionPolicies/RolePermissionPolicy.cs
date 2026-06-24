using AuthorizationPolicies.Domain.Errors;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace AuthorizationPolicies.Domain.RolePermissionPolicies;

public sealed class RolePermissionPolicy : SoftDeletableEntity
{
    private RolePermissionPolicy()
    {
    }

    private RolePermissionPolicy(
        Guid id,
        Guid roleId,
        Guid permissionPolicyId,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        RoleId = roleId;
        PermissionPolicyId = permissionPolicyId;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionPolicyId { get; private set; }

    public bool IsActive { get; private set; }

    public static RolePermissionPolicy Create(
        Guid roleId,
        Guid permissionPolicyId,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (roleId == Guid.Empty)
        {
            throw new DomainException("Role id is required.", "RolePermissionPolicy.InvalidRoleId");
        }

        if (permissionPolicyId == Guid.Empty)
        {
            throw new DomainException("Permission policy id is required.", "RolePermissionPolicy.InvalidPolicyId");
        }

        return new RolePermissionPolicy(Guid.NewGuid(), roleId, permissionPolicyId, createdAt, createdBy);
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Assignment is already active.", RolePermissionPolicyErrors.AlreadyActive);
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Assignment is already inactive.", RolePermissionPolicyErrors.AlreadyInactive);
        }

        IsActive = false;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Assignment is already removed.", "RolePermissionPolicy.AlreadyRemoved");
        }

        MarkDeleted(deletedBy, deletedAt);
    }
}
