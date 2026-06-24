using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Tenants.ActivateTenant;

public sealed record ActivateTenantCommand(Guid Id) : ICommand;

public sealed class ActivateTenantCommandValidator : AbstractValidator<ActivateTenantCommand>
{
    public ActivateTenantCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class ActivateTenantCommandHandler : IRequestHandler<ActivateTenantCommand, Result>
{
    private readonly ITenantService _tenantService;

    public ActivateTenantCommandHandler(ITenantService tenantService) => _tenantService = tenantService;

    public async Task<Result> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        await _tenantService.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
