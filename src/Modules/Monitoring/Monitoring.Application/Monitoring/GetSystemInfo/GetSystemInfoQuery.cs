using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using MediatR;
using Monitoring.Application.Abstractions;
using Monitoring.Application.Monitoring;

namespace Monitoring.Application.Monitoring.GetSystemInfo;

public sealed record GetSystemInfoQuery : IQuery<SystemInfoResponse>;

public sealed class GetSystemInfoQueryHandler : IRequestHandler<GetSystemInfoQuery, Result<SystemInfoResponse>>
{
    private readonly ISystemInfoProvider _systemInfoProvider;

    public GetSystemInfoQueryHandler(ISystemInfoProvider systemInfoProvider)
    {
        _systemInfoProvider = systemInfoProvider;
    }

    public async Task<Result<SystemInfoResponse>> Handle(
        GetSystemInfoQuery request,
        CancellationToken cancellationToken)
    {
        var info = await _systemInfoProvider.GetAsync(cancellationToken);
        return Result<SystemInfoResponse>.Success(info);
    }
}
