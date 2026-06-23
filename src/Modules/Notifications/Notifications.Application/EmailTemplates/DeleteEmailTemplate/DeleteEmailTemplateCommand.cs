using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;

namespace Notifications.Application.EmailTemplates.DeleteEmailTemplate;

public sealed record DeleteEmailTemplateCommand(Guid Id) : ICommand;

public sealed class DeleteEmailTemplateCommandValidator : AbstractValidator<DeleteEmailTemplateCommand>
{
    public DeleteEmailTemplateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class DeleteEmailTemplateCommandHandler : IRequestHandler<DeleteEmailTemplateCommand, Result>
{
    private readonly IEmailTemplateService _emailTemplateService;

    public DeleteEmailTemplateCommandHandler(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    public async Task<Result> Handle(DeleteEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        await _emailTemplateService.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
