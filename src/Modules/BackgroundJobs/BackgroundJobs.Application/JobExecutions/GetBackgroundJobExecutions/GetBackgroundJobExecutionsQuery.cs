using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using BackgroundJobs.Application.Abstractions;
using BackgroundJobs.Application.JobExecutions;
using FluentValidation;
using MediatR;

namespace BackgroundJobs.Application.JobExecutions.GetBackgroundJobExecutions;

public sealed record GetBackgroundJobExecutionsQuery(
    string? Keyword,
    string? JobName,
    string? JobType,
    string? Status,
    string? TriggerSource,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<BackgroundJobExecutionListItemResponse>>;

public sealed class GetBackgroundJobExecutionsQueryValidator : AbstractValidator<GetBackgroundJobExecutionsQuery>
{
    public GetBackgroundJobExecutionsQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query)
            .Must(query => !query.FromDate.HasValue || !query.ToDate.HasValue || query.FromDate <= query.ToDate)
            .WithMessage("FromDate must be less than or equal to ToDate.");
    }
}

public sealed class GetBackgroundJobExecutionsQueryHandler
    : IRequestHandler<GetBackgroundJobExecutionsQuery, Result<PagedResult<BackgroundJobExecutionListItemResponse>>>
{
    private readonly IBackgroundJobExecutionService _executionService;

    public GetBackgroundJobExecutionsQueryHandler(IBackgroundJobExecutionService executionService)
    {
        _executionService = executionService;
    }

    public async Task<Result<PagedResult<BackgroundJobExecutionListItemResponse>>> Handle(
        GetBackgroundJobExecutionsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _executionService.GetListAsync(
            request.Keyword,
            request.JobName,
            request.JobType,
            request.Status,
            request.TriggerSource,
            request.FromDate,
            request.ToDate,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<BackgroundJobExecutionListItemResponse>>.Success(result);
    }
}
