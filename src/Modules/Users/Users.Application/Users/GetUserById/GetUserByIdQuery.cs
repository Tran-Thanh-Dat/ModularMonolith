using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using MediatR;
using Users.Application.Abstractions;

namespace Users.Application.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IQuery<UserDetailResponse>;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailResponse>>
{
    private readonly IUserManagementService _userManagementService;

    public GetUserByIdQueryHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<Result<UserDetailResponse>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userManagementService.GetUserByIdAsync(request.Id, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                UserErrors.NotFound,
                $"User with id '{request.Id}' was not found.");
        }

        return Result<UserDetailResponse>.Success(user);
    }
}
