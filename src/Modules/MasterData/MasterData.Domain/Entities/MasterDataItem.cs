using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;
using MasterData.Domain.Errors;

namespace MasterData.Domain.Entities;

public sealed class MasterDataItem : SoftDeletableEntity
{
    private MasterDataItem()
    {
    }

    private MasterDataItem(
        Guid id,
        Guid groupId,
        string code,
        string name,
        string? value,
        string? description,
        Guid? parentItemId,
        bool isSystem,
        bool isDefault,
        int sortOrder,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        GroupId = groupId;
        Code = code;
        Name = name;
        Value = value;
        Description = description;
        ParentItemId = parentItemId;
        IsSystem = isSystem;
        IsDefault = isDefault;
        SortOrder = sortOrder;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Metadata = metadata;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public Guid GroupId { get; private set; }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Value { get; private set; }

    public string? Description { get; private set; }

    public Guid? ParentItemId { get; private set; }

    public bool IsSystem { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset? EffectiveFrom { get; private set; }

    public DateTimeOffset? EffectiveTo { get; private set; }

    public string? Metadata { get; private set; }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public static MasterDataItem Create(
        Guid groupId,
        string code,
        string name,
        string? value,
        string? description,
        Guid? parentItemId,
        bool isSystem,
        bool isDefault,
        int sortOrder,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        ValidateEffectiveRange(effectiveFrom, effectiveTo);

        if (groupId == Guid.Empty)
        {
            throw new DomainException("GroupId is required.", MasterDataItemErrors.InvalidParent);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Item code is required.", MasterDataItemErrors.InvalidParent);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Item name is required.", MasterDataItemErrors.InvalidParent);
        }

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", MasterDataItemErrors.InvalidParent);
        }

        return new MasterDataItem(
            Guid.NewGuid(),
            groupId,
            NormalizeCode(code),
            name.Trim(),
            string.IsNullOrWhiteSpace(value) ? null : value.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            parentItemId,
            isSystem,
            isDefault,
            sortOrder,
            effectiveFrom,
            effectiveTo,
            metadata,
            createdAt,
            createdBy);
    }

    public void Update(
        string name,
        string? value,
        string? description,
        Guid? parentItemId,
        int sortOrder,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        string? metadata,
        Guid itemId)
    {
        if (parentItemId == itemId)
        {
            throw new DomainException("Item cannot be its own parent.", MasterDataItemErrors.ParentSelfReference);
        }

        ValidateEffectiveRange(effectiveFrom, effectiveTo);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Item name is required.", MasterDataItemErrors.InvalidParent);
        }

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", MasterDataItemErrors.InvalidParent);
        }

        Name = name.Trim();
        Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ParentItemId = parentItemId;
        SortOrder = sortOrder;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Metadata = metadata;
    }

    public void ChangeCode(string code)
    {
        if (IsSystem)
        {
            throw new DomainException("System item code cannot be changed.", MasterDataItemErrors.CodeChangeNotAllowed);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Item code is required.", MasterDataItemErrors.InvalidParent);
        }

        Code = NormalizeCode(code);
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Item is already active.", MasterDataItemErrors.AlreadyActive);
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Item is already inactive.", MasterDataItemErrors.AlreadyInactive);
        }

        if (IsDefault)
        {
            IsDefault = false;
        }

        IsActive = false;
    }

    public void SetDefault(bool isDefault)
    {
        if (isDefault && !IsActive)
        {
            throw new DomainException("Inactive item cannot be default.", MasterDataItemErrors.InactiveCannotBeDefault);
        }

        IsDefault = isDefault;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsSystem)
        {
            throw new DomainException("System item cannot be deleted.", MasterDataItemErrors.SystemProtected);
        }

        if (IsDeleted)
        {
            throw new DomainException("Item is already deleted.", MasterDataItemErrors.AlreadyDeleted);
        }

        IsDefault = false;
        MarkDeleted(deletedBy, deletedAt);
    }

    public static void ValidateEffectiveRange(DateTimeOffset? effectiveFrom, DateTimeOffset? effectiveTo)
    {
        if (effectiveFrom.HasValue && effectiveTo.HasValue && effectiveFrom > effectiveTo)
        {
            throw new DomainException("EffectiveFrom must be less than or equal to EffectiveTo.", MasterDataItemErrors.InvalidEffectiveRange);
        }
    }
}
