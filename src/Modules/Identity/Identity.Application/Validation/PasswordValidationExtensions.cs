using FluentValidation;
using Settings.Application.Abstractions;

namespace Identity.Application.Validation;

public static class PasswordValidationExtensions
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordRules<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

    public static IRuleBuilderOptionsConditions<T, string> ApplyPasswordPolicy<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IPasswordPolicyValidator passwordPolicyValidator) =>
        ruleBuilder
            .NotEmpty()
            .CustomAsync(async (password, context, cancellationToken) =>
            {
                var errors = await passwordPolicyValidator.ValidateAsync(password, cancellationToken);
                foreach (var error in errors)
                {
                    context.AddFailure(error);
                }
            });
}
