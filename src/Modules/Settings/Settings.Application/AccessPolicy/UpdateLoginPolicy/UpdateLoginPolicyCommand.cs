using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;

namespace Settings.Application.AccessPolicy.UpdateLoginPolicy;

public sealed record UpdateLoginPolicyCommand(
    int MaxFailedLoginAttempts,
    int LockoutDurationMinutes,
    bool EnableLockout,
    bool RequireConfirmedEmail) : ICommand;

public sealed class UpdateLoginPolicyCommandValidator : AbstractValidator<UpdateLoginPolicyCommand>
{
    public UpdateLoginPolicyCommandValidator()
    {
        RuleFor(command => command.MaxFailedLoginAttempts).InclusiveBetween(1, 20);
        RuleFor(command => command.LockoutDurationMinutes).InclusiveBetween(1, 1440);
    }
}

public sealed class UpdateLoginPolicyCommandHandler : IRequestHandler<UpdateLoginPolicyCommand, Result>
{
    private readonly IAccessPolicyService _accessPolicyService;

    public UpdateLoginPolicyCommandHandler(IAccessPolicyService accessPolicyService)
    {
        _accessPolicyService = accessPolicyService;
    }

    public async Task<Result> Handle(UpdateLoginPolicyCommand request, CancellationToken cancellationToken)
    {
        await _accessPolicyService.UpdateLoginPolicyAsync(
            new UpdateLoginPolicyRequest
            {
                MaxFailedLoginAttempts = request.MaxFailedLoginAttempts,
                LockoutDurationMinutes = request.LockoutDurationMinutes,
                EnableLockout = request.EnableLockout,
                RequireConfirmedEmail = request.RequireConfirmedEmail
            },
            cancellationToken);

        return Result.Success();
    }
}
