using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.Settings;

namespace Settings.Application.Settings.GetSettingsByGroup;

public sealed record GetSettingsByGroupQuery(string Group) : IQuery<IReadOnlyCollection<SettingListItemResponse>>;

public sealed class GetSettingsByGroupQueryValidator : AbstractValidator<GetSettingsByGroupQuery>
{
    public GetSettingsByGroupQueryValidator()
    {
        RuleFor(query => query.Group)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public sealed class GetSettingsByGroupQueryHandler
    : IRequestHandler<GetSettingsByGroupQuery, Result<IReadOnlyCollection<SettingListItemResponse>>>
{
    private readonly ISettingService _settingService;

    public GetSettingsByGroupQueryHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result<IReadOnlyCollection<SettingListItemResponse>>> Handle(
        GetSettingsByGroupQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _settingService.GetByGroupAsync(request.Group, cancellationToken);
        return Result<IReadOnlyCollection<SettingListItemResponse>>.Success(settings);
    }
}
