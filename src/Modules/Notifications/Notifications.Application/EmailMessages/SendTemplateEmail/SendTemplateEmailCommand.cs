using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailMessages;
using Notifications.Application.Validation;

namespace Notifications.Application.EmailMessages.SendTemplateEmail;

public sealed record SendTemplateEmailCommand(
    string TemplateCode,
    string To,
    string? Cc,
    string? Bcc,
    Dictionary<string, string> TemplateData,
    string? ReferenceType,
    string? ReferenceId) : ICommand<SendEmailResponse>;

public sealed class SendTemplateEmailCommandValidator : AbstractValidator<SendTemplateEmailCommand>
{
    public SendTemplateEmailCommandValidator()
    {
        RuleFor(command => command.TemplateCode)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.To)
            .NotEmpty()
            .Must(EmailValidationRules.IsValidEmail)
            .WithMessage("A valid email address is required.");

        RuleFor(command => command.Cc)
            .Must(EmailValidationRules.IsValidEmailList!)
            .When(command => !string.IsNullOrWhiteSpace(command.Cc))
            .WithMessage("Cc must contain valid email addresses.");

        RuleFor(command => command.Bcc)
            .Must(EmailValidationRules.IsValidEmailList!)
            .When(command => !string.IsNullOrWhiteSpace(command.Bcc))
            .WithMessage("Bcc must contain valid email addresses.");

        RuleFor(command => command.TemplateData)
            .NotNull();
    }
}

public sealed class SendTemplateEmailCommandHandler
    : IRequestHandler<SendTemplateEmailCommand, Result<SendEmailResponse>>
{
    private readonly IEmailService _emailService;

    public SendTemplateEmailCommandHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Result<SendEmailResponse>> Handle(
        SendTemplateEmailCommand request,
        CancellationToken cancellationToken)
    {
        var response = await _emailService.SendTemplateEmailAsync(
            request.TemplateCode,
            request.To,
            request.Cc,
            request.Bcc,
            request.TemplateData,
            request.ReferenceType,
            request.ReferenceId,
            cancellationToken);

        return Result<SendEmailResponse>.Success(response);
    }
}
