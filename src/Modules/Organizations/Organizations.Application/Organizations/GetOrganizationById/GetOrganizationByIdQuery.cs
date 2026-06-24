using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Organizations;

namespace Organizations.Application.Organizations.GetOrganizationById;

public sealed record GetOrganizationByIdQuery(Guid Id) : IQuery<OrganizationDetailResponse>;

public sealed class GetOrganizationByIdQueryValidator : AbstractValidator<GetOrganizationByIdQuery>
{
    public GetOrganizationByIdQueryValidator() => RuleFor(q => q.Id).NotEmpty();
}

public sealed class GetOrganizationByIdQueryHandler : IRequestHandler<GetOrganizationByIdQuery, Result<OrganizationDetailResponse>>
{
    private readonly IOrganizationService _organizationService;

    public GetOrganizationByIdQueryHandler(IOrganizationService organizationService) =>
        _organizationService = organizationService;

    public async Task<Result<OrganizationDetailResponse>> Handle(GetOrganizationByIdQuery request, CancellationToken cancellationToken)
    {
        var organization = await _organizationService.GetByIdAsync(request.Id, cancellationToken);
        if (organization is null)
        {
            throw new NotFoundException(OrganizationErrors.NotFound, $"Organization with id '{request.Id}' was not found.");
        }

        return Result<OrganizationDetailResponse>.Success(organization);
    }
}
