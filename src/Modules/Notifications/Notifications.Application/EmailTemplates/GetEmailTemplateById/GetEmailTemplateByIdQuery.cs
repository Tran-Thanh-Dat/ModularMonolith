using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailTemplates;

namespace Notifications.Application.EmailTemplates.GetEmailTemplateById;

public sealed record GetEmailTemplateByIdQuery(Guid Id) : IQuery<EmailTemplateDetailResponse>;

public sealed class GetEmailTemplateByIdQueryValidator : AbstractValidator<GetEmailTemplateByIdQuery>
{
    public GetEmailTemplateByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class GetEmailTemplateByIdQueryHandler
    : IRequestHandler<GetEmailTemplateByIdQuery, Result<EmailTemplateDetailResponse>>
{
    private readonly IEmailTemplateService _emailTemplateService;

    public GetEmailTemplateByIdQueryHandler(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    public async Task<Result<EmailTemplateDetailResponse>> Handle(
        GetEmailTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _emailTemplateService.GetByIdAsync(request.Id, cancellationToken);

        if (template is null)
        {
            throw new NotFoundException(
                EmailErrors.TemplateNotFound,
                $"Email template with id '{request.Id}' was not found.");
        }

        return Result<EmailTemplateDetailResponse>.Success(template);
    }
}
