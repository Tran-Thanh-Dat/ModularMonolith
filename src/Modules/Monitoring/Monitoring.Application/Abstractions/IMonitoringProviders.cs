using Monitoring.Application.Monitoring;

namespace Monitoring.Application.Abstractions;

public interface IHealthDetailsProvider
{
    Task<HealthDetailsResponse> GetAsync(CancellationToken cancellationToken = default);
}

public interface ISystemInfoProvider
{
    Task<SystemInfoResponse> GetAsync(CancellationToken cancellationToken = default);
}
