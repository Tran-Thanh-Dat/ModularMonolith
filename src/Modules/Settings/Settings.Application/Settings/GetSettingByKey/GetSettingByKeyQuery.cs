using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.Settings;

namespace Settings.Application.Settings.GetSettingByKey;

public sealed record GetSettingByKeyQuery(string Key) : IQuery<SettingDetailResponse>;

public sealed class GetSettingByKeyQueryValidator : AbstractValidator<GetSettingByKeyQuery>
{
    public GetSettingByKeyQueryValidator()
    {
        RuleFor(query => query.Key)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public sealed class GetSettingByKeyQueryHandler
    : IRequestHandler<GetSettingByKeyQuery, Result<SettingDetailResponse>>
{
    private readonly ISettingService _settingService;
    private readonly BuildingBlocks.Application.Abstractions.ICurrentUserService _currentUserService;

    public GetSettingByKeyQueryHandler(
        ISettingService settingService,
        BuildingBlocks.Application.Abstractions.ICurrentUserService currentUserService)
    {
        _settingService = settingService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SettingDetailResponse>> Handle(
        GetSettingByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var setting = await _settingService.GetByKeyAsync(
            request.Key,
            SettingAuthorizationHelper.CanViewSensitive(_currentUserService),
            cancellationToken);

        if (setting is null)
        {
            throw new NotFoundException(
                SettingErrors.NotFound,
                $"Setting with key '{request.Key}' was not found.");
        }

        return Result<SettingDetailResponse>.Success(setting);
    }
}
