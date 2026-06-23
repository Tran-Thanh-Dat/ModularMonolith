using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Settings.Domain.Settings;

public sealed class SystemSetting : SoftDeletableEntity
{
    private SystemSetting()
    {
    }

    private SystemSetting(
        Guid id,
        string key,
        string group,
        string name,
        string? description,
        string? value,
        string? defaultValue,
        string dataType,
        bool isEncrypted,
        bool isSensitive,
        bool isSystem,
        bool isEditable,
        int sortOrder,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        Key = key;
        Group = group;
        Name = name;
        Description = description;
        Value = value;
        DefaultValue = defaultValue;
        DataType = dataType;
        IsEncrypted = isEncrypted;
        IsSensitive = isSensitive;
        IsSystem = isSystem;
        IsEditable = isEditable;
        SortOrder = sortOrder;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public string Key { get; private set; } = default!;

    public string Group { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public string? Value { get; private set; }

    public string? DefaultValue { get; private set; }

    public string DataType { get; private set; } = default!;

    public bool IsEncrypted { get; private set; }

    public bool IsSensitive { get; private set; }

    public bool IsSystem { get; private set; }

    public bool IsEditable { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public static SystemSetting Create(
        string key,
        string group,
        string name,
        string? description,
        string? value,
        string? defaultValue,
        string dataType,
        bool isEncrypted,
        bool isSensitive,
        bool isSystem,
        bool isEditable,
        int sortOrder,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        ValidateKey(key);
        ValidateGroup(group);
        ValidateName(name);
        ValidateDataType(dataType);

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", SettingErrors.InvalidValue);
        }

        return new SystemSetting(
            Guid.NewGuid(),
            key.Trim(),
            group.Trim(),
            name.Trim(),
            NormalizeOptional(description),
            value,
            defaultValue,
            dataType.Trim(),
            isEncrypted,
            isSensitive,
            isSystem,
            isEditable,
            sortOrder,
            createdAt,
            createdBy);
    }

    public void UpdateValue(string? value)
    {
        EnsureNotDeleted();
        EnsureEditable();

        Value = value;
    }

    public void UpdateMetadata(
        string name,
        string? description,
        int sortOrder,
        bool isEditable)
    {
        EnsureNotDeleted();
        ValidateName(name);

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", SettingErrors.InvalidValue);
        }

        Name = name.Trim();
        Description = NormalizeOptional(description);
        SortOrder = sortOrder;
        IsEditable = isEditable;
    }

    public void Activate()
    {
        EnsureNotDeleted();

        if (IsActive)
        {
            throw new DomainException("Setting is already active.", SettingErrors.InvalidValue);
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        EnsureNotDeleted();

        if (!IsActive)
        {
            throw new DomainException("Setting is already inactive.", SettingErrors.InvalidValue);
        }

        IsActive = false;
    }

    public void MarkSensitive() => IsSensitive = true;

    public void MarkEncrypted() => IsEncrypted = true;

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Setting is already deleted.", SettingErrors.NotFound);
        }

        if (IsSystem)
        {
            throw new DomainException("System settings cannot be deleted.", SettingErrors.SystemSettingCannotBeDeleted);
        }

        MarkDeleted(deletedBy, deletedAt);
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException("Setting is deleted.", SettingErrors.NotFound);
        }
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new DomainException("Setting is not editable.", SettingErrors.NotEditable);
        }
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Setting key is required.", SettingErrors.InvalidKey);
        }
    }

    private static void ValidateGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            throw new DomainException("Setting group is required.", SettingErrors.InvalidGroup);
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Setting name is required.", SettingErrors.InvalidValue);
        }
    }

    private static void ValidateDataType(string dataType)
    {
        if (string.IsNullOrWhiteSpace(dataType))
        {
            throw new DomainException("Setting data type is required.", SettingErrors.InvalidDataType);
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class SettingErrors
{
    public const string InvalidKey = "Setting.InvalidKey";
    public const string InvalidGroup = "Setting.InvalidGroup";
    public const string InvalidDataType = "Setting.InvalidDataType";
    public const string InvalidValue = "Setting.InvalidValue";
    public const string NotFound = "Setting.NotFound";
    public const string NotEditable = "Setting.NotEditable";
    public const string SystemSettingCannotBeDeleted = "Setting.SystemSettingCannotBeDeleted";
}
