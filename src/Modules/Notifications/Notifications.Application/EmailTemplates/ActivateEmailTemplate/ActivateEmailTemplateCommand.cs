using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;

namespace Notifications.Application.EmailTemplates.ActivateEmailTemplate;

public sealed record ActivateEmailTemplateCommand(Guid Id) : ICommand;

public sealed class ActivateEmailTemplateCommandValidator : AbstractValidator<ActivateEmailTemplateCommand>
{
    public ActivateEmailTemplateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class ActivateEmailTemplateCommandHandler : IRequestHandler<ActivateEmailTemplateCommand, Result>
{
    private readonly IEmailTemplateService _emailTemplateService;

    public ActivateEmailTemplateCommandHandler(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    public async Task<Result> Handle(ActivateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        await _emailTemplateService.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
