using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.OrganizationUsers;

namespace Organizations.Application.OrganizationUsers.GetOrganizationUserById;

public sealed record GetOrganizationUserByIdQuery(Guid Id) : IQuery<OrganizationUserDetailResponse>;

public sealed class GetOrganizationUserByIdQueryValidator : AbstractValidator<GetOrganizationUserByIdQuery>
{
    public GetOrganizationUserByIdQueryValidator() => RuleFor(q => q.Id).NotEmpty();
}

public sealed class GetOrganizationUserByIdQueryHandler : IRequestHandler<GetOrganizationUserByIdQuery, Result<OrganizationUserDetailResponse>>
{
    private readonly IOrganizationUserService _organizationUserService;

    public GetOrganizationUserByIdQueryHandler(IOrganizationUserService organizationUserService) =>
        _organizationUserService = organizationUserService;

    public async Task<Result<OrganizationUserDetailResponse>> Handle(
        GetOrganizationUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await _organizationUserService.GetByIdAsync(request.Id, cancellationToken);
        if (membership is null)
        {
            throw new NotFoundException(
                OrganizationUserErrors.NotFound,
                $"Organization membership with id '{request.Id}' was not found.");
        }

        return Result<OrganizationUserDetailResponse>.Success(membership);
    }
}
