using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;
using Organizations.Domain.Enums;

namespace Organizations.Domain.Organizations;

public sealed class Organization : SoftDeletableEntity
{
    private Organization()
    {
    }

    private Organization(
        Guid id,
        Guid tenantId,
        Guid? parentOrganizationId,
        string code,
        string name,
        string? description,
        OrganizationType type,
        int sortOrder,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        TenantId = tenantId;
        ParentOrganizationId = parentOrganizationId;
        Code = code;
        Name = name;
        Description = description;
        Type = type;
        SortOrder = sortOrder;
        Metadata = metadata;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public Guid TenantId { get; private set; }

    public Guid? ParentOrganizationId { get; private set; }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public OrganizationType Type { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public string? Metadata { get; private set; }

    public static Organization Create(
        Guid tenantId,
        Guid? parentOrganizationId,
        string code,
        string name,
        string? description,
        OrganizationType type,
        int sortOrder,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.", "Organization.InvalidTenant");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Organization code is required.", "Organization.InvalidCode");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Organization name is required.", "Organization.InvalidName");
        }

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", "Organization.InvalidSortOrder");
        }

        return new Organization(
            Guid.NewGuid(),
            tenantId,
            parentOrganizationId,
            NormalizeCode(code),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            type,
            sortOrder,
            metadata,
            createdAt,
            createdBy);
    }

    public void Update(
        string name,
        string? description,
        OrganizationType type,
        int sortOrder,
        string? metadata,
        Guid? parentOrganizationId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Organization name is required.", "Organization.InvalidName");
        }

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", "Organization.InvalidSortOrder");
        }

        if (parentOrganizationId == Id)
        {
            throw new DomainException("Organization cannot be its own parent.", "Organization.ParentSelfReference");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Type = type;
        SortOrder = sortOrder;
        Metadata = metadata;
        ParentOrganizationId = parentOrganizationId;
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Organization is already active.", "Organization.AlreadyActive");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Organization is already inactive.", "Organization.AlreadyInactive");
        }

        IsActive = false;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Organization is already deleted.", "Organization.AlreadyDeleted");
        }

        MarkDeleted(deletedBy, deletedAt);
    }

    public static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();
}
