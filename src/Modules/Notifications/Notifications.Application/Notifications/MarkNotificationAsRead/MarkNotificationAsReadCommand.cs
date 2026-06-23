using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Notifications.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid Id) : ICommand;

public sealed class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
{
    public MarkNotificationAsReadCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Result>
{
    private readonly INotificationService _notificationService;

    public MarkNotificationAsReadCommandHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
