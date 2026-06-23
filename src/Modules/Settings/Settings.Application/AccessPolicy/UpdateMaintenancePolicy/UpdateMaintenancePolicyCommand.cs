using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;

namespace Settings.Application.AccessPolicy.UpdateMaintenancePolicy;

public sealed record UpdateMaintenancePolicyCommand(
    bool Enabled,
    string? Message,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    bool AllowAdminBypass) : ICommand;

public sealed class UpdateMaintenancePolicyCommandValidator : AbstractValidator<UpdateMaintenancePolicyCommand>
{
    public UpdateMaintenancePolicyCommandValidator()
    {
        RuleFor(command => command.Message).MaximumLength(1000);
        RuleFor(command => command)
            .Must(command => !command.StartAt.HasValue || !command.EndAt.HasValue || command.StartAt <= command.EndAt)
            .WithMessage("Maintenance start time must be before end time.");
    }
}

public sealed class UpdateMaintenancePolicyCommandHandler : IRequestHandler<UpdateMaintenancePolicyCommand, Result>
{
    private readonly IAccessPolicyService _accessPolicyService;

    public UpdateMaintenancePolicyCommandHandler(IAccessPolicyService accessPolicyService)
    {
        _accessPolicyService = accessPolicyService;
    }

    public async Task<Result> Handle(UpdateMaintenancePolicyCommand request, CancellationToken cancellationToken)
    {
        await _accessPolicyService.UpdateMaintenancePolicyAsync(
            new UpdateMaintenancePolicyRequest
            {
                Enabled = request.Enabled,
                Message = request.Message,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                AllowAdminBypass = request.AllowAdminBypass
            },
            cancellationToken);

        return Result.Success();
    }
}
