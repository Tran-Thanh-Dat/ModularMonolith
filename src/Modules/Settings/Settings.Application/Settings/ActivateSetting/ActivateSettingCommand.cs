using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;

namespace Settings.Application.Settings.ActivateSetting;

public sealed record ActivateSettingCommand(Guid Id) : ICommand;

public sealed class ActivateSettingCommandValidator : AbstractValidator<ActivateSettingCommand>
{
    public ActivateSettingCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class ActivateSettingCommandHandler : IRequestHandler<ActivateSettingCommand, Result>
{
    private readonly ISettingService _settingService;

    public ActivateSettingCommandHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result> Handle(ActivateSettingCommand request, CancellationToken cancellationToken)
    {
        await _settingService.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
