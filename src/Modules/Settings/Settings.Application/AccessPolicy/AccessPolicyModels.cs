namespace Settings.Application.AccessPolicy;

public class PasswordPolicyOptions
{
    public int MinimumLength { get; init; } = 8;

    public bool RequireUppercase { get; init; } = true;

    public bool RequireLowercase { get; init; } = true;

    public bool RequireDigit { get; init; } = true;

    public bool RequireSpecialCharacter { get; init; }

    public int? PasswordExpirationDays { get; init; }

    public int PreventPasswordReuseCount { get; init; }
}

public class LoginPolicyOptions
{
    public int MaxFailedLoginAttempts { get; init; } = 5;

    public int LockoutDurationMinutes { get; init; } = 15;

    public bool EnableLockout { get; init; } = true;

    public bool RequireConfirmedEmail { get; init; }
}

public class SessionPolicyOptions
{
    public int AccessTokenExpirationMinutes { get; init; } = 30;

    public int RefreshTokenExpirationDays { get; init; } = 7;

    public int? SessionTimeoutMinutes { get; init; }

    public bool RefreshTokenReuseDetectionEnabled { get; init; } = true;
}

public class MaintenancePolicyOptions
{
    public bool Enabled { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset? StartAt { get; init; }

    public DateTimeOffset? EndAt { get; init; }

    public bool AllowAdminBypass { get; init; } = true;
}

public sealed class PasswordPolicyResponse : PasswordPolicyOptions;

public sealed class LoginPolicyResponse : LoginPolicyOptions;

public sealed class SessionPolicyResponse : SessionPolicyOptions;

public sealed class MaintenancePolicyResponse : MaintenancePolicyOptions;

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
