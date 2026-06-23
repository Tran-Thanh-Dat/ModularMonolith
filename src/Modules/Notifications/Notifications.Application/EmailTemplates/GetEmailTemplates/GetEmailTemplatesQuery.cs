using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailTemplates;
using Notifications.Application.Validation;

namespace Notifications.Application.EmailTemplates.GetEmailTemplates;

public sealed record GetEmailTemplatesQuery(
    string? Keyword,
    bool? IsActive,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<EmailTemplateListItemResponse>>;

public sealed class GetEmailTemplatesQueryValidator : AbstractValidator<GetEmailTemplatesQuery>
{
    public GetEmailTemplatesQueryValidator()
    {
        RuleFor(query => query.PageIndex).ValidPageIndex();
        RuleFor(query => query.PageSize).ValidPageSize();
    }
}

public sealed class GetEmailTemplatesQueryHandler
    : IRequestHandler<GetEmailTemplatesQuery, Result<PagedResult<EmailTemplateListItemResponse>>>
{
    private readonly IEmailTemplateService _emailTemplateService;

    public GetEmailTemplatesQueryHandler(IEmailTemplateService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    public async Task<Result<PagedResult<EmailTemplateListItemResponse>>> Handle(
        GetEmailTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _emailTemplateService.GetListAsync(
            request.Keyword,
            request.IsActive,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<EmailTemplateListItemResponse>>.Success(result);
    }
}
