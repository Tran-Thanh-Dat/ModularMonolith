using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.Enums;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;

namespace AuthorizationPolicies.Application.UserPermissionPolicyOverrides;

public sealed record CreateUserPermissionPolicyOverrideCommand(
    Guid UserId,
    Guid PermissionPolicyId,
    AuthorizationEffect Effect,
    DateTimeOffset? ExpiresAt,
    string? Reason) : ICommand<CreateUserPermissionPolicyOverrideResponse>;

public sealed class CreateUserPermissionPolicyOverrideCommandValidator : AbstractValidator<CreateUserPermissionPolicyOverrideCommand>
{
    public CreateUserPermissionPolicyOverrideCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.PermissionPolicyId).NotEmpty();
        RuleFor(c => c.Effect).IsInEnum();
        RuleFor(c => c.Reason).MaximumLength(1000);
        RuleFor(c => c.ExpiresAt).Must((_, expiresAt) => !expiresAt.HasValue || expiresAt > DateTimeOffset.UtcNow)
            .WithMessage("ExpiresAt must be in the future.")
            .When(c => c.ExpiresAt.HasValue);
    }
}

public sealed class CreateUserPermissionPolicyOverrideCommandHandler : IRequestHandler<CreateUserPermissionPolicyOverrideCommand, Result<CreateUserPermissionPolicyOverrideResponse>>
{
    private readonly IUserPermissionPolicyOverrideService _service;

    public CreateUserPermissionPolicyOverrideCommandHandler(IUserPermissionPolicyOverrideService service) => _service = service;

    public async Task<Result<CreateUserPermissionPolicyOverrideResponse>> Handle(CreateUserPermissionPolicyOverrideCommand request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(
            request.UserId, request.PermissionPolicyId, request.Effect, request.ExpiresAt, request.Reason, cancellationToken);
        return Result<CreateUserPermissionPolicyOverrideResponse>.Success(new CreateUserPermissionPolicyOverrideResponse { Id = id });
    }
}

public sealed record GetUserPermissionPolicyOverridesQuery(
    Guid? UserId,
    Guid? PermissionPolicyId,
    AuthorizationEffect? Effect,
    bool? IsActive,
    bool IncludeExpired,
    int PageIndex,
    int PageSize) : IQuery<PagedResult<UserPermissionPolicyOverrideListItemResponse>>;

public sealed class GetUserPermissionPolicyOverridesQueryHandler : IRequestHandler<GetUserPermissionPolicyOverridesQuery, Result<PagedResult<UserPermissionPolicyOverrideListItemResponse>>>
{
    private readonly IUserPermissionPolicyOverrideService _service;

    public GetUserPermissionPolicyOverridesQueryHandler(IUserPermissionPolicyOverrideService service) => _service = service;

    public async Task<Result<PagedResult<UserPermissionPolicyOverrideListItemResponse>>> Handle(GetUserPermissionPolicyOverridesQuery request, CancellationToken cancellationToken)
    {
        var data = await _service.GetListAsync(
            request.UserId, request.PermissionPolicyId, request.Effect, request.IsActive, request.IncludeExpired,
            request.PageIndex, request.PageSize, cancellationToken);
        return Result<PagedResult<UserPermissionPolicyOverrideListItemResponse>>.Success(data);
    }
}

public sealed record RemoveUserPermissionPolicyOverrideCommand(Guid Id) : ICommand;

public sealed class RemoveUserPermissionPolicyOverrideCommandHandler : IRequestHandler<RemoveUserPermissionPolicyOverrideCommand, Result>
{
    private readonly IUserPermissionPolicyOverrideService _service;

    public RemoveUserPermissionPolicyOverrideCommandHandler(IUserPermissionPolicyOverrideService service) => _service = service;

    public async Task<Result> Handle(RemoveUserPermissionPolicyOverrideCommand request, CancellationToken cancellationToken)
    {
        await _service.RemoveAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record ActivateUserPermissionPolicyOverrideCommand(Guid Id) : ICommand;

public sealed class ActivateUserPermissionPolicyOverrideCommandHandler : IRequestHandler<ActivateUserPermissionPolicyOverrideCommand, Result>
{
    private readonly IUserPermissionPolicyOverrideService _service;

    public ActivateUserPermissionPolicyOverrideCommandHandler(IUserPermissionPolicyOverrideService service) => _service = service;

    public async Task<Result> Handle(ActivateUserPermissionPolicyOverrideCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record DeactivateUserPermissionPolicyOverrideCommand(Guid Id) : ICommand;

public sealed class DeactivateUserPermissionPolicyOverrideCommandHandler : IRequestHandler<DeactivateUserPermissionPolicyOverrideCommand, Result>
{
    private readonly IUserPermissionPolicyOverrideService _service;

    public DeactivateUserPermissionPolicyOverrideCommandHandler(IUserPermissionPolicyOverrideService service) => _service = service;

    public async Task<Result> Handle(DeactivateUserPermissionPolicyOverrideCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
