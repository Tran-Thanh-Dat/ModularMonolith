using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Tenants.DeleteTenant;

public sealed record DeleteTenantCommand(Guid Id) : ICommand;

public sealed class DeleteTenantCommandValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, Result>
{
    private readonly ITenantService _tenantService;

    public DeleteTenantCommandHandler(ITenantService tenantService) => _tenantService = tenantService;

    public async Task<Result> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        await _tenantService.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
