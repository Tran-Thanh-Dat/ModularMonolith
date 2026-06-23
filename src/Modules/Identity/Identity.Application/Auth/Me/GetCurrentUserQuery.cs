using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using Identity.Application.Abstractions;
using MediatR;

namespace Identity.Application.Auth.Me;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserResponse>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityUserRepository _userRepository;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IIdentityUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    public async Task<Result<CurrentUserResponse>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedException(CommonErrors.Unauthorized, "User is not authenticated.");
        }

        var userId = _currentUserService.UserId.Value;

        var user = await _userRepository.FindActiveByIdWithRolesAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(CommonErrors.Unauthorized, "User is not authenticated.");
        }

        var roles = user.Roles.Where(r => r.IsActive).Select(r => r.Code).Distinct().ToArray();
        var permissions = user.Roles
            .Where(r => r.IsActive)
            .SelectMany(r => r.Permissions)
            .Select(p => p.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Result<CurrentUserResponse>.Success(new CurrentUserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions
        });
    }
}
