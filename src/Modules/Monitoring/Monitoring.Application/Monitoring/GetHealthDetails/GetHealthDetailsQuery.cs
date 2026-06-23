using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Monitoring;
using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.Extensions.Options;
using Monitoring.Application.Abstractions;
using Monitoring.Application.Monitoring;

namespace Monitoring.Application.Monitoring.GetHealthDetails;

public sealed record GetHealthDetailsQuery : IQuery<HealthDetailsResponse>;

public sealed class GetHealthDetailsQueryHandler : IRequestHandler<GetHealthDetailsQuery, Result<HealthDetailsResponse>>
{
    private readonly IHealthDetailsProvider _healthDetailsProvider;
    private readonly HealthChecksOptions _healthChecksOptions;

    public GetHealthDetailsQueryHandler(
        IHealthDetailsProvider healthDetailsProvider,
        IOptions<HealthChecksOptions> healthChecksOptions)
    {
        _healthDetailsProvider = healthDetailsProvider;
        _healthChecksOptions = healthChecksOptions.Value;
    }

    public async Task<Result<HealthDetailsResponse>> Handle(
        GetHealthDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_healthChecksOptions.Enabled || !_healthChecksOptions.DetailsEnabled)
        {
            return Result<HealthDetailsResponse>.Failure(
                MonitoringErrors.InvalidConfiguration,
                "Detailed health information is disabled by configuration.");
        }

        var details = await _healthDetailsProvider.GetAsync(cancellationToken);
        return Result<HealthDetailsResponse>.Success(details);
    }
}