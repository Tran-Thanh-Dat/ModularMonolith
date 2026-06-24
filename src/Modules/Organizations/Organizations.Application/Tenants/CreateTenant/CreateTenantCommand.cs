using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Tenants;

namespace Organizations.Application.Tenants.CreateTenant;

public sealed record CreateTenantCommand(
    string Code,
    string Name,
    string? Description,
    string? Metadata) : ICommand<CreateTenantResponse>;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(c => c.Code).Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Code is required.").MaximumLength(100);
        RuleFor(c => c.Name).Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Name is required.").MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.Metadata).MaximumLength(4000).When(c => c.Metadata is not null);
    }
}

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<CreateTenantResponse>>
{
    private readonly ITenantService _tenantService;

    public CreateTenantCommandHandler(ITenantService tenantService) => _tenantService = tenantService;

    public async Task<Result<CreateTenantResponse>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var id = await _tenantService.CreateAsync(request.Code, request.Name, request.Description, request.Metadata, cancellationToken);
        return Result<CreateTenantResponse>.Success(new CreateTenantResponse { Id = id });
    }
}
