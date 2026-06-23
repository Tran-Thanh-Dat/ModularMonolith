using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.Settings;

namespace Settings.Application.Settings.GetSettingById;

public sealed record GetSettingByIdQuery(Guid Id) : IQuery<SettingDetailResponse>;

public sealed class GetSettingByIdQueryValidator : AbstractValidator<GetSettingByIdQuery>
{
    public GetSettingByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class GetSettingByIdQueryHandler
    : IRequestHandler<GetSettingByIdQuery, Result<SettingDetailResponse>>
{
    private readonly ISettingService _settingService;
    private readonly BuildingBlocks.Application.Abstractions.ICurrentUserService _currentUserService;

    public GetSettingByIdQueryHandler(
        ISettingService settingService,
        BuildingBlocks.Application.Abstractions.ICurrentUserService currentUserService)
    {
        _settingService = settingService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SettingDetailResponse>> Handle(
        GetSettingByIdQuery request,
        CancellationToken cancellationToken)
    {
        var setting = await _settingService.GetByIdAsync(
            request.Id,
            SettingAuthorizationHelper.CanViewSensitive(_currentUserService),
            cancellationToken);

        if (setting is null)
        {
            throw new NotFoundException(
                SettingErrors.NotFound,
                $"Setting with id '{request.Id}' was not found.");
        }

        return Result<SettingDetailResponse>.Success(setting);
    }
}
