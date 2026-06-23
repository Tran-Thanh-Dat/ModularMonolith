using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Identity.Application.Abstractions;
using MediatR;

namespace Identity.Application.Account.Profile;

public sealed class AccountProfileResponse
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string FullName { get; init; } = default!;
}

public sealed record GetAccountProfileQuery : IQuery<AccountProfileResponse>;

public sealed class GetAccountProfileQueryHandler
    : IRequestHandler<GetAccountProfileQuery, Result<AccountProfileResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityUserRepository _userRepository;

    public GetAccountProfileQueryHandler(
        ICurrentUserService currentUserService,
        IIdentityUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    public async Task<Result<AccountProfileResponse>> Handle(
        GetAccountProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedException(CommonErrors.Unauthorized, "User is not authenticated.");
        }

        var user = await _userRepository.FindActiveByIdForUpdateAsync(
            _currentUserService.UserId.Value,
            cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(CommonErrors.Unauthorized, "User is not authenticated.");
        }

        return Result<AccountProfileResponse>.Success(new AccountProfileResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName
        });
    }
}

public sealed record UpdateAccountProfileCommand(
    string Email,
    string FullName) : ICommand<AccountProfileResponse>;

public sealed class UpdateAccountProfileCommandValidator : AbstractValidator<UpdateAccountProfileCommand>
{
    public UpdateAccountProfileCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(255);
    }
}

public sealed class UpdateAccountProfileCommandHandler
    : IRequestHandler<UpdateAccountProfileCommand, Result<AccountProfileResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IActivityLogService _activityLogService;

    public UpdateAccountProfileCommandHandler(
        ICurrentUserService currentUserService,
        IIdentityUserRepository userRepository,
        IActivityLogService activityLogService)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _activityLogService = activityLogService;
    }

    public async Task<Result<AccountProfileResponse>> Handle(
        UpdateAccountProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedException(CommonErrors.Unauthorized, "User is not authenticated.");
        }

        var userId = _currentUserService.UserId.Value;
        var user = await _userRepository.FindActiveByIdForUpdateAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(CommonErrors.Unauthorized, "User is not authenticated.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailTaken = await _userRepository.EmailExistsForOtherUserAsync(
                normalizedEmail,
                userId,
                cancellationToken);

            if (emailTaken)
            {
                throw new ConflictException(
                    UserErrors.EmailAlreadyExists,
                    $"Email '{normalizedEmail}' is already taken.");
            }
        }

        user.UpdateProfile(request.FullName, normalizedEmail);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"User '{user.UserName}' updated their profile.",
            AuditLogConstants.Modules.Identity,
            userId,
            user.UserName,
            cancellationToken);

        return Result<AccountProfileResponse>.Success(new AccountProfileResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName
        });
    }
}
