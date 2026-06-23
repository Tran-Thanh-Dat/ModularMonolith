using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailMessages;
using Notifications.Application.Validation;

namespace Notifications.Application.EmailMessages.SendEmail;

public sealed record SendEmailCommand(
    string To,
    string? Cc,
    string? Bcc,
    string Subject,
    string Body,
    bool IsHtml,
    string? ReferenceType,
    string? ReferenceId) : ICommand<SendEmailResponse>;

public sealed class SendEmailCommandValidator : AbstractValidator<SendEmailCommand>
{
    public SendEmailCommandValidator()
    {
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

        RuleFor(command => command.Subject)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(command => command.Body)
            .NotEmpty();
    }
}

public sealed class SendEmailCommandHandler
    : IRequestHandler<SendEmailCommand, Result<SendEmailResponse>>
{
    private readonly IEmailService _emailService;

    public SendEmailCommandHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Result<SendEmailResponse>> Handle(
        SendEmailCommand request,
        CancellationToken cancellationToken)
    {
        var response = await _emailService.SendEmailAsync(
            request.To,
            request.Cc,
            request.Bcc,
            request.Subject,
            request.Body,
            request.IsHtml,
            request.ReferenceType,
            request.ReferenceId,
            cancellationToken);

        return Result<SendEmailResponse>.Success(response);
    }
}
