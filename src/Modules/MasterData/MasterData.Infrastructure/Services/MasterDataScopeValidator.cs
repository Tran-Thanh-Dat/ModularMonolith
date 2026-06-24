using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using MasterData.Domain.Enums;
using MasterData.Domain.Errors;
using Organizations.Application.Abstractions;

namespace MasterData.Infrastructure.Services;

public sealed class MasterDataScopeValidator
{
    private readonly ITenantService _tenantService;
    private readonly IOrganizationService _organizationService;

    public MasterDataScopeValidator(ITenantService tenantService, IOrganizationService organizationService)
    {
        _tenantService = tenantService;
        _organizationService = organizationService;
    }

    public async Task ValidateScopeAsync(
        MasterDataScope scope,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        switch (scope)
        {
            case MasterDataScope.Global:
                if (tenantId.HasValue || organizationId.HasValue)
                {
                    throw new BadRequestException(
                        MasterDataGroupErrors.InvalidScope,
                        "Global scope requires null tenant and organization.");
                }

                break;

            case MasterDataScope.Tenant:
                if (!tenantId.HasValue || organizationId.HasValue)
                {
                    throw new BadRequestException(
                        MasterDataGroupErrors.InvalidScope,
                        "Tenant scope requires tenantId and null organizationId.");
                }

                await EnsureTenantActiveAsync(tenantId.Value, cancellationToken);
                break;

            case MasterDataScope.Organization:
                if (!tenantId.HasValue || !organizationId.HasValue)
                {
                    throw new BadRequestException(
                        MasterDataGroupErrors.InvalidScope,
                        "Organization scope requires tenantId and organizationId.");
                }

                await EnsureTenantActiveAsync(tenantId.Value, cancellationToken);
                await EnsureOrganizationActiveAsync(tenantId.Value, organizationId.Value, cancellationToken);
                break;

            default:
                throw new BadRequestException(MasterDataGroupErrors.InvalidScope, "Invalid master data scope.");
        }
    }

    private async Task EnsureTenantActiveAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new BadRequestException(
                MasterDataGroupErrors.InvalidTenant,
                $"Tenant with id '{tenantId}' was not found.");
        }

        if (!tenant.IsActive)
        {
            throw new BadRequestException(
                MasterDataGroupErrors.TenantInactive,
                "Tenant is inactive.");
        }
    }

    private async Task EnsureOrganizationActiveAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var organization = await _organizationService.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            throw new BadRequestException(
                MasterDataGroupErrors.InvalidOrganization,
                $"Organization with id '{organizationId}' was not found.");
        }

        if (organization.TenantId != tenantId)
        {
            throw new BadRequestException(
                MasterDataGroupErrors.OrganizationTenantMismatch,
                "Organization does not belong to the specified tenant.");
        }

        if (!organization.IsActive)
        {
            throw new BadRequestException(
                MasterDataGroupErrors.OrganizationInactive,
                "Organization is inactive.");
        }
    }
}
