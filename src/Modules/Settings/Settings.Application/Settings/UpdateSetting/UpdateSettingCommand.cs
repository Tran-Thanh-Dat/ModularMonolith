using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.Settings;

namespace Settings.Application.Settings.UpdateSetting;

public sealed record UpdateSettingCommand(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    bool IsEditable) : ICommand;

public sealed class UpdateSettingCommandValidator : AbstractValidator<UpdateSettingCommand>
{
    public UpdateSettingCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(1000);
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSettingCommandHandler : IRequestHandler<UpdateSettingCommand, Result>
{
    private readonly ISettingService _settingService;

    public UpdateSettingCommandHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
    {
        await _settingService.UpdateAsync(
            request.Id,
            new UpdateSettingRequest
            {
                Name = request.Name,
                Description = request.Description,
                SortOrder = request.SortOrder,
                IsEditable = request.IsEditable
            },
            cancellationToken);

        return Result.Success();
    }
}
