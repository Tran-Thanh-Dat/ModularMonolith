using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using MediatR;

namespace AuditLogs.Application.AuditLogs.GetAuditLogById;

public sealed record GetAuditLogByIdQuery(Guid Id) : IQuery<AuditLogDetailResponse>;

public sealed class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, Result<AuditLogDetailResponse>>
{
    private readonly IAuditLogReadService _auditLogReadService;

    public GetAuditLogByIdQueryHandler(IAuditLogReadService auditLogReadService)
    {
        _auditLogReadService = auditLogReadService;
    }

    public async Task<Result<AuditLogDetailResponse>> Handle(
        GetAuditLogByIdQuery request,
        CancellationToken cancellationToken)
    {
        var auditLog = await _auditLogReadService.GetAuditLogByIdAsync(request.Id, cancellationToken);

        if (auditLog is null)
        {
            throw new NotFoundException(
                CommonErrors.NotFound,
                $"Audit log '{request.Id}' was not found.");
        }

        return Result<AuditLogDetailResponse>.Success(auditLog);
    }
}
