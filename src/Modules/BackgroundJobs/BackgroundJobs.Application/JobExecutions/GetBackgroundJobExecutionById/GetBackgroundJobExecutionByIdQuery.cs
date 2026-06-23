using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using BackgroundJobs.Application.Abstractions;
using BackgroundJobs.Application.JobExecutions;
using FluentValidation;
using MediatR;

namespace BackgroundJobs.Application.JobExecutions.GetBackgroundJobExecutionById;

public sealed record GetBackgroundJobExecutionByIdQuery(Guid Id) : IQuery<BackgroundJobExecutionDetailResponse>;

public sealed class GetBackgroundJobExecutionByIdQueryValidator : AbstractValidator<GetBackgroundJobExecutionByIdQuery>
{
    public GetBackgroundJobExecutionByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class GetBackgroundJobExecutionByIdQueryHandler
    : IRequestHandler<GetBackgroundJobExecutionByIdQuery, Result<BackgroundJobExecutionDetailResponse>>
{
    private readonly IBackgroundJobExecutionService _executionService;

    public GetBackgroundJobExecutionByIdQueryHandler(IBackgroundJobExecutionService executionService)
    {
        _executionService = executionService;
    }

    public async Task<Result<BackgroundJobExecutionDetailResponse>> Handle(
        GetBackgroundJobExecutionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var execution = await _executionService.GetByIdAsync(request.Id, cancellationToken);

        if (execution is null)
        {
            throw new NotFoundException(
                BackgroundJobErrors.NotFound,
                $"Background job execution with id '{request.Id}' was not found.");
        }

        return Result<BackgroundJobExecutionDetailResponse>.Success(execution);
    }
}
