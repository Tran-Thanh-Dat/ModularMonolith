using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Organizations.Domain.Workspaces;

public sealed class Workspace : SoftDeletableEntity
{
    private Workspace()
    {
    }

    private Workspace(
        Guid id,
        Guid tenantId,
        Guid? organizationId,
        string code,
        string name,
        string? description,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        TenantId = tenantId;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        Description = description;
        Metadata = metadata;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public Guid TenantId { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public string? Metadata { get; private set; }

    public static Workspace Create(
        Guid tenantId,
        Guid? organizationId,
        string code,
        string name,
        string? description,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.", "Workspace.InvalidTenant");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Workspace code is required.", "Workspace.InvalidCode");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Workspace name is required.", "Workspace.InvalidName");
        }

        return new Workspace(
            Guid.NewGuid(),
            tenantId,
            organizationId,
            NormalizeCode(code),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            metadata,
            createdAt,
            createdBy);
    }

    public void Update(
        string name,
        string? description,
        Guid? organizationId,
        string? metadata)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Workspace name is required.", "Workspace.InvalidName");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        OrganizationId = organizationId;
        Metadata = metadata;
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Workspace is already active.", "Workspace.AlreadyActive");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Workspace is already inactive.", "Workspace.AlreadyInactive");
        }

        IsActive = false;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Workspace is already deleted.", "Workspace.AlreadyDeleted");
        }

        MarkDeleted(deletedBy, deletedAt);
    }

    public static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();
}
