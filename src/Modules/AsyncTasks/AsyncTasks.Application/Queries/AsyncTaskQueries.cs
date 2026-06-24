using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Dtos;
using AsyncTasks.Domain.Constants;
using BuildingBlocks.Application.CQRS;
using AsyncTasks.Domain.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;

namespace AsyncTasks.Application.Queries;

public sealed record GetAsyncTaskByIdQuery(Guid Id) : IQuery<AsyncTaskDto>;

public sealed class GetAsyncTaskByIdQueryHandler : IRequestHandler<GetAsyncTaskByIdQuery, Result<AsyncTaskDto>>
{
    private readonly IAsyncTaskService _service;

    public GetAsyncTaskByIdQueryHandler(IAsyncTaskService service) => _service = service;

    public async Task<Result<AsyncTaskDto>> Handle(GetAsyncTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await _service.GetByIdAsync(request.Id, cancellationToken);
        if (task is null)
        {
            throw new NotFoundException(AsyncTaskErrors.NotFound, $"Async task '{request.Id}' was not found.");
        }

        return Result<AsyncTaskDto>.Success(task);
    }
}

public sealed record GetAsyncTasksQuery(
    string? Keyword,
    string? TaskNo,
    string? Type,
    string? Status,
    Guid? RequestedByUserId,
    Guid? TenantId,
    Guid? OrganizationId,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<AsyncTaskDto>>;

public sealed class GetAsyncTasksQueryValidator : AbstractValidator<GetAsyncTasksQuery>
{
    public GetAsyncTasksQueryValidator()
    {
        RuleFor(q => q.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
        RuleFor(q => q.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || AsyncTaskStatuses.All.Contains(s))
            .WithMessage("Invalid status filter.");
        RuleFor(q => q.Type)
            .Must(t => string.IsNullOrWhiteSpace(t) || AsyncTaskTypes.All.Contains(t))
            .WithMessage("Invalid type filter.");
        RuleFor(q => q)
            .Must(q => !q.CreatedFrom.HasValue || !q.CreatedTo.HasValue || q.CreatedFrom <= q.CreatedTo)
            .WithMessage("CreatedFrom must be less than or equal to CreatedTo.");
    }
}

public sealed class GetAsyncTasksQueryHandler : IRequestHandler<GetAsyncTasksQuery, Result<PagedResult<AsyncTaskDto>>>
{
    private readonly IAsyncTaskService _service;

    public GetAsyncTasksQueryHandler(IAsyncTaskService service) => _service = service;

    public async Task<Result<PagedResult<AsyncTaskDto>>> Handle(GetAsyncTasksQuery request, CancellationToken cancellationToken)
    {
        var result = await _service.GetPagedAsync(new AsyncTaskListFilter
        {
            Keyword = request.Keyword,
            TaskNo = request.TaskNo,
            Type = request.Type,
            Status = request.Status,
            RequestedByUserId = request.RequestedByUserId,
            TenantId = request.TenantId,
            OrganizationId = request.OrganizationId,
            CreatedFrom = request.CreatedFrom,
            CreatedTo = request.CreatedTo,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        }, cancellationToken);

        return Result<PagedResult<AsyncTaskDto>>.Success(result);
    }
}
