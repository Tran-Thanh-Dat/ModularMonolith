using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Notifications.ArchiveNotification;

public sealed record ArchiveNotificationCommand(Guid Id) : ICommand;

public sealed class ArchiveNotificationCommandValidator : AbstractValidator<ArchiveNotificationCommand>
{
    public ArchiveNotificationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class ArchiveNotificationCommandHandler : IRequestHandler<ArchiveNotificationCommand, Result>
{
    private readonly INotificationService _notificationService;

    public ArchiveNotificationCommandHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(ArchiveNotificationCommand request, CancellationToken cancellationToken)
    {
        await _notificationService.ArchiveAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
