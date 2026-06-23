namespace Identity.Api.Contracts;

public sealed class UpdateAccountProfileRequest
{
    public string Email { get; init; } = default!;

    public string FullName { get; init; } = default!;
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = default!;

    public string NewPassword { get; init; } = default!;
}

public sealed class ForgotPasswordRequest
{
    public string Email { get; init; } = default!;
}

public sealed class ResetPasswordRequest
{
    public string Token { get; init; } = default!;

    public string NewPassword { get; init; } = default!;
}
