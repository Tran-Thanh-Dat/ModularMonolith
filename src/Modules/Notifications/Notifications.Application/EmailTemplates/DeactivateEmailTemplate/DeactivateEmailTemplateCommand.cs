using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;

namespace Notifications.Application.EmailTemplates.DeactivateEmailTemplate;

public sealed record DeactivateEmailTemplateCommand(Guid Id) : ICommand;

public sealed class DeactivateEmailTemplateCommandValidator : AbstractValidator<DeactivateEmailTemplateCommand>
{
    public DeactivateEmailTemplateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class DeactivateEmailTemplateCommandHandler : IRequestHandler<DeactivateEmailTemplateCommand, Result>
{
    private readonly IEmailTemplateService _emailTemplateService;

    public DeactivateEmailTemplateCommandHandler(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    public async Task<Result> Handle(DeactivateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        await _emailTemplateService.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
