namespace Settings.Application.Settings;

public sealed class SettingListItemResponse
{
    public Guid Id { get; init; }

    public string Key { get; init; } = default!;

    public string Group { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string DataType { get; init; } = default!;

    public bool IsSensitive { get; init; }

    public bool IsEncrypted { get; init; }

    public bool IsSystem { get; init; }

    public bool IsActive { get; init; }

    public bool IsEditable { get; init; }

    public int SortOrder { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class SettingDetailResponse
{
    public Guid Id { get; init; }

    public string Key { get; init; } = default!;

    public string Group { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public string? Value { get; init; }

    public string? DefaultValue { get; init; }

    public string DataType { get; init; } = default!;

    public bool IsEncrypted { get; init; }

    public bool IsSensitive { get; init; }

    public bool IsSystem { get; init; }

    public bool IsActive { get; init; }

    public bool IsEditable { get; init; }

    public int SortOrder { get; init; }

    public bool IsValueMasked { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class SettingValueResponse
{
    public string Key { get; init; } = default!;

    public string? Value { get; init; }

    public bool IsValueMasked { get; init; }
}

public sealed class SettingGroupResponse
{
    public string Group { get; init; } = default!;

    public IReadOnlyCollection<SettingListItemResponse> Settings { get; init; } = [];
}

public sealed class CreateSettingResponse
{
    public Guid Id { get; init; }
}

public sealed class CreateSettingRequest
{
    public string Key { get; init; } = default!;

    public string Group { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public string? Value { get; init; }

    public string? DefaultValue { get; init; }

    public string DataType { get; init; } = default!;

    public bool IsEncrypted { get; init; }

    public bool IsSensitive { get; init; }

    public bool IsSystem { get; init; }

    public bool IsEditable { get; init; } = true;

    public int SortOrder { get; init; }
}

public sealed class UpdateSettingRequest
{
    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public int SortOrder { get; init; }

    public bool IsEditable { get; init; } = true;
}

public sealed class UpdateSettingValueRequest
{
    public string? Value { get; init; }
}
