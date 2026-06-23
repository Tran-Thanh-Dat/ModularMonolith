using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailMessages;
using Notifications.Application.Validation;

namespace Notifications.Application.EmailMessages.SendTestEmail;

public sealed record SendTestEmailCommand(
    string To,
    string Subject,
    string Body) : ICommand<SendEmailResponse>;

public sealed class SendTestEmailCommandValidator : AbstractValidator<SendTestEmailCommand>
{
    public SendTestEmailCommandValidator()
    {
        RuleFor(command => command.To)
            .NotEmpty()
            .Must(EmailValidationRules.IsValidEmail)
            .WithMessage("A valid email address is required.");

        RuleFor(command => command.Subject)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(command => command.Body)
            .NotEmpty();
    }
}

public sealed class SendTestEmailCommandHandler
    : IRequestHandler<SendTestEmailCommand, Result<SendEmailResponse>>
{
    private readonly IEmailService _emailService;

    public SendTestEmailCommandHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Result<SendEmailResponse>> Handle(
        SendTestEmailCommand request,
        CancellationToken cancellationToken)
    {
        var response = await _emailService.SendEmailAsync(
            request.To,
            null,
            null,
            request.Subject,
            request.Body,
            isHtml: true,
            referenceType: "TestEmail",
            referenceId: null,
            cancellationToken);

        return Result<SendEmailResponse>.Success(response);
    }
}
