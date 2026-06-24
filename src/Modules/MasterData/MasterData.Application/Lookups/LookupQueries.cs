using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MasterData.Application.Abstractions;
using MasterData.Application.Dtos;
using MasterData.Domain.Constants;
using MasterData.Domain.Enums;
using MasterData.Domain.Errors;
using MediatR;

namespace MasterData.Application.Lookups;

public sealed record GetLookupByGroupCodeQuery(
    string GroupCode,
    MasterDataScope Scope,
    Guid? TenantId,
    Guid? OrganizationId,
    bool IncludeInactive,
    DateTimeOffset? EffectiveAt,
    bool IncludeMetadata) : IQuery<IReadOnlyList<LookupItemDto>>;

public sealed class GetLookupByGroupCodeQueryValidator : AbstractValidator<GetLookupByGroupCodeQuery>
{
    public GetLookupByGroupCodeQueryValidator()
    {
        RuleFor(q => q.GroupCode).NotEmpty().MaximumLength(100);
        RuleFor(q => q.Scope).IsInEnum();
    }
}

public sealed class GetLookupByGroupCodeQueryHandler : IRequestHandler<GetLookupByGroupCodeQuery, Result<IReadOnlyList<LookupItemDto>>>
{
    private readonly ILookupService _service;
    public GetLookupByGroupCodeQueryHandler(ILookupService service) => _service = service;
    public async Task<Result<IReadOnlyList<LookupItemDto>>> Handle(GetLookupByGroupCodeQuery request, CancellationToken cancellationToken)
    {
        var items = await _service.GetByGroupCodeAsync(new LookupQuery(request.GroupCode, null, request.Scope, request.TenantId, request.OrganizationId, request.IncludeInactive, request.EffectiveAt, request.IncludeMetadata), cancellationToken);
        return Result<IReadOnlyList<LookupItemDto>>.Success(items);
    }
}

public sealed record GetLookupsQuery(
    string GroupCodes,
    MasterDataScope Scope,
    Guid? TenantId,
    Guid? OrganizationId,
    bool IncludeInactive,
    DateTimeOffset? EffectiveAt,
    bool IncludeMetadata) : IQuery<IReadOnlyList<LookupGroupDto>>;

public sealed class GetLookupsQueryValidator : AbstractValidator<GetLookupsQuery>
{
    public GetLookupsQueryValidator()
    {
        RuleFor(q => q.GroupCodes).NotEmpty();
        RuleFor(q => q.Scope).IsInEnum();
        RuleFor(q => q.GroupCodes)
            .Must(c => MasterDataLookupValidation.SplitGroupCodes(c).Count > 0)
            .WithMessage(LookupErrors.GroupCodesRequired);
        RuleFor(q => q.GroupCodes)
            .Must(c => MasterDataLookupValidation.SplitGroupCodes(c).Count <= MasterDataConstants.MaxBatchGroupCodes)
            .WithErrorCode(LookupErrors.TooManyGroupCodes)
            .WithMessage($"Maximum {MasterDataConstants.MaxBatchGroupCodes} group codes are allowed.");
        RuleFor(q => q.GroupCodes)
            .Must(c => MasterDataLookupValidation.SplitGroupCodes(c).All(code => code.Length <= 100))
            .WithMessage("Each group code must be at most 100 characters.");
    }
}

public sealed class GetLookupsQueryHandler : IRequestHandler<GetLookupsQuery, Result<IReadOnlyList<LookupGroupDto>>>
{
    private readonly ILookupService _service;
    public GetLookupsQueryHandler(ILookupService service) => _service = service;
    public async Task<Result<IReadOnlyList<LookupGroupDto>>> Handle(GetLookupsQuery request, CancellationToken cancellationToken)
    {
        var codes = MasterDataLookupValidation.SplitGroupCodes(request.GroupCodes);
        var groups = await _service.GetMultipleAsync(new LookupQuery(null, codes, request.Scope, request.TenantId, request.OrganizationId, request.IncludeInactive, request.EffectiveAt, request.IncludeMetadata), cancellationToken);
        return Result<IReadOnlyList<LookupGroupDto>>.Success(groups);
    }
}

public sealed record BatchLookupQuery(
    IReadOnlyList<string> GroupCodes,
    MasterDataScope Scope,
    Guid? TenantId,
    Guid? OrganizationId,
    bool IncludeInactive,
    DateTimeOffset? EffectiveAt,
    bool IncludeMetadata) : IQuery<BatchLookupResponse>;

public sealed class BatchLookupQueryValidator : AbstractValidator<BatchLookupQuery>
{
    public BatchLookupQueryValidator()
    {
        RuleFor(q => q.GroupCodes).NotEmpty().WithMessage(LookupErrors.GroupCodesRequired);
        RuleFor(q => q.GroupCodes.Count).LessThanOrEqualTo(MasterDataConstants.MaxBatchGroupCodes);
        RuleFor(q => q.Scope).IsInEnum();
    }
}

public sealed class BatchLookupQueryHandler : IRequestHandler<BatchLookupQuery, Result<BatchLookupResponse>>
{
    private readonly ILookupService _service;
    public BatchLookupQueryHandler(ILookupService service) => _service = service;
    public async Task<Result<BatchLookupResponse>> Handle(BatchLookupQuery request, CancellationToken cancellationToken)
    {
        var result = await _service.GetBatchAsync(new LookupQuery(null, request.GroupCodes, request.Scope, request.TenantId, request.OrganizationId, request.IncludeInactive, request.EffectiveAt, request.IncludeMetadata), cancellationToken);
        return Result<BatchLookupResponse>.Success(result);
    }
}
