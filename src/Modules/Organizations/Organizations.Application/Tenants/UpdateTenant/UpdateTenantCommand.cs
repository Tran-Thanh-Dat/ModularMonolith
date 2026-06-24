using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Tenants.UpdateTenant;

public sealed record UpdateTenantCommand(Guid Id, string Name, string? Description, string? Metadata) : ICommand;

public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Name is required.").MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.Metadata).MaximumLength(4000).When(c => c.Metadata is not null);
    }
}

public sealed class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, Result>
{
    private readonly ITenantService _tenantService;

    public UpdateTenantCommandHandler(ITenantService tenantService) => _tenantService = tenantService;

    public async Task<Result> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        await _tenantService.UpdateAsync(request.Id, request.Name, request.Description, request.Metadata, cancellationToken);
        return Result.Success();
    }
}
