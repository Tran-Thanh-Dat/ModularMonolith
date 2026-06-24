using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Domain.Errors;
using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace AuthorizationPolicies.Domain.UserPermissionPolicyOverrides;

public sealed class UserPermissionPolicyOverride : SoftDeletableEntity
{
    private UserPermissionPolicyOverride()
    {
    }

    private UserPermissionPolicyOverride(
        Guid id,
        Guid userId,
        Guid permissionPolicyId,
        AuthorizationEffect effect,
        DateTimeOffset? expiresAt,
        string? reason,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        UserId = userId;
        PermissionPolicyId = permissionPolicyId;
        Effect = effect;
        ExpiresAt = expiresAt;
        Reason = reason;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public Guid UserId { get; private set; }

    public Guid PermissionPolicyId { get; private set; }

    public AuthorizationEffect Effect { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public string? Reason { get; private set; }

    public static UserPermissionPolicyOverride Create(
        Guid userId,
        Guid permissionPolicyId,
        AuthorizationEffect effect,
        DateTimeOffset? expiresAt,
        string? reason,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.", "UserPermissionPolicyOverride.InvalidUserId");
        }

        if (permissionPolicyId == Guid.Empty)
        {
            throw new DomainException("Permission policy id is required.", "UserPermissionPolicyOverride.InvalidPolicyId");
        }

        return new UserPermissionPolicyOverride(
            Guid.NewGuid(),
            userId,
            permissionPolicyId,
            effect,
            expiresAt,
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            createdAt,
            createdBy);
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Override is already active.", UserPermissionPolicyOverrideErrors.AlreadyActive);
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Override is already inactive.", UserPermissionPolicyOverrideErrors.AlreadyInactive);
        }

        IsActive = false;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Override is already removed.", "UserPermissionPolicyOverride.AlreadyRemoved");
        }

        MarkDeleted(deletedBy, deletedAt);
    }

    public bool IsExpired(DateTimeOffset utcNow) =>
        ExpiresAt.HasValue && ExpiresAt.Value <= utcNow;
}
