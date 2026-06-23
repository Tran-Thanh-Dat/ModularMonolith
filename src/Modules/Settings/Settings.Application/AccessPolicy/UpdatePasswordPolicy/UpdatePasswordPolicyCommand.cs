using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;

namespace Settings.Application.AccessPolicy.UpdatePasswordPolicy;

public sealed record UpdatePasswordPolicyCommand(
    int MinimumLength,
    bool RequireUppercase,
    bool RequireLowercase,
    bool RequireDigit,
    bool RequireSpecialCharacter,
    int? PasswordExpirationDays,
    int PreventPasswordReuseCount) : ICommand;

public sealed class UpdatePasswordPolicyCommandValidator : AbstractValidator<UpdatePasswordPolicyCommand>
{
    public UpdatePasswordPolicyCommandValidator()
    {
        RuleFor(command => command.MinimumLength).InclusiveBetween(8, 128);
        RuleFor(command => command.PasswordExpirationDays)
            .InclusiveBetween(1, 365)
            .When(command => command.PasswordExpirationDays.HasValue);
        RuleFor(command => command.PreventPasswordReuseCount).InclusiveBetween(0, 24);
        RuleFor(command => command)
            .Must(command =>
                command.MinimumLength >= 12 ||
                command.RequireUppercase ||
                command.RequireLowercase ||
                command.RequireDigit ||
                command.RequireSpecialCharacter)
            .WithMessage("At least one complexity rule must be enabled when minimum length is below 12.");
    }
}

public sealed class UpdatePasswordPolicyCommandHandler : IRequestHandler<UpdatePasswordPolicyCommand, Result>
{
    private readonly IAccessPolicyService _accessPolicyService;

    public UpdatePasswordPolicyCommandHandler(IAccessPolicyService accessPolicyService)
    {
        _accessPolicyService = accessPolicyService;
    }

    public async Task<Result> Handle(UpdatePasswordPolicyCommand request, CancellationToken cancellationToken)
    {
        var updateRequest = new UpdatePasswordPolicyRequest
        {
            MinimumLength = request.MinimumLength,
            RequireUppercase = request.RequireUppercase,
            RequireLowercase = request.RequireLowercase,
            RequireDigit = request.RequireDigit,
            RequireSpecialCharacter = request.RequireSpecialCharacter,
            PasswordExpirationDays = request.PasswordExpirationDays,
            PreventPasswordReuseCount = request.PreventPasswordReuseCount
        };

        await _accessPolicyService.UpdatePasswordPolicyAsync(updateRequest, cancellationToken);
        return Result.Success();
    }
}
