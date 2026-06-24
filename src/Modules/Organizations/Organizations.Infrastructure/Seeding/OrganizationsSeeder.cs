using BuildingBlocks.Application.Abstractions;
using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Organizations.Application.Abstractions;
using Organizations.Domain.Enums;
using Organizations.Domain.Organizations;
using Organizations.Domain.OrganizationUsers;
using Organizations.Domain.Tenants;
using Organizations.Domain.Workspaces;
using Organizations.Domain.WorkspaceUsers;
using Organizations.Infrastructure.Persistence;

namespace Organizations.Infrastructure.Seeding;

public sealed class OrganizationsSeeder : IOrganizationsSeeder
{
    private const string DemoTenantCode = "DEMO";
    private const string DemoOrganizationCode = "HQ";
    private const string DemoWorkspaceCode = "MAIN";
    private const string AdminSeedSection = "AdminSeed";

    private readonly OrganizationsUnitOfWork _unitOfWork;
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly OrganizationsSeedOptions _seedOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrganizationsSeeder> _logger;

    public OrganizationsSeeder(
        OrganizationsUnitOfWork unitOfWork,
        IIdentityUserRepository identityUserRepository,
        IDateTimeProvider dateTimeProvider,
        IOptions<OrganizationsSeedOptions> seedOptions,
        IConfiguration configuration,
        ILogger<OrganizationsSeeder> logger)
    {
        _unitOfWork = unitOfWork;
        _identityUserRepository = identityUserRepository;
        _dateTimeProvider = dateTimeProvider;
        _seedOptions = seedOptions.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_seedOptions.Enabled)
        {
            _logger.LogInformation("Organizations seed is disabled.");
            return;
        }

        if (await TenantExistsAsync(DemoTenantCode, cancellationToken))
        {
            _logger.LogInformation("Organizations demo data already exists (tenant {TenantCode}). Skipping seed.", DemoTenantCode);
            return;
        }

        var now = _dateTimeProvider.UtcNow;

        var tenant = Tenant.Create(
            DemoTenantCode,
            "Demo Tenant",
            "Sample tenant for local development and API testing.",
            null,
            now);

        var organization = Organization.Create(
            tenant.Id,
            parentOrganizationId: null,
            DemoOrganizationCode,
            "Demo Company",
            "Root organization for the demo tenant.",
            OrganizationType.Company,
            sortOrder: 0,
            metadata: null,
            now);

        var workspace = Workspace.Create(
            tenant.Id,
            organization.Id,
            DemoWorkspaceCode,
            "Main Workspace",
            "Default workspace linked to the demo company.",
            metadata: null,
            now);

        await _unitOfWork.Repository<Tenant, Guid>().AddAsync(tenant, cancellationToken);
        await _unitOfWork.Repository<Organization, Guid>().AddAsync(organization, cancellationToken);
        await _unitOfWork.Repository<Workspace, Guid>().AddAsync(workspace, cancellationToken);

        var adminUserName = _configuration.GetValue<string>($"{AdminSeedSection}:UserName") ?? "admin";
        var adminUser = await _identityUserRepository.FindActiveByUserNameOrEmailAsync(
            adminUserName.Trim().ToLowerInvariant(),
            cancellationToken);

        if (adminUser is not null)
        {
            var organizationUser = OrganizationUser.Create(
                tenant.Id,
                organization.Id,
                adminUser.Id,
                isDefault: true,
                now,
                now);

            var workspaceUser = WorkspaceUser.Create(
                tenant.Id,
                workspace.Id,
                adminUser.Id,
                now,
                now);

            await _unitOfWork.Repository<OrganizationUser, Guid>().AddAsync(organizationUser, cancellationToken);
            await _unitOfWork.Repository<WorkspaceUser, Guid>().AddAsync(workspaceUser, cancellationToken);

            _logger.LogInformation(
                "Assigned admin user {UserName} to demo organization and workspace.",
                adminUserName);
        }
        else
        {
            _logger.LogWarning(
                "Admin user {UserName} not found. Demo tenant/org/workspace were created without membership assignments.",
                adminUserName);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded Organizations demo data: tenant {TenantCode}, organization {OrganizationCode}, workspace {WorkspaceCode}.",
            DemoTenantCode,
            DemoOrganizationCode,
            DemoWorkspaceCode);
    }

    private async Task<bool> TenantExistsAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = Tenant.NormalizeCode(code);

        return await _unitOfWork.Repository<Tenant, Guid>()
            .QueryReadOnly()
            .AnyAsync(t => t.Code == normalizedCode && !t.IsDeleted, cancellationToken);
    }
}
