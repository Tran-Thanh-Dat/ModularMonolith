using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;

namespace Settings.Application.Settings.UpdateSettingValue;

public sealed record UpdateSettingValueCommand(Guid Id, string? Value) : ICommand;

public sealed class UpdateSettingValueCommandValidator : AbstractValidator<UpdateSettingValueCommand>
{
    public UpdateSettingValueCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class UpdateSettingValueCommandHandler : IRequestHandler<UpdateSettingValueCommand, Result>
{
    private readonly ISettingService _settingService;

    public UpdateSettingValueCommandHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result> Handle(UpdateSettingValueCommand request, CancellationToken cancellationToken)
    {
        await _settingService.UpdateValueAsync(request.Id, request.Value, cancellationToken);
        return Result.Success();
    }
}
