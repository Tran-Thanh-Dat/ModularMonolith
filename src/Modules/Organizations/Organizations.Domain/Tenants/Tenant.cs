using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Organizations.Domain.Tenants;

public sealed class Tenant : SoftDeletableEntity
{
    private Tenant()
    {
    }

    private Tenant(
        Guid id,
        string code,
        string name,
        string? description,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        Metadata = metadata;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public string? Metadata { get; private set; }

    public static Tenant Create(
        string code,
        string name,
        string? description,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Tenant code is required.", "Tenant.InvalidCode");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Tenant name is required.", "Tenant.InvalidName");
        }

        return new Tenant(
            Guid.NewGuid(),
            NormalizeCode(code),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            metadata,
            createdAt,
            createdBy);
    }

    public void Update(string name, string? description, string? metadata)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Tenant name is required.", "Tenant.InvalidName");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Metadata = metadata;
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Tenant is already active.", "Tenant.AlreadyActive");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Tenant is already inactive.", "Tenant.AlreadyInactive");
        }

        IsActive = false;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Tenant is already deleted.", "Tenant.AlreadyDeleted");
        }

        MarkDeleted(deletedBy, deletedAt);
    }

    public static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();
}
