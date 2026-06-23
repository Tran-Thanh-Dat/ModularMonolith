using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailTemplates;

namespace Notifications.Application.EmailTemplates.CreateEmailTemplate;

public sealed record CreateEmailTemplateCommand(
    string Code,
    string Name,
    string Subject,
    string Body,
    bool IsHtml,
    string? Description) : ICommand<CreateEmailTemplateResponse>;

public sealed class CreateEmailTemplateCommandValidator : AbstractValidator<CreateEmailTemplateCommand>
{
    public CreateEmailTemplateCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(100);

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

public sealed class CreateEmailTemplateCommandHandler
    : IRequestHandler<CreateEmailTemplateCommand, Result<CreateEmailTemplateResponse>>
{
    private readonly IEmailTemplateService _emailTemplateService;

    public CreateEmailTemplateCommandHandler(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    public async Task<Result<CreateEmailTemplateResponse>> Handle(
        CreateEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var id = await _emailTemplateService.CreateAsync(
            request.Code,
            request.Name,
            request.Subject,
            request.Body,
            request.IsHtml,
            request.Description,
            cancellationToken);

        return Result<CreateEmailTemplateResponse>.Success(new CreateEmailTemplateResponse { Id = id });
    }
}
