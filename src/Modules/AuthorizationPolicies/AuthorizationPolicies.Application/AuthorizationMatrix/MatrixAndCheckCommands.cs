using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.Models;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.Enums;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;

namespace AuthorizationPolicies.Application.AuthorizationMatrix;

public sealed record CreateAuthorizationMatrixEntryCommand(
    string ModuleCode,
    string ResourceType,
    string Action,
    AuthorizationScope Scope,
    string RequiredPermissionCode,
    string? Description,
    string? Metadata) : ICommand<CreateAuthorizationMatrixEntryResponse>;

public sealed class CreateAuthorizationMatrixEntryCommandValidator : AbstractValidator<CreateAuthorizationMatrixEntryCommand>
{
    public CreateAuthorizationMatrixEntryCommandValidator()
    {
        RuleFor(c => c.ModuleCode).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.ResourceType).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.Action).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.Scope).IsInEnum();
        RuleFor(c => c.RequiredPermissionCode).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(150);
        RuleFor(c => c.Description).MaximumLength(1000);
    }
}

public sealed class CreateAuthorizationMatrixEntryCommandHandler : IRequestHandler<CreateAuthorizationMatrixEntryCommand, Result<CreateAuthorizationMatrixEntryResponse>>
{
    private readonly IAuthorizationMatrixEntryService _service;

    public CreateAuthorizationMatrixEntryCommandHandler(IAuthorizationMatrixEntryService service) => _service = service;

    public async Task<Result<CreateAuthorizationMatrixEntryResponse>> Handle(CreateAuthorizationMatrixEntryCommand request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(
            request.ModuleCode, request.ResourceType, request.Action, request.Scope,
            request.RequiredPermissionCode, request.Description, request.Metadata, cancellationToken);
        return Result<CreateAuthorizationMatrixEntryResponse>.Success(new CreateAuthorizationMatrixEntryResponse { Id = id });
    }
}

public sealed record UpdateAuthorizationMatrixEntryCommand(
    Guid Id,
    string ModuleCode,
    string ResourceType,
    string Action,
    AuthorizationScope Scope,
    string RequiredPermissionCode,
    string? Description,
    string? Metadata) : ICommand;

public sealed class UpdateAuthorizationMatrixEntryCommandValidator : AbstractValidator<UpdateAuthorizationMatrixEntryCommand>
{
    public UpdateAuthorizationMatrixEntryCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.ModuleCode).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.ResourceType).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.Action).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(100);
        RuleFor(c => c.Scope).IsInEnum();
        RuleFor(c => c.RequiredPermissionCode).Must(c => !string.IsNullOrWhiteSpace(c)).MaximumLength(150);
        RuleFor(c => c.Description).MaximumLength(1000);
    }
}

public sealed class UpdateAuthorizationMatrixEntryCommandHandler : IRequestHandler<UpdateAuthorizationMatrixEntryCommand, Result>
{
    private readonly IAuthorizationMatrixEntryService _service;

    public UpdateAuthorizationMatrixEntryCommandHandler(IAuthorizationMatrixEntryService service) => _service = service;

    public async Task<Result> Handle(UpdateAuthorizationMatrixEntryCommand request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(
            request.Id, request.ModuleCode, request.ResourceType, request.Action, request.Scope,
            request.RequiredPermissionCode, request.Description, request.Metadata, cancellationToken);
        return Result.Success();
    }
}

public sealed record GetAuthorizationMatrixEntriesQuery(
    string? ModuleCode,
    string? ResourceType,
    string? Action,
    AuthorizationScope? Scope,
    string? RequiredPermissionCode,
    bool? IsEnabled,
    int PageIndex,
    int PageSize) : IQuery<PagedResult<AuthorizationMatrixEntryListItemResponse>>;

public sealed class GetAuthorizationMatrixEntriesQueryHandler : IRequestHandler<GetAuthorizationMatrixEntriesQuery, Result<PagedResult<AuthorizationMatrixEntryListItemResponse>>>
{
    private readonly IAuthorizationMatrixEntryService _service;

    public GetAuthorizationMatrixEntriesQueryHandler(IAuthorizationMatrixEntryService service) => _service = service;

    public async Task<Result<PagedResult<AuthorizationMatrixEntryListItemResponse>>> Handle(GetAuthorizationMatrixEntriesQuery request, CancellationToken cancellationToken)
    {
        var data = await _service.GetListAsync(
            request.ModuleCode, request.ResourceType, request.Action, request.Scope,
            request.RequiredPermissionCode, request.IsEnabled, request.PageIndex, request.PageSize, cancellationToken);
        return Result<PagedResult<AuthorizationMatrixEntryListItemResponse>>.Success(data);
    }
}

public sealed record GetAuthorizationMatrixEntryByIdQuery(Guid Id) : IQuery<AuthorizationMatrixEntryDetailResponse>;

public sealed class GetAuthorizationMatrixEntryByIdQueryHandler : IRequestHandler<GetAuthorizationMatrixEntryByIdQuery, Result<AuthorizationMatrixEntryDetailResponse>>
{
    private readonly IAuthorizationMatrixEntryService _service;

    public GetAuthorizationMatrixEntryByIdQueryHandler(IAuthorizationMatrixEntryService service) => _service = service;

    public async Task<Result<AuthorizationMatrixEntryDetailResponse>> Handle(GetAuthorizationMatrixEntryByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(request.Id, cancellationToken);
        return item is null
            ? Result<AuthorizationMatrixEntryDetailResponse>.Failure("AuthorizationMatrix.NotFound", "Matrix entry not found.")
            : Result<AuthorizationMatrixEntryDetailResponse>.Success(item);
    }
}

public sealed record DeleteAuthorizationMatrixEntryCommand(Guid Id) : ICommand;

public sealed class DeleteAuthorizationMatrixEntryCommandHandler : IRequestHandler<DeleteAuthorizationMatrixEntryCommand, Result>
{
    private readonly IAuthorizationMatrixEntryService _service;

    public DeleteAuthorizationMatrixEntryCommandHandler(IAuthorizationMatrixEntryService service) => _service = service;

    public async Task<Result> Handle(DeleteAuthorizationMatrixEntryCommand request, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record EnableAuthorizationMatrixEntryCommand(Guid Id) : ICommand;

public sealed class EnableAuthorizationMatrixEntryCommandHandler : IRequestHandler<EnableAuthorizationMatrixEntryCommand, Result>
{
    private readonly IAuthorizationMatrixEntryService _service;

    public EnableAuthorizationMatrixEntryCommandHandler(IAuthorizationMatrixEntryService service) => _service = service;

    public async Task<Result> Handle(EnableAuthorizationMatrixEntryCommand request, CancellationToken cancellationToken)
    {
        await _service.EnableAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record DisableAuthorizationMatrixEntryCommand(Guid Id) : ICommand;

public sealed class DisableAuthorizationMatrixEntryCommandHandler : IRequestHandler<DisableAuthorizationMatrixEntryCommand, Result>
{
    private readonly IAuthorizationMatrixEntryService _service;

    public DisableAuthorizationMatrixEntryCommandHandler(IAuthorizationMatrixEntryService service) => _service = service;

    public async Task<Result> Handle(DisableAuthorizationMatrixEntryCommand request, CancellationToken cancellationToken)
    {
        await _service.DisableAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
