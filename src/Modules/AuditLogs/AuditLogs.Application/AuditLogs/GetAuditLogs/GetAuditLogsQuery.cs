using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;

namespace AuditLogs.Application.AuditLogs.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    string? Keyword,
    string? ModuleName,
    string? Action,
    Guid? UserId,
    string? UserName,
    string? EntityName,
    string? EntityId,
    string? Status,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<AuditLogListItemResponse>>;

public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query)
            .Must(query => !query.FromDate.HasValue || !query.ToDate.HasValue || query.FromDate <= query.ToDate)
            .WithMessage("FromDate must be less than or equal to ToDate.");
    }
}

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogListItemResponse>>>
{
    private readonly IAuditLogReadService _auditLogReadService;

    public GetAuditLogsQueryHandler(IAuditLogReadService auditLogReadService)
    {
        _auditLogReadService = auditLogReadService;
    }

    public async Task<Result<PagedResult<AuditLogListItemResponse>>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogReadService.GetAuditLogsAsync(
            request.Keyword,
            request.ModuleName,
            request.Action,
            request.UserId,
            request.UserName,
            request.EntityName,
            request.EntityId,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<AuditLogListItemResponse>>.Success(result);
    }
}
