using System.Text.RegularExpressions;
using Settings.Application.Abstractions;

namespace Settings.Infrastructure.Services;

public sealed class PasswordPolicyValidator : IPasswordPolicyValidator
{
    private readonly IAccessPolicyService _accessPolicyService;

    public PasswordPolicyValidator(IAccessPolicyService accessPolicyService)
    {
        _accessPolicyService = accessPolicyService;
    }

    public async Task<IReadOnlyList<string>> ValidateAsync(
        string password,
        CancellationToken cancellationToken = default)
    {
        var policy = await _accessPolicyService.GetPasswordPolicyAsync(cancellationToken);
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required.");
            return errors;
        }

        if (password.Length < policy.MinimumLength)
        {
            errors.Add($"Password must be at least {policy.MinimumLength} characters.");
        }

        if (password.Length > 128)
        {
            errors.Add("Password cannot exceed 128 characters.");
        }

        if (policy.RequireUppercase && !Regex.IsMatch(password, "[A-Z]"))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (policy.RequireLowercase && !Regex.IsMatch(password, "[a-z]"))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (policy.RequireDigit && !Regex.IsMatch(password, "[0-9]"))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (policy.RequireSpecialCharacter && !Regex.IsMatch(password, "[^a-zA-Z0-9]"))
        {
            errors.Add("Password must contain at least one special character.");
        }

        if (policy.MinimumLength < 12 &&
            !policy.RequireUppercase &&
            !policy.RequireLowercase &&
            !policy.RequireDigit &&
            !policy.RequireSpecialCharacter)
        {
            errors.Add("At least one password complexity rule must be enabled.");
        }

        return errors;
    }
}
