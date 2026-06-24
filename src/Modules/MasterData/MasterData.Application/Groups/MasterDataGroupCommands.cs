using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MasterData.Application.Abstractions;
using MasterData.Application.Dtos;
using MasterData.Domain.Enums;
using MasterData.Domain.Errors;
using MediatR;

namespace MasterData.Application.Groups;

public sealed record CreateMasterDataGroupCommand(
    string Code,
    string Name,
    string? Description,
    MasterDataScope Scope,
    Guid? TenantId,
    Guid? OrganizationId,
    int SortOrder,
    string? Metadata) : ICommand<CreateMasterDataGroupResponse>;

public sealed class CreateMasterDataGroupCommandValidator : AbstractValidator<CreateMasterDataGroupCommand>
{
    public CreateMasterDataGroupCommandValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.Scope).IsInEnum();
        RuleFor(c => c.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(c => c.TenantId).Null().When(c => c.Scope == MasterDataScope.Global);
        RuleFor(c => c.OrganizationId).Null().When(c => c.Scope == MasterDataScope.Global);
        RuleFor(c => c.TenantId).NotEmpty().When(c => c.Scope is MasterDataScope.Tenant or MasterDataScope.Organization);
        RuleFor(c => c.OrganizationId).Null().When(c => c.Scope == MasterDataScope.Tenant);
        RuleFor(c => c.OrganizationId).NotEmpty().When(c => c.Scope == MasterDataScope.Organization);
    }
}

public sealed class CreateMasterDataGroupCommandHandler : IRequestHandler<CreateMasterDataGroupCommand, Result<CreateMasterDataGroupResponse>>
{
    private readonly IMasterDataGroupService _service;
    public CreateMasterDataGroupCommandHandler(IMasterDataGroupService service) => _service = service;
    public async Task<Result<CreateMasterDataGroupResponse>> Handle(CreateMasterDataGroupCommand request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request.Code, request.Name, request.Description, request.Scope, request.TenantId, request.OrganizationId, request.SortOrder, request.Metadata, cancellationToken);
        return Result<CreateMasterDataGroupResponse>.Success(new CreateMasterDataGroupResponse { Id = id });
    }
}

public sealed record UpdateMasterDataGroupCommand(Guid Id, string? Code, string Name, string? Description, int SortOrder, string? Metadata) : ICommand;
public sealed class UpdateMasterDataGroupCommandValidator : AbstractValidator<UpdateMasterDataGroupCommand>
{
    public UpdateMasterDataGroupCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Code).MaximumLength(100).When(c => !string.IsNullOrWhiteSpace(c.Code));
    }
}
public sealed class UpdateMasterDataGroupCommandHandler : IRequestHandler<UpdateMasterDataGroupCommand, Result>
{
    private readonly IMasterDataGroupService _service;
    public UpdateMasterDataGroupCommandHandler(IMasterDataGroupService service) => _service = service;
    public async Task<Result> Handle(UpdateMasterDataGroupCommand request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(request.Id, request.Code, request.Name, request.Description, request.SortOrder, request.Metadata, cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteMasterDataGroupCommand(Guid Id) : ICommand;
public sealed class DeleteMasterDataGroupCommandHandler : IRequestHandler<DeleteMasterDataGroupCommand, Result>
{
    private readonly IMasterDataGroupService _service;
    public DeleteMasterDataGroupCommandHandler(IMasterDataGroupService service) => _service = service;
    public async Task<Result> Handle(DeleteMasterDataGroupCommand request, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record ActivateMasterDataGroupCommand(Guid Id) : ICommand;
public sealed class ActivateMasterDataGroupCommandHandler : IRequestHandler<ActivateMasterDataGroupCommand, Result>
{
    private readonly IMasterDataGroupService _service;
    public ActivateMasterDataGroupCommandHandler(IMasterDataGroupService service) => _service = service;
    public async Task<Result> Handle(ActivateMasterDataGroupCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record DeactivateMasterDataGroupCommand(Guid Id) : ICommand;
public sealed class DeactivateMasterDataGroupCommandHandler : IRequestHandler<DeactivateMasterDataGroupCommand, Result>
{
    private readonly IMasterDataGroupService _service;
    public DeactivateMasterDataGroupCommandHandler(IMasterDataGroupService service) => _service = service;
    public async Task<Result> Handle(DeactivateMasterDataGroupCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record GetMasterDataGroupByIdQuery(Guid Id) : IQuery<MasterDataGroupDetailResponse>;
public sealed class GetMasterDataGroupByIdQueryHandler : IRequestHandler<GetMasterDataGroupByIdQuery, Result<MasterDataGroupDetailResponse>>
{
    private readonly IMasterDataGroupService _service;
    public GetMasterDataGroupByIdQueryHandler(IMasterDataGroupService service) => _service = service;
    public async Task<Result<MasterDataGroupDetailResponse>> Handle(GetMasterDataGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var group = await _service.GetByIdAsync(request.Id, cancellationToken);
        return group is null
            ? Result<MasterDataGroupDetailResponse>.Failure(MasterDataGroupErrors.NotFound, "Master data group not found.")
            : Result<MasterDataGroupDetailResponse>.Success(group);
    }
}

public sealed record GetMasterDataGroupsQuery(
    string? Keyword, string? Code, MasterDataScope? Scope, Guid? TenantId, Guid? OrganizationId, bool? IsSystem, bool? IsActive, int PageIndex, int PageSize)
    : IQuery<PagedResult<MasterDataGroupListItemResponse>>;

public sealed class GetMasterDataGroupsQueryHandler : IRequestHandler<GetMasterDataGroupsQuery, Result<PagedResult<MasterDataGroupListItemResponse>>>
{
    private readonly IMasterDataGroupService _service;
    public GetMasterDataGroupsQueryHandler(IMasterDataGroupService service) => _service = service;
    public async Task<Result<PagedResult<MasterDataGroupListItemResponse>>> Handle(GetMasterDataGroupsQuery request, CancellationToken cancellationToken)
    {
        var result = await _service.GetListAsync(new MasterDataGroupListQuery(
            request.Keyword, request.Code, request.Scope, request.TenantId, request.OrganizationId, request.IsSystem, request.IsActive, request.PageIndex, request.PageSize), cancellationToken);
        return Result<PagedResult<MasterDataGroupListItemResponse>>.Success(result);
    }
}
