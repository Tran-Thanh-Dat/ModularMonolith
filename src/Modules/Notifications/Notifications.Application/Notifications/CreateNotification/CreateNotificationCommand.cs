using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.Notifications;
using Notifications.Domain.Constants;

namespace Notifications.Application.Notifications.CreateNotification;

public sealed record CreateNotificationCommand(
    Guid UserId,
    string Title,
    string Message,
    string Type,
    string? ReferenceType,
    string? ReferenceId,
    string? Metadata) : ICommand<CreateNotificationResponse>;

public sealed class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    private static readonly string[] AllowedTypes =
    [
        NotificationTypes.Info,
        NotificationTypes.Success,
        NotificationTypes.Warning,
        NotificationTypes.Error,
        NotificationTypes.System
    ];

    public CreateNotificationCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(command => command.Type)
            .NotEmpty()
            .Must(type => AllowedTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Notification type is invalid.");
    }
}

public sealed class CreateNotificationCommandHandler
    : IRequestHandler<CreateNotificationCommand, Result<CreateNotificationResponse>>
{
    private readonly INotificationService _notificationService;

    public CreateNotificationCommandHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<Result<CreateNotificationResponse>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var id = await _notificationService.CreateAsync(
            request.UserId,
            request.Title,
            request.Message,
            request.Type,
            request.ReferenceType,
            request.ReferenceId,
            request.Metadata,
            cancellationToken);

        return Result<CreateNotificationResponse>.Success(new CreateNotificationResponse { Id = id });
    }
}
