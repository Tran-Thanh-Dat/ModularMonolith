using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.Settings;
using Settings.Domain.Constants;

namespace Settings.Application.Settings.CreateSetting;

public sealed record CreateSettingCommand(
    string Key,
    string Group,
    string Name,
    string? Description,
    string? Value,
    string? DefaultValue,
    string DataType,
    bool IsEncrypted,
    bool IsSensitive,
    bool IsSystem,
    bool IsEditable,
    int SortOrder) : ICommand<CreateSettingResponse>;

public sealed class CreateSettingCommandValidator : AbstractValidator<CreateSettingCommand>
{
    public CreateSettingCommandValidator()
    {
        RuleFor(command => command.Key).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Group).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(1000);
        RuleFor(command => command.DataType)
            .NotEmpty()
            .MaximumLength(50)
            .Must(dataType => SettingDataTypes.All.Contains(dataType))
            .WithMessage("Data type is not supported.");
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateSettingCommandHandler
    : IRequestHandler<CreateSettingCommand, Result<CreateSettingResponse>>
{
    private readonly ISettingService _settingService;

    public CreateSettingCommandHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result<CreateSettingResponse>> Handle(
        CreateSettingCommand request,
        CancellationToken cancellationToken)
    {
        var id = await _settingService.CreateAsync(
            new CreateSettingRequest
            {
                Key = request.Key,
                Group = request.Group,
                Name = request.Name,
                Description = request.Description,
                Value = request.Value,
                DefaultValue = request.DefaultValue,
                DataType = request.DataType,
                IsEncrypted = request.IsEncrypted,
                IsSensitive = request.IsSensitive,
                IsSystem = request.IsSystem,
                IsEditable = request.IsEditable,
                SortOrder = request.SortOrder
            },
            cancellationToken);

        return Result<CreateSettingResponse>.Success(new CreateSettingResponse { Id = id });
    }
}
