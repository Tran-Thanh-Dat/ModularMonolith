using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;

namespace Settings.Application.Settings.DeactivateSetting;

public sealed record DeactivateSettingCommand(Guid Id) : ICommand;

public sealed class DeactivateSettingCommandValidator : AbstractValidator<DeactivateSettingCommand>
{
    public DeactivateSettingCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class DeactivateSettingCommandHandler : IRequestHandler<DeactivateSettingCommand, Result>
{
    private readonly ISettingService _settingService;

    public DeactivateSettingCommandHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result> Handle(DeactivateSettingCommand request, CancellationToken cancellationToken)
    {
        await _settingService.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
