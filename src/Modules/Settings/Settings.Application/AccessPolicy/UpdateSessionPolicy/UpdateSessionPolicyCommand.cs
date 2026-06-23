using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;

namespace Settings.Application.AccessPolicy.UpdateSessionPolicy;

public sealed record UpdateSessionPolicyCommand(
    int AccessTokenExpirationMinutes,
    int RefreshTokenExpirationDays,
    int? SessionTimeoutMinutes,
    bool RefreshTokenReuseDetectionEnabled) : ICommand;

public sealed class UpdateSessionPolicyCommandValidator : AbstractValidator<UpdateSessionPolicyCommand>
{
    public UpdateSessionPolicyCommandValidator()
    {
        RuleFor(command => command.AccessTokenExpirationMinutes).InclusiveBetween(1, 1440);
        RuleFor(command => command.RefreshTokenExpirationDays).InclusiveBetween(1, 365);
        RuleFor(command => command.SessionTimeoutMinutes)
            .InclusiveBetween(5, 1440)
            .When(command => command.SessionTimeoutMinutes.HasValue);
    }
}

public sealed class UpdateSessionPolicyCommandHandler : IRequestHandler<UpdateSessionPolicyCommand, Result>
{
    private readonly IAccessPolicyService _accessPolicyService;

    public UpdateSessionPolicyCommandHandler(IAccessPolicyService accessPolicyService)
    {
        _accessPolicyService = accessPolicyService;
    }

    public async Task<Result> Handle(UpdateSessionPolicyCommand request, CancellationToken cancellationToken)
    {
        await _accessPolicyService.UpdateSessionPolicyAsync(
            new UpdateSessionPolicyRequest
            {
                AccessTokenExpirationMinutes = request.AccessTokenExpirationMinutes,
                RefreshTokenExpirationDays = request.RefreshTokenExpirationDays,
                SessionTimeoutMinutes = request.SessionTimeoutMinutes,
                RefreshTokenReuseDetectionEnabled = request.RefreshTokenReuseDetectionEnabled
            },
            cancellationToken);

        return Result.Success();
    }
}
