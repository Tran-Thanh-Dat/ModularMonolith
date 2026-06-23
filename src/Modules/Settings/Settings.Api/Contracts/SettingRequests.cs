namespace Settings.Api.Contracts;

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

public sealed class UpdatePasswordPolicyRequest
{
    public int MinimumLength { get; init; }

    public bool RequireUppercase { get; init; }

    public bool RequireLowercase { get; init; }

    public bool RequireDigit { get; init; }

    public bool RequireSpecialCharacter { get; init; }

    public int? PasswordExpirationDays { get; init; }

    public int PreventPasswordReuseCount { get; init; }
}

public sealed class UpdateLoginPolicyRequest
{
    public int MaxFailedLoginAttempts { get; init; }

    public int LockoutDurationMinutes { get; init; }

    public bool EnableLockout { get; init; }

    public bool RequireConfirmedEmail { get; init; }
}

public sealed class UpdateSessionPolicyRequest
{
    public int AccessTokenExpirationMinutes { get; init; }

    public int RefreshTokenExpirationDays { get; init; }

    public int? SessionTimeoutMinutes { get; init; }

    public bool RefreshTokenReuseDetectionEnabled { get; init; }
}

public sealed class UpdateMaintenancePolicyRequest
{
    public bool Enabled { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset? StartAt { get; init; }

    public DateTimeOffset? EndAt { get; init; }

    public bool AllowAdminBypass { get; init; } = true;
}
