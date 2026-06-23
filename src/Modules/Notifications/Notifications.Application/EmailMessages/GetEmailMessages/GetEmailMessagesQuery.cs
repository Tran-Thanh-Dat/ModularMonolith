using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailMessages;
using Notifications.Application.Validation;

namespace Notifications.Application.EmailMessages.GetEmailMessages;

public sealed record GetEmailMessagesQuery(
    string? Keyword,
    string? To,
    string? Status,
    string? Provider,
    string? TemplateCode,
    string? ReferenceType,
    string? ReferenceId,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<EmailMessageListItemResponse>>;

public sealed class GetEmailMessagesQueryValidator : AbstractValidator<GetEmailMessagesQuery>
{
    public GetEmailMessagesQueryValidator()
    {
        RuleFor(query => query.PageIndex).ValidPageIndex();
        RuleFor(query => query.PageSize).ValidPageSize();
    }
}

public sealed class GetEmailMessagesQueryHandler
    : IRequestHandler<GetEmailMessagesQuery, Result<PagedResult<EmailMessageListItemResponse>>>
{
    private readonly IEmailService _emailService;

    public GetEmailMessagesQueryHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Result<PagedResult<EmailMessageListItemResponse>>> Handle(
        GetEmailMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _emailService.GetEmailMessagesAsync(
            request.Keyword,
            request.To,
            request.Status,
            request.Provider,
            request.TemplateCode,
            request.ReferenceType,
            request.ReferenceId,
            request.FromDate,
            request.ToDate,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<EmailMessageListItemResponse>>.Success(result);
    }
}
