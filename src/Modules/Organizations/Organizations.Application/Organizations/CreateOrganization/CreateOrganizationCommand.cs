using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Organizations;
using Organizations.Domain.Enums;

namespace Organizations.Application.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(
    Guid TenantId,
    Guid? ParentOrganizationId,
    string Code,
    string Name,
    string? Description,
    OrganizationType Type,
    int SortOrder,
    string? Metadata) : ICommand<CreateOrganizationResponse>;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Code is required.").MaximumLength(100);
        RuleFor(c => c.Name).Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Name is required.").MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Metadata).MaximumLength(4000).When(c => c.Metadata is not null);
    }
}

public sealed class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, Result<CreateOrganizationResponse>>
{
    private readonly IOrganizationService _organizationService;

    public CreateOrganizationCommandHandler(IOrganizationService organizationService) =>
        _organizationService = organizationService;

    public async Task<Result<CreateOrganizationResponse>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var id = await _organizationService.CreateAsync(
            request.TenantId,
            request.ParentOrganizationId,
            request.Code,
            request.Name,
            request.Description,
            request.Type,
            request.SortOrder,
            request.Metadata,
            cancellationToken);

        return Result<CreateOrganizationResponse>.Success(new CreateOrganizationResponse { Id = id });
    }
}
