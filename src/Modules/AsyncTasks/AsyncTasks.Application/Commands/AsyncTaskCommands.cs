using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Dtos;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using MediatR;

namespace AsyncTasks.Application.Commands;

public sealed record SubmitEmailDemoTaskCommand(
    string? EmailTo,
    string? Subject,
    string? Body,
    Guid? TenantId,
    Guid? OrganizationId) : ICommand<AsyncTaskDto>;

public sealed class SubmitEmailDemoTaskCommandHandler : IRequestHandler<SubmitEmailDemoTaskCommand, Result<AsyncTaskDto>>
{
    private readonly IAsyncTaskService _service;

    public SubmitEmailDemoTaskCommandHandler(IAsyncTaskService service) => _service = service;

    public async Task<Result<AsyncTaskDto>> Handle(SubmitEmailDemoTaskCommand request, CancellationToken cancellationToken)
    {
        var dto = await _service.SubmitEmailDemoAsync(
            request.EmailTo, request.Subject, request.Body, request.TenantId, request.OrganizationId, cancellationToken);
        return Result<AsyncTaskDto>.Success(dto);
    }
}

public sealed record SubmitFileProcessingDemoTaskCommand(
    Guid? FileId,
    string? FileName,
    Guid? TenantId,
    Guid? OrganizationId) : ICommand<AsyncTaskDto>;

public sealed class SubmitFileProcessingDemoTaskCommandHandler : IRequestHandler<SubmitFileProcessingDemoTaskCommand, Result<AsyncTaskDto>>
{
    private readonly IAsyncTaskService _service;

    public SubmitFileProcessingDemoTaskCommandHandler(IAsyncTaskService service) => _service = service;

    public async Task<Result<AsyncTaskDto>> Handle(SubmitFileProcessingDemoTaskCommand request, CancellationToken cancellationToken)
    {
        var dto = await _service.SubmitFileProcessingDemoAsync(
            request.FileId, request.FileName, request.TenantId, request.OrganizationId, cancellationToken);
        return Result<AsyncTaskDto>.Success(dto);
    }
}

public sealed record SubmitFailDemoTaskCommand(
    string? FailReason,
    bool ShouldAlwaysFail,
    Guid? TenantId,
    Guid? OrganizationId) : ICommand<AsyncTaskDto>;

public sealed class SubmitFailDemoTaskCommandHandler : IRequestHandler<SubmitFailDemoTaskCommand, Result<AsyncTaskDto>>
{
    private readonly IAsyncTaskService _service;

    public SubmitFailDemoTaskCommandHandler(IAsyncTaskService service) => _service = service;

    public async Task<Result<AsyncTaskDto>> Handle(SubmitFailDemoTaskCommand request, CancellationToken cancellationToken)
    {
        var dto = await _service.SubmitFailDemoAsync(
            request.FailReason, request.ShouldAlwaysFail, request.TenantId, request.OrganizationId, cancellationToken);
        return Result<AsyncTaskDto>.Success(dto);
    }
}

public sealed record SubmitLongRunningDemoTaskCommand(
    int DurationSeconds,
    int Steps,
    Guid? TenantId,
    Guid? OrganizationId) : ICommand<AsyncTaskDto>;

public sealed class SubmitLongRunningDemoTaskCommandHandler : IRequestHandler<SubmitLongRunningDemoTaskCommand, Result<AsyncTaskDto>>
{
    private readonly IAsyncTaskService _service;

    public SubmitLongRunningDemoTaskCommandHandler(IAsyncTaskService service) => _service = service;

    public async Task<Result<AsyncTaskDto>> Handle(SubmitLongRunningDemoTaskCommand request, CancellationToken cancellationToken)
    {
        var dto = await _service.SubmitLongRunningDemoAsync(
            request.DurationSeconds, request.Steps, request.TenantId, request.OrganizationId, cancellationToken);
        return Result<AsyncTaskDto>.Success(dto);
    }
}

public sealed record CancelAsyncTaskCommand(Guid Id) : ICommand;

public sealed class CancelAsyncTaskCommandHandler : IRequestHandler<CancelAsyncTaskCommand, Result>
{
    private readonly IAsyncTaskService _service;

    public CancelAsyncTaskCommandHandler(IAsyncTaskService service) => _service = service;

    public async Task<Result> Handle(CancelAsyncTaskCommand request, CancellationToken cancellationToken)
    {
        await _service.CancelAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record RetryAsyncTaskCommand(Guid Id) : ICommand<AsyncTaskDto>;

public sealed class RetryAsyncTaskCommandHandler : IRequestHandler<RetryAsyncTaskCommand, Result<AsyncTaskDto>>
{
    private readonly IAsyncTaskService _service;

    public RetryAsyncTaskCommandHandler(IAsyncTaskService service) => _service = service;

    public async Task<Result<AsyncTaskDto>> Handle(RetryAsyncTaskCommand request, CancellationToken cancellationToken)
    {
        var dto = await _service.RetryAsync(request.Id, cancellationToken);
        return Result<AsyncTaskDto>.Success(dto);
    }
}
