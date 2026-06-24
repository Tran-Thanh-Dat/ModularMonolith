using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Organizations.Domain.Enums;
using Organizations.Infrastructure.Persistence;
using Organizations.Infrastructure.Services;
using Organizations.Infrastructure.Tests.Fakes;
using Xunit;

namespace Organizations.Infrastructure.Tests.Services;

public sealed class OrganizationsModuleTests : IDisposable
{
    private readonly OrganizationsDbContext _dbContext;
    private readonly OrganizationsUnitOfWork _unitOfWork;
    private readonly FakeActivityLogService _activityLog;
    private readonly FakeIdentityUserRepository _identityUsers;
    private readonly TenantService _tenantService;
    private readonly OrganizationService _organizationService;
    private readonly OrganizationUserService _organizationUserService;
    private readonly WorkspaceService _workspaceService;
    private readonly WorkspaceUserService _workspaceUserService;
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public OrganizationsModuleTests()
    {
        var currentUser = new FakeCurrentUserService();
        var dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero));

        var options = new DbContextOptionsBuilder<OrganizationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new OrganizationsDbContext(options, currentUser, dateTime);
        _unitOfWork = new OrganizationsUnitOfWork(_dbContext, NullLogger<OrganizationsUnitOfWork>.Instance);
        _activityLog = new FakeActivityLogService();
        _identityUsers = new FakeIdentityUserRepository();
        _identityUsers.AddActiveUser(_userId);

        _tenantService = new TenantService(
            _unitOfWork,
            currentUser,
            dateTime,
            _activityLog,
            NullLogger<TenantService>.Instance);

        _organizationService = new OrganizationService(
            _unitOfWork,
            _tenantService,
            currentUser,
            dateTime,
            _activityLog,
            NullLogger<OrganizationService>.Instance);

        _organizationUserService = new OrganizationUserService(
            _unitOfWork,
            _tenantService,
            _organizationService,
            _identityUsers,
            currentUser,
            dateTime,
            _activityLog,
            NullLogger<OrganizationUserService>.Instance);

        _workspaceService = new WorkspaceService(
            _unitOfWork,
            _tenantService,
            _organizationService,
            currentUser,
            dateTime,
            _activityLog,
            NullLogger<WorkspaceService>.Instance);

        _workspaceUserService = new WorkspaceUserService(
            _unitOfWork,
            _tenantService,
            _workspaceService,
            _organizationUserService,
            _identityUsers,
            currentUser,
            dateTime,
            _activityLog,
            NullLogger<WorkspaceUserService>.Instance);
    }

    [Fact]
    public async Task CreateTenant_WhenCodeIsUnique_Succeeds()
    {
        var id = await _tenantService.CreateAsync("ACME", "Acme Corp", null, null);
        await _unitOfWork.SaveChangesAsync();

        var tenant = await _dbContext.Tenants.SingleAsync(t => t.Id == id);
        Assert.Equal("ACME", tenant.Code);
        Assert.Single(_activityLog.PostCommitEntries);
    }

    [Fact]
    public async Task CreateTenant_WhenDuplicateCode_ThrowsConflict()
    {
        await _tenantService.CreateAsync("ACME", "First", null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _tenantService.CreateAsync("acme", "Second", null, null));

        Assert.Equal(TenantErrors.CodeAlreadyExists, exception.Code);
    }

    [Fact]
    public async Task CreateRootOrganization_Succeeds()
    {
        var tenantId = await CreateTenantAsync("T1");

        var orgId = await _organizationService.CreateAsync(
            tenantId,
            null,
            "ROOT",
            "Root Org",
            null,
            OrganizationType.Company,
            0,
            null);

        await _unitOfWork.SaveChangesAsync();
        Assert.NotEqual(Guid.Empty, orgId);
    }

    [Fact]
    public async Task CreateChildOrganization_Succeeds()
    {
        var tenantId = await CreateTenantAsync("T1");
        var parentId = await _organizationService.CreateAsync(
            tenantId, null, "PARENT", "Parent", null, OrganizationType.Division, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var childId = await _organizationService.CreateAsync(
            tenantId, parentId, "CHILD", "Child", null, OrganizationType.Department, 1, null);
        await _unitOfWork.SaveChangesAsync();

        var child = await _dbContext.Organizations.SingleAsync(o => o.Id == childId);
        Assert.Equal(parentId, child.ParentOrganizationId);
    }

    [Fact]
    public async Task CreateOrganization_WhenDuplicateCodeInSameTenant_ThrowsConflict()
    {
        var tenantId = await CreateTenantAsync("T1");
        await _organizationService.CreateAsync(tenantId, null, "ORG-1", "First", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _organizationService.CreateAsync(tenantId, null, "org-1", "Second", null, OrganizationType.Company, 0, null));

        Assert.Equal(OrganizationErrors.CodeAlreadyExists, exception.Code);
    }

    [Fact]
    public async Task CreateOrganization_WhenSameCodeDifferentTenant_Allowed()
    {
        var tenantA = await CreateTenantAsync("TA");
        var tenantB = await CreateTenantAsync("TB");

        await _organizationService.CreateAsync(tenantA, null, "SHARED", "A", null, OrganizationType.Company, 0, null);
        await _organizationService.CreateAsync(tenantB, null, "SHARED", "B", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        Assert.Equal(2, await _dbContext.Organizations.CountAsync(o => o.Code == "SHARED"));
    }

    [Fact]
    public async Task UpdateOrganization_WhenParentIsSelf_ThrowsBadRequest()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _organizationService.UpdateAsync(orgId, "Org", null, OrganizationType.Company, 0, null, orgId));

        Assert.Equal(OrganizationErrors.ParentSelfReference, exception.Code);
    }

    [Fact]
    public async Task UpdateOrganization_WhenCircularParent_ThrowsBadRequest()
    {
        var tenantId = await CreateTenantAsync("T1");
        var a = await _organizationService.CreateAsync(tenantId, null, "A", "A", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();
        var b = await _organizationService.CreateAsync(tenantId, a, "B", "B", null, OrganizationType.Division, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _organizationService.UpdateAsync(a, "A", null, OrganizationType.Company, 0, null, b));

        Assert.Equal(OrganizationErrors.CircularParent, exception.Code);
    }

    [Fact]
    public async Task AssignOrganizationUser_Succeeds()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var membershipId = await _organizationUserService.AssignAsync(tenantId, orgId, _userId, false);
        await _unitOfWork.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, membershipId);
    }

    [Fact]
    public async Task AssignOrganizationUser_WhenDuplicate_ThrowsConflict()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        await _organizationUserService.AssignAsync(tenantId, orgId, _userId, false);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _organizationUserService.AssignAsync(tenantId, orgId, _userId, false));

        Assert.Equal(OrganizationUserErrors.AlreadyExists, exception.Code);
    }

    [Fact]
    public async Task SetDefaultOrganizationUser_Succeeds()
    {
        var tenantId = await CreateTenantAsync("T1");
        var org1 = await _organizationService.CreateAsync(tenantId, null, "O1", "One", null, OrganizationType.Company, 0, null);
        var org2 = await _organizationService.CreateAsync(tenantId, null, "O2", "Two", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var m1 = await _organizationUserService.AssignAsync(tenantId, org1, _userId, true);
        var m2 = await _organizationUserService.AssignAsync(tenantId, org2, _userId, false);
        await _unitOfWork.SaveChangesAsync();

        await _organizationUserService.SetDefaultAsync(m2);
        await _unitOfWork.SaveChangesAsync();

        var memberships = await _dbContext.OrganizationUsers.Where(m => m.UserId == _userId).ToListAsync();
        Assert.Single(memberships, m => m.IsDefault);
        Assert.Equal(m2, memberships.Single(m => m.IsDefault).Id);
    }

    [Fact]
    public async Task SetDefaultOrganizationUser_UnsetsPreviousDefault()
    {
        var tenantId = await CreateTenantAsync("T1");
        var org1 = await _organizationService.CreateAsync(tenantId, null, "O1", "One", null, OrganizationType.Company, 0, null);
        var org2 = await _organizationService.CreateAsync(tenantId, null, "O2", "Two", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var m1 = await _organizationUserService.AssignAsync(tenantId, org1, _userId, true);
        await _unitOfWork.SaveChangesAsync();
        await _organizationUserService.AssignAsync(tenantId, org2, _userId, true);
        await _unitOfWork.SaveChangesAsync();

        var first = await _dbContext.OrganizationUsers.SingleAsync(m => m.Id == m1);
        Assert.False(first.IsDefault);
    }

    [Fact]
    public async Task CreateWorkspace_Succeeds()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var workspaceId = await _workspaceService.CreateAsync(tenantId, orgId, "WS-1", "Workspace", null, null);
        await _unitOfWork.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, workspaceId);
    }

    [Fact]
    public async Task CreateWorkspace_WhenOrganizationDifferentTenant_ThrowsBadRequest()
    {
        var tenantA = await CreateTenantAsync("TA");
        var tenantB = await CreateTenantAsync("TB");
        var orgB = await _organizationService.CreateAsync(tenantB, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _workspaceService.CreateAsync(tenantA, orgB, "WS", "Workspace", null, null));

        Assert.Equal(WorkspaceErrors.OrganizationDifferentTenant, exception.Code);
    }

    [Fact]
    public async Task AssignWorkspaceUser_Succeeds()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();
        await _organizationUserService.AssignAsync(tenantId, orgId, _userId, false);
        await _unitOfWork.SaveChangesAsync();

        var wsId = await _workspaceService.CreateAsync(tenantId, orgId, "WS", "Workspace", null, null);
        await _unitOfWork.SaveChangesAsync();

        var membershipId = await _workspaceUserService.AssignAsync(tenantId, wsId, _userId);
        await _unitOfWork.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, membershipId);
    }

    [Fact]
    public async Task AssignWorkspaceUser_WhenDuplicate_ThrowsConflict()
    {
        var tenantId = await CreateTenantAsync("T1");
        var wsId = await _workspaceService.CreateAsync(tenantId, null, "WS", "Workspace", null, null);
        await _unitOfWork.SaveChangesAsync();

        await _workspaceUserService.AssignAsync(tenantId, wsId, _userId);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _workspaceUserService.AssignAsync(tenantId, wsId, _userId));

        Assert.Equal(WorkspaceUserErrors.AlreadyExists, exception.Code);
    }

    [Fact]
    public async Task DeactivateOrganization_WhenNoActiveDependencies_Succeeds()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        await _organizationService.DeactivateAsync(orgId);
        await _unitOfWork.SaveChangesAsync();

        var org = await _dbContext.Organizations.SingleAsync(o => o.Id == orgId);
        Assert.False(org.IsActive);
    }

    [Fact]
    public async Task AssignOrganizationUser_WhenOrganizationInactive_ThrowsBadRequest()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();
        await _organizationService.DeactivateAsync(orgId);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _organizationUserService.AssignAsync(tenantId, orgId, _userId, false));

        Assert.Equal(OrganizationErrors.Inactive, exception.Code);
    }

    [Fact]
    public async Task AssignWorkspaceUser_WhenWorkspaceInactive_ThrowsBadRequest()
    {
        var tenantId = await CreateTenantAsync("T1");
        var wsId = await _workspaceService.CreateAsync(tenantId, null, "WS", "Workspace", null, null);
        await _unitOfWork.SaveChangesAsync();
        await _workspaceService.DeactivateAsync(wsId);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _workspaceUserService.AssignAsync(tenantId, wsId, _userId));

        Assert.Equal(WorkspaceErrors.Inactive, exception.Code);
    }

    [Fact]
    public async Task DeleteOrganization_WhenChildActive_ThrowsConflict()
    {
        var tenantId = await CreateTenantAsync("T1");
        var parentId = await _organizationService.CreateAsync(tenantId, null, "P", "Parent", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();
        await _organizationService.CreateAsync(tenantId, parentId, "C", "Child", null, OrganizationType.Department, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _organizationService.DeleteAsync(parentId));

        Assert.Equal(OrganizationErrors.HasActiveDependencies, exception.Code);
    }

    [Fact]
    public async Task DeleteTenant_WhenOrganizationActive_ThrowsConflict()
    {
        var tenantId = await CreateTenantAsync("T1");
        await _organizationService.CreateAsync(tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _tenantService.DeleteAsync(tenantId));

        Assert.Equal(TenantErrors.HasActiveDependencies, exception.Code);
    }

    [Fact]
    public async Task CreateWorkspace_WhenDuplicateCode_ThrowsConflict()
    {
        var tenantId = await CreateTenantAsync("T1");
        await _workspaceService.CreateAsync(tenantId, null, "WS001", "First", null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _workspaceService.CreateAsync(tenantId, null, "ws001", "Second", null, null));

        Assert.Equal(WorkspaceErrors.CodeAlreadyExists, exception.Code);
    }

    [Fact]
    public async Task CreateWorkspace_WhenSameCodeDifferentTenant_Allowed()
    {
        var tenantA = await CreateTenantAsync("TA");
        var tenantB = await CreateTenantAsync("TB");

        await _workspaceService.CreateAsync(tenantA, null, "WS001", "A", null, null);
        await _workspaceService.CreateAsync(tenantB, null, "WS001", "B", null, null);
        await _unitOfWork.SaveChangesAsync();

        Assert.Equal(2, await _dbContext.Workspaces.CountAsync(w => w.Code == "WS001"));
    }

    [Fact]
    public async Task UpdateWorkspace_WhenOrganizationInactive_ThrowsBadRequest()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(
            tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var wsId = await _workspaceService.CreateAsync(tenantId, null, "WS", "Workspace", null, null);
        await _unitOfWork.SaveChangesAsync();

        await _organizationService.DeactivateAsync(orgId);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _workspaceService.UpdateAsync(wsId, "Workspace", null, orgId, null));

        Assert.Equal(OrganizationErrors.Inactive, exception.Code);
    }

    [Fact]
    public async Task AssignWorkspaceUser_WhenOrganizationMembershipMissing_ThrowsBadRequest()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(
            tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var wsId = await _workspaceService.CreateAsync(tenantId, orgId, "WS", "Workspace", null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _workspaceUserService.AssignAsync(tenantId, wsId, _userId));

        Assert.Equal(WorkspaceUserErrors.OrganizationMembershipRequired, exception.Code);
    }

    [Fact]
    public async Task DeactivateTenant_WhenActiveWorkspaceUsers_ThrowsConflict()
    {
        var tenantId = await CreateTenantAsync("T1");
        var wsId = await _workspaceService.CreateAsync(tenantId, null, "WS", "Workspace", null, null);
        await _unitOfWork.SaveChangesAsync();

        await _workspaceUserService.AssignAsync(tenantId, wsId, _userId);
        await _unitOfWork.SaveChangesAsync();

        var workspace = await _dbContext.Workspaces.SingleAsync(w => w.Id == wsId);
        _dbContext.Entry(workspace).Property(nameof(Organizations.Domain.Workspaces.Workspace.IsActive)).CurrentValue = false;
        await _dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _tenantService.DeactivateAsync(tenantId));

        Assert.Equal(TenantErrors.HasActiveDependencies, exception.Code);
    }

    [Fact]
    public async Task DeactivateOrganizationUser_SetsLeftAt()
    {
        var tenantId = await CreateTenantAsync("T1");
        var orgId = await _organizationService.CreateAsync(
            tenantId, null, "ORG", "Org", null, OrganizationType.Company, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var membershipId = await _organizationUserService.AssignAsync(tenantId, orgId, _userId, false);
        await _unitOfWork.SaveChangesAsync();

        await _organizationUserService.DeactivateAsync(membershipId);
        await _unitOfWork.SaveChangesAsync();

        var membership = await _dbContext.OrganizationUsers.SingleAsync(m => m.Id == membershipId);
        Assert.False(membership.IsActive);
        Assert.NotNull(membership.LeftAt);
    }

    [Fact]
    public async Task DeactivateWorkspaceUser_SetsLeftAt()
    {
        var tenantId = await CreateTenantAsync("T1");
        var wsId = await _workspaceService.CreateAsync(tenantId, null, "WS", "Workspace", null, null);
        await _unitOfWork.SaveChangesAsync();

        var membershipId = await _workspaceUserService.AssignAsync(tenantId, wsId, _userId);
        await _unitOfWork.SaveChangesAsync();

        await _workspaceUserService.DeactivateAsync(membershipId);
        await _unitOfWork.SaveChangesAsync();

        var membership = await _dbContext.WorkspaceUsers.SingleAsync(m => m.Id == membershipId);
        Assert.False(membership.IsActive);
        Assert.NotNull(membership.LeftAt);
    }

    private async Task<Guid> CreateTenantAsync(string code)
    {
        var id = await _tenantService.CreateAsync(code, code, null, null);
        await _unitOfWork.SaveChangesAsync();
        return id;
    }

    public void Dispose() => _dbContext.Dispose();
}
