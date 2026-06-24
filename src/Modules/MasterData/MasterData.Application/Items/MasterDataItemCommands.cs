using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MasterData.Application.Abstractions;
using MasterData.Application.Dtos;
using MasterData.Domain.Errors;
using MediatR;

namespace MasterData.Application.Items;

public sealed record CreateMasterDataItemCommand(
    Guid GroupId,
    string Code,
    string Name,
    string? Value,
    string? Description,
    Guid? ParentItemId,
    bool IsDefault,
    int SortOrder,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? Metadata) : ICommand<CreateMasterDataItemResponse>;

public sealed class CreateMasterDataItemCommandValidator : AbstractValidator<CreateMasterDataItemCommand>
{
    public CreateMasterDataItemCommandValidator()
    {
        RuleFor(c => c.GroupId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Value).MaximumLength(500);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(c => c).Must(c => !c.EffectiveFrom.HasValue || !c.EffectiveTo.HasValue || c.EffectiveFrom <= c.EffectiveTo)
            .WithMessage("EffectiveFrom must be less than or equal to EffectiveTo.");
    }
}

public sealed class CreateMasterDataItemCommandHandler : IRequestHandler<CreateMasterDataItemCommand, Result<CreateMasterDataItemResponse>>
{
    private readonly IMasterDataItemService _service;
    public CreateMasterDataItemCommandHandler(IMasterDataItemService service) => _service = service;
    public async Task<Result<CreateMasterDataItemResponse>> Handle(CreateMasterDataItemCommand request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request.GroupId, request.Code, request.Name, request.Value, request.Description, request.ParentItemId, request.IsDefault, request.SortOrder, request.EffectiveFrom, request.EffectiveTo, request.Metadata, cancellationToken);
        return Result<CreateMasterDataItemResponse>.Success(new CreateMasterDataItemResponse { Id = id });
    }
}

public sealed record UpdateMasterDataItemCommand(Guid Id, string? Code, string Name, string? Value, string? Description, Guid? ParentItemId, int SortOrder, DateTimeOffset? EffectiveFrom, DateTimeOffset? EffectiveTo, string? Metadata) : ICommand;
public sealed class UpdateMasterDataItemCommandValidator : AbstractValidator<UpdateMasterDataItemCommand>
{
    public UpdateMasterDataItemCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Value).MaximumLength(500);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(c => c).Must(c => !c.EffectiveFrom.HasValue || !c.EffectiveTo.HasValue || c.EffectiveFrom <= c.EffectiveTo);
    }
}
public sealed class UpdateMasterDataItemCommandHandler : IRequestHandler<UpdateMasterDataItemCommand, Result>
{
    private readonly IMasterDataItemService _service;
    public UpdateMasterDataItemCommandHandler(IMasterDataItemService service) => _service = service;
    public async Task<Result> Handle(UpdateMasterDataItemCommand request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(request.Id, request.Code, request.Name, request.Value, request.Description, request.ParentItemId, request.SortOrder, request.EffectiveFrom, request.EffectiveTo, request.Metadata, cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteMasterDataItemCommand(Guid Id) : ICommand;
public sealed class DeleteMasterDataItemCommandHandler : IRequestHandler<DeleteMasterDataItemCommand, Result>
{
    private readonly IMasterDataItemService _service;
    public DeleteMasterDataItemCommandHandler(IMasterDataItemService service) => _service = service;
    public async Task<Result> Handle(DeleteMasterDataItemCommand request, CancellationToken cancellationToken) { await _service.DeleteAsync(request.Id, cancellationToken); return Result.Success(); }
}

public sealed record ActivateMasterDataItemCommand(Guid Id) : ICommand;
public sealed class ActivateMasterDataItemCommandHandler : IRequestHandler<ActivateMasterDataItemCommand, Result>
{
    private readonly IMasterDataItemService _service;
    public ActivateMasterDataItemCommandHandler(IMasterDataItemService service) => _service = service;
    public async Task<Result> Handle(ActivateMasterDataItemCommand request, CancellationToken cancellationToken) { await _service.ActivateAsync(request.Id, cancellationToken); return Result.Success(); }
}

public sealed record DeactivateMasterDataItemCommand(Guid Id) : ICommand;
public sealed class DeactivateMasterDataItemCommandHandler : IRequestHandler<DeactivateMasterDataItemCommand, Result>
{
    private readonly IMasterDataItemService _service;
    public DeactivateMasterDataItemCommandHandler(IMasterDataItemService service) => _service = service;
    public async Task<Result> Handle(DeactivateMasterDataItemCommand request, CancellationToken cancellationToken) { await _service.DeactivateAsync(request.Id, cancellationToken); return Result.Success(); }
}

public sealed record SetDefaultMasterDataItemCommand(Guid Id) : ICommand;
public sealed class SetDefaultMasterDataItemCommandHandler : IRequestHandler<SetDefaultMasterDataItemCommand, Result>
{
    private readonly IMasterDataItemService _service;
    public SetDefaultMasterDataItemCommandHandler(IMasterDataItemService service) => _service = service;
    public async Task<Result> Handle(SetDefaultMasterDataItemCommand request, CancellationToken cancellationToken) { await _service.SetDefaultAsync(request.Id, cancellationToken); return Result.Success(); }
}

public sealed record GetMasterDataItemByIdQuery(Guid Id) : IQuery<MasterDataItemDetailResponse>;
public sealed class GetMasterDataItemByIdQueryHandler : IRequestHandler<GetMasterDataItemByIdQuery, Result<MasterDataItemDetailResponse>>
{
    private readonly IMasterDataItemService _service;
    public GetMasterDataItemByIdQueryHandler(IMasterDataItemService service) => _service = service;
    public async Task<Result<MasterDataItemDetailResponse>> Handle(GetMasterDataItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(request.Id, cancellationToken);
        return item is null ? Result<MasterDataItemDetailResponse>.Failure(MasterDataItemErrors.NotFound, "Master data item not found.") : Result<MasterDataItemDetailResponse>.Success(item);
    }
}

public sealed record GetMasterDataItemsQuery(Guid? GroupId, string? GroupCode, string? Keyword, string? Code, Guid? ParentItemId, bool? IsSystem, bool? IsDefault, bool? IsActive, DateTimeOffset? EffectiveAt, int PageIndex, int PageSize)
    : IQuery<PagedResult<MasterDataItemListItemResponse>>;

public sealed class GetMasterDataItemsQueryHandler : IRequestHandler<GetMasterDataItemsQuery, Result<PagedResult<MasterDataItemListItemResponse>>>
{
    private readonly IMasterDataItemService _service;
    public GetMasterDataItemsQueryHandler(IMasterDataItemService service) => _service = service;
    public async Task<Result<PagedResult<MasterDataItemListItemResponse>>> Handle(GetMasterDataItemsQuery request, CancellationToken cancellationToken)
    {
        var result = await _service.GetListAsync(new MasterDataItemListQuery(request.GroupId, request.GroupCode, request.Keyword, request.Code, request.ParentItemId, request.IsSystem, request.IsDefault, request.IsActive, request.EffectiveAt, request.PageIndex, request.PageSize), cancellationToken);
        return Result<PagedResult<MasterDataItemListItemResponse>>.Success(result);
    }
}
