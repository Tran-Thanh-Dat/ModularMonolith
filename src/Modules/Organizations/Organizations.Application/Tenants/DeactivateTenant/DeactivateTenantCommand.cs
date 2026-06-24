using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Tenants.DeactivateTenant;

public sealed record DeactivateTenantCommand(Guid Id) : ICommand;

public sealed class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
{
    public DeactivateTenantCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class DeactivateTenantCommandHandler : IRequestHandler<DeactivateTenantCommand, Result>
{
    private readonly ITenantService _tenantService;

    public DeactivateTenantCommandHandler(ITenantService tenantService) => _tenantService = tenantService;

    public async Task<Result> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        await _tenantService.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
