using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Organizations;

namespace Organizations.Application.Organizations.GetOrganizationTree;

public sealed record GetOrganizationTreeQuery(Guid TenantId) : IQuery<IReadOnlyList<OrganizationTreeNodeResponse>>;

public sealed class GetOrganizationTreeQueryValidator : AbstractValidator<GetOrganizationTreeQuery>
{
    public GetOrganizationTreeQueryValidator() => RuleFor(q => q.TenantId).NotEmpty();
}

public sealed class GetOrganizationTreeQueryHandler : IRequestHandler<GetOrganizationTreeQuery, Result<IReadOnlyList<OrganizationTreeNodeResponse>>>
{
    private readonly IOrganizationService _organizationService;

    public GetOrganizationTreeQueryHandler(IOrganizationService organizationService) =>
        _organizationService = organizationService;

    public async Task<Result<IReadOnlyList<OrganizationTreeNodeResponse>>> Handle(GetOrganizationTreeQuery request, CancellationToken cancellationToken)
    {
        var tree = await _organizationService.GetTreeAsync(request.TenantId, cancellationToken);
        return Result<IReadOnlyList<OrganizationTreeNodeResponse>>.Success(tree);
    }
}
