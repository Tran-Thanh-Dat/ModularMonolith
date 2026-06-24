using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.OrganizationUsers;

namespace Organizations.Application.OrganizationUsers.GetUserOrganizations;

public sealed record GetUserOrganizationsQuery(Guid UserId, Guid? TenantId) : IQuery<IReadOnlyList<OrganizationUserListItemResponse>>;

public sealed class GetUserOrganizationsQueryValidator : AbstractValidator<GetUserOrganizationsQuery>
{
    public GetUserOrganizationsQueryValidator() => RuleFor(q => q.UserId).NotEmpty();
}

public sealed class GetUserOrganizationsQueryHandler : IRequestHandler<GetUserOrganizationsQuery, Result<IReadOnlyList<OrganizationUserListItemResponse>>>
{
    private readonly IOrganizationUserService _organizationUserService;

    public GetUserOrganizationsQueryHandler(IOrganizationUserService organizationUserService) =>
        _organizationUserService = organizationUserService;

    public async Task<Result<IReadOnlyList<OrganizationUserListItemResponse>>> Handle(GetUserOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var result = await _organizationUserService.GetUserOrganizationsAsync(request.UserId, request.TenantId, cancellationToken);
        return Result<IReadOnlyList<OrganizationUserListItemResponse>>.Success(result);
    }
}
