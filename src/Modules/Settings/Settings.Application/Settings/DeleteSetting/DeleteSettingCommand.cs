using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;

namespace Settings.Application.Settings.DeleteSetting;

public sealed record DeleteSettingCommand(Guid Id) : ICommand;

public sealed class DeleteSettingCommandValidator : AbstractValidator<DeleteSettingCommand>
{
    public DeleteSettingCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class DeleteSettingCommandHandler : IRequestHandler<DeleteSettingCommand, Result>
{
    private readonly ISettingService _settingService;

    public DeleteSettingCommandHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result> Handle(DeleteSettingCommand request, CancellationToken cancellationToken)
    {
        await _settingService.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
