using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.Settings;

namespace Settings.Application.Settings.GetSettings;

public sealed record GetSettingsQuery(
    string? Keyword,
    string? Group,
    string? DataType,
    bool? IsActive,
    bool? IsSystem,
    bool? IsEditable,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<SettingListItemResponse>>;

public sealed class GetSettingsQueryValidator : AbstractValidator<GetSettingsQuery>
{
    public GetSettingsQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Keyword).MaximumLength(200);
        RuleFor(query => query.Group).MaximumLength(100);
        RuleFor(query => query.DataType).MaximumLength(50);
    }
}

public sealed class GetSettingsQueryHandler
    : IRequestHandler<GetSettingsQuery, Result<PagedResult<SettingListItemResponse>>>
{
    private readonly ISettingService _settingService;

    public GetSettingsQueryHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result<PagedResult<SettingListItemResponse>>> Handle(
        GetSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _settingService.GetSettingsAsync(
            request.Keyword,
            request.Group,
            request.DataType,
            request.IsActive,
            request.IsSystem,
            request.IsEditable,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<SettingListItemResponse>>.Success(result);
    }
}
