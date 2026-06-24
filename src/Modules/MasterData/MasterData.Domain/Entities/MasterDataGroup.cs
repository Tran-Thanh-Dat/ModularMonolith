using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;
using MasterData.Domain.Enums;
using MasterData.Domain.Errors;

namespace MasterData.Domain.Entities;

public sealed class MasterDataGroup : SoftDeletableEntity
{
    private MasterDataGroup()
    {
    }

    private MasterDataGroup(
        Guid id,
        string code,
        string name,
        string? description,
        MasterDataScope scope,
        Guid? tenantId,
        Guid? organizationId,
        bool isSystem,
        int sortOrder,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        Scope = scope;
        TenantId = tenantId;
        OrganizationId = organizationId;
        IsSystem = isSystem;
        SortOrder = sortOrder;
        Metadata = metadata;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public MasterDataScope Scope { get; private set; }

    public Guid? TenantId { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public string? Metadata { get; private set; }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public static MasterDataGroup Create(
        string code,
        string name,
        string? description,
        MasterDataScope scope,
        Guid? tenantId,
        Guid? organizationId,
        bool isSystem,
        int sortOrder,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        ValidateScope(scope, tenantId, organizationId);

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Group code is required.", MasterDataGroupErrors.InvalidScope);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Group name is required.", MasterDataGroupErrors.InvalidScope);
        }

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", MasterDataGroupErrors.InvalidScope);
        }

        return new MasterDataGroup(
            Guid.NewGuid(),
            NormalizeCode(code),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            scope,
            tenantId,
            organizationId,
            isSystem,
            sortOrder,
            metadata,
            createdAt,
            createdBy);
    }

    public void Update(string name, string? description, int sortOrder, string? metadata)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Group name is required.", MasterDataGroupErrors.InvalidScope);
        }

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", MasterDataGroupErrors.InvalidScope);
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SortOrder = sortOrder;
        Metadata = metadata;
    }

    public void ChangeCode(string code)
    {
        if (IsSystem)
        {
            throw new DomainException("System group code cannot be changed.", MasterDataGroupErrors.CodeChangeNotAllowed);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Group code is required.", MasterDataGroupErrors.InvalidScope);
        }

        Code = NormalizeCode(code);
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Group is already active.", MasterDataGroupErrors.AlreadyActive);
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Group is already inactive.", MasterDataGroupErrors.AlreadyInactive);
        }

        IsActive = false;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsSystem)
        {
            throw new DomainException("System group cannot be deleted.", MasterDataGroupErrors.SystemProtected);
        }

        if (IsDeleted)
        {
            throw new DomainException("Group is already deleted.", MasterDataGroupErrors.AlreadyDeleted);
        }

        MarkDeleted(deletedBy, deletedAt);
    }

    public static void ValidateScope(MasterDataScope scope, Guid? tenantId, Guid? organizationId)
    {
        switch (scope)
        {
            case MasterDataScope.Global:
                if (tenantId.HasValue || organizationId.HasValue)
                {
                    throw new DomainException("Global scope requires null tenant and organization.", MasterDataGroupErrors.InvalidScope);
                }

                break;

            case MasterDataScope.Tenant:
                if (!tenantId.HasValue || organizationId.HasValue)
                {
                    throw new DomainException("Tenant scope requires tenantId and null organizationId.", MasterDataGroupErrors.InvalidScope);
                }

                break;

            case MasterDataScope.Organization:
                if (!tenantId.HasValue || !organizationId.HasValue)
                {
                    throw new DomainException("Organization scope requires tenantId and organizationId.", MasterDataGroupErrors.InvalidScope);
                }

                break;

            default:
                throw new DomainException("Invalid master data scope.", MasterDataGroupErrors.InvalidScope);
        }
    }
}
