using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Organizations.Domain.OrganizationUsers;

public sealed class OrganizationUser : SoftDeletableEntity
{
    private OrganizationUser()
    {
    }

    private OrganizationUser(
        Guid id,
        Guid tenantId,
        Guid organizationId,
        Guid userId,
        bool isDefault,
        DateTimeOffset joinedAt,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        TenantId = tenantId;
        OrganizationId = organizationId;
        UserId = userId;
        IsDefault = isDefault;
        IsActive = true;
        JoinedAt = joinedAt;
        SetCreated(createdBy, createdAt);
    }

    public Guid TenantId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public DateTimeOffset? LeftAt { get; private set; }

    public static OrganizationUser Create(
        Guid tenantId,
        Guid organizationId,
        Guid userId,
        bool isDefault,
        DateTimeOffset joinedAt,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.", "OrganizationUser.InvalidTenant");
        }

        if (organizationId == Guid.Empty)
        {
            throw new DomainException("Organization id is required.", "OrganizationUser.InvalidOrganization");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.", "OrganizationUser.InvalidUser");
        }

        return new OrganizationUser(
            Guid.NewGuid(),
            tenantId,
            organizationId,
            userId,
            isDefault,
            joinedAt,
            createdAt,
            createdBy);
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }

    public void Activate(DateTimeOffset joinedAt)
    {
        if (IsActive)
        {
            throw new DomainException("Organization membership is already active.", "OrganizationUser.AlreadyActive");
        }

        IsActive = true;
        JoinedAt = joinedAt;
        LeftAt = null;
    }

    public void Deactivate(DateTimeOffset leftAt)
    {
        if (!IsActive)
        {
            throw new DomainException("Organization membership is already inactive.", "OrganizationUser.AlreadyInactive");
        }

        IsActive = false;
        IsDefault = false;
        LeftAt = leftAt;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Organization membership is already removed.", "OrganizationUser.AlreadyRemoved");
        }

        MarkDeleted(deletedBy, deletedAt);
    }
}
