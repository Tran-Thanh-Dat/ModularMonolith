using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Domain.Enums;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;

namespace AuthorizationPolicies.Application.PermissionPolicies;

public sealed record CreatePermissionPolicyCommand(
    string Code,
    string Name,
    string? Description,
    string PermissionCode,
    string ModuleCode,
    string ResourceType,
    string Action,
    AuthorizationScope Scope,
    AuthorizationEffect Effect,
    int Priority,
    string? Conditions) : ICommand<CreatePermissionPolicyResponse>;

public sealed class CreatePermissionPolicyCommandValidator : AbstractValidator<CreatePermissionPolicyCommand>
{
    public CreatePermissionPolicyCommandValidator()
    {
        RuleFor(c => c.Code).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(150);
        RuleFor(c => c.Name).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.PermissionCode).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(150);
        RuleFor(c => c.ModuleCode).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.ResourceType).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.Action).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.Scope).IsInEnum();
        RuleFor(c => c.Effect).IsInEnum();
    }
}

public sealed class CreatePermissionPolicyCommandHandler : IRequestHandler<CreatePermissionPolicyCommand, Result<CreatePermissionPolicyResponse>>
{
    private readonly IPermissionPolicyService _service;

    public CreatePermissionPolicyCommandHandler(IPermissionPolicyService service) => _service = service;

    public async Task<Result<CreatePermissionPolicyResponse>> Handle(CreatePermissionPolicyCommand request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(
            request.Code, request.Name, request.Description, request.PermissionCode, request.ModuleCode,
            request.ResourceType, request.Action, request.Scope, request.Effect, request.Priority,
            request.Conditions, cancellationToken);
        return Result<CreatePermissionPolicyResponse>.Success(new CreatePermissionPolicyResponse { Id = id });
    }
}

public sealed record UpdatePermissionPolicyCommand(
    Guid Id,
    string Name,
    string? Description,
    string PermissionCode,
    string ModuleCode,
    string ResourceType,
    string Action,
    AuthorizationScope Scope,
    AuthorizationEffect Effect,
    int Priority,
    string? Conditions) : ICommand;

public sealed class UpdatePermissionPolicyCommandValidator : AbstractValidator<UpdatePermissionPolicyCommand>
{
    public UpdatePermissionPolicyCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.PermissionCode).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(150);
        RuleFor(c => c.ModuleCode).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.ResourceType).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.Action).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.Scope).IsInEnum();
        RuleFor(c => c.Effect).IsInEnum();
    }
}

public sealed class UpdatePermissionPolicyCommandHandler : IRequestHandler<UpdatePermissionPolicyCommand, Result>
{
    private readonly IPermissionPolicyService _service;

    public UpdatePermissionPolicyCommandHandler(IPermissionPolicyService service) => _service = service;

    public async Task<Result> Handle(UpdatePermissionPolicyCommand request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(
            request.Id, request.Name, request.Description, request.PermissionCode, request.ModuleCode,
            request.ResourceType, request.Action, request.Scope, request.Effect, request.Priority,
            request.Conditions, cancellationToken);
        return Result.Success();
    }
}

public sealed record GetPermissionPoliciesQuery(
    string? Keyword,
    string? PermissionCode,
    string? ModuleCode,
    string? ResourceType,
    string? Action,
    AuthorizationScope? Scope,
    AuthorizationEffect? Effect,
    bool? IsActive,
    int PageIndex,
    int PageSize) : IQuery<PagedResult<PermissionPolicyListItemResponse>>;

public sealed class GetPermissionPoliciesQueryHandler : IRequestHandler<GetPermissionPoliciesQuery, Result<PagedResult<PermissionPolicyListItemResponse>>>
{
    private readonly IPermissionPolicyService _service;

    public GetPermissionPoliciesQueryHandler(IPermissionPolicyService service) => _service = service;

    public async Task<Result<PagedResult<PermissionPolicyListItemResponse>>> Handle(GetPermissionPoliciesQuery request, CancellationToken cancellationToken)
    {
        var data = await _service.GetListAsync(
            request.Keyword, request.PermissionCode, request.ModuleCode, request.ResourceType, request.Action,
            request.Scope, request.Effect, request.IsActive, request.PageIndex, request.PageSize, cancellationToken);
        return Result<PagedResult<PermissionPolicyListItemResponse>>.Success(data);
    }
}

public sealed record GetPermissionPolicyByIdQuery(Guid Id) : IQuery<PermissionPolicyDetailResponse>;

public sealed class GetPermissionPolicyByIdQueryHandler : IRequestHandler<GetPermissionPolicyByIdQuery, Result<PermissionPolicyDetailResponse>>
{
    private readonly IPermissionPolicyService _service;

    public GetPermissionPolicyByIdQueryHandler(IPermissionPolicyService service) => _service = service;

    public async Task<Result<PermissionPolicyDetailResponse>> Handle(GetPermissionPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(request.Id, cancellationToken);
        return item is null
            ? Result<PermissionPolicyDetailResponse>.Failure("PermissionPolicy.NotFound", "Permission policy not found.")
            : Result<PermissionPolicyDetailResponse>.Success(item);
    }
}

public sealed record DeletePermissionPolicyCommand(Guid Id) : ICommand;

public sealed class DeletePermissionPolicyCommandHandler : IRequestHandler<DeletePermissionPolicyCommand, Result>
{
    private readonly IPermissionPolicyService _service;

    public DeletePermissionPolicyCommandHandler(IPermissionPolicyService service) => _service = service;

    public async Task<Result> Handle(DeletePermissionPolicyCommand request, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record ActivatePermissionPolicyCommand(Guid Id) : ICommand;

public sealed class ActivatePermissionPolicyCommandHandler : IRequestHandler<ActivatePermissionPolicyCommand, Result>
{
    private readonly IPermissionPolicyService _service;

    public ActivatePermissionPolicyCommandHandler(IPermissionPolicyService service) => _service = service;

    public async Task<Result> Handle(ActivatePermissionPolicyCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record DeactivatePermissionPolicyCommand(Guid Id) : ICommand;

public sealed class DeactivatePermissionPolicyCommandHandler : IRequestHandler<DeactivatePermissionPolicyCommand, Result>
{
    private readonly IPermissionPolicyService _service;

    public DeactivatePermissionPolicyCommandHandler(IPermissionPolicyService service) => _service = service;

    public async Task<Result> Handle(DeactivatePermissionPolicyCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
