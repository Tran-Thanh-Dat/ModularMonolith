using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;

namespace Notifications.Application.EmailTemplates.UpdateEmailTemplate;

public sealed record UpdateEmailTemplateCommand(
    Guid Id,
    string Name,
    string Subject,
    string Body,
    bool IsHtml,
    string? Description) : ICommand;

public sealed class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    public UpdateEmailTemplateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Subject)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(command => command.Body)
            .NotEmpty();
    }
}

public sealed class UpdateEmailTemplateCommandHandler : IRequestHandler<UpdateEmailTemplateCommand, Result>
{
    private readonly IEmailTemplateService _emailTemplateService;

    public UpdateEmailTemplateCommandHandler(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    public async Task<Result> Handle(UpdateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        await _emailTemplateService.UpdateAsync(
            request.Id,
            request.Name,
            request.Subject,
            request.Body,
            request.IsHtml,
            request.Description,
            cancellationToken);

        return Result.Success();
    }
}
