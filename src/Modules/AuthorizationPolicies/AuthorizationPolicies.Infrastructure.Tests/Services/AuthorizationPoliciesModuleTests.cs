using AuditLogs.Domain.Constants;
using AuthorizationPolicies.Application.Constants;
using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.Models;
using AuthorizationPolicies.Domain.AuthorizationMatrix;
using AuthorizationPolicies.Domain.Constants;
using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Domain.Errors;
using AuthorizationPolicies.Domain.PermissionPolicies;
using AuthorizationPolicies.Domain.UserPermissionPolicyOverrides;
using AuthorizationPolicies.Infrastructure.Persistence;
using AuthorizationPolicies.Infrastructure.Services;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthorizationPolicies.Infrastructure.Tests.Services;

public sealed class AuthorizationPoliciesModuleTests : IDisposable
{
    private readonly AuthorizationPoliciesDbContext _dbContext;
    private readonly AuthorizationPoliciesUnitOfWork _unitOfWork;
    private readonly FakeActivityLogService _activityLog;
    private readonly FakeIdentityUserRepository _identityUsers;
    private readonly FakeOrganizationMembershipProvider _membershipProvider;
    private readonly FixedDateTimeProvider _dateTime;
    private readonly PermissionPolicyService _policyService;
    private readonly RolePermissionPolicyService _rolePolicyService;
    private readonly UserPermissionPolicyOverrideService _userOverrideService;
    private readonly AuthorizationMatrixEntryService _matrixService;
    private readonly AuthorizationMatrixService _evaluator;
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _roleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _tenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _organizationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _workspaceId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public AuthorizationPoliciesModuleTests()
    {
        var currentUser = new FakeCurrentUserService();
        _dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero));

        var options = new DbContextOptionsBuilder<AuthorizationPoliciesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new AuthorizationPoliciesDbContext(options, currentUser, _dateTime);
        _unitOfWork = new AuthorizationPoliciesUnitOfWork(_dbContext, NullLogger<AuthorizationPoliciesUnitOfWork>.Instance);
        _activityLog = new FakeActivityLogService();
        _identityUsers = new FakeIdentityUserRepository();
        _membershipProvider = new FakeOrganizationMembershipProvider();

        _policyService = new PermissionPolicyService(
            _unitOfWork, currentUser, _dateTime, _activityLog, NullLogger<PermissionPolicyService>.Instance);

        _rolePolicyService = new RolePermissionPolicyService(
            _unitOfWork, currentUser, _dateTime, _activityLog, NullLogger<RolePermissionPolicyService>.Instance);

        _userOverrideService = new UserPermissionPolicyOverrideService(
            _unitOfWork, currentUser, _dateTime, _activityLog, _identityUsers, NullLogger<UserPermissionPolicyOverrideService>.Instance);

        _matrixService = new AuthorizationMatrixEntryService(
            _unitOfWork, currentUser, _dateTime, _activityLog, NullLogger<AuthorizationMatrixEntryService>.Instance);

        _evaluator = new AuthorizationMatrixService(
            _unitOfWork, _membershipProvider, _dateTime, _activityLog, NullLogger<AuthorizationMatrixService>.Instance);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task CreatePermissionPolicy_WhenValid_Succeeds()
    {
        var id = await _policyService.CreateAsync(
            "ORG-VIEW", "Org View Policy", null, "Organization.View", "Organizations",
            AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, AuthorizationEffect.Allow, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var policy = await _dbContext.PermissionPolicies.SingleAsync(p => p.Id == id);
        Assert.Equal("ORG-VIEW", policy.Code);
        Assert.Single(_activityLog.PostCommitEntries);
    }

    [Fact]
    public async Task CreatePermissionPolicy_WhenDuplicateCode_ThrowsConflict()
    {
        await _policyService.CreateAsync(
            "DUP", "First", null, "Organization.View", "Organizations",
            AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, AuthorizationEffect.Allow, 0, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _policyService.CreateAsync(
                "dup", "Second", null, "Organization.View", "Organizations",
                AuthorizationResourceTypes.Organization, AuthorizationActions.View,
                AuthorizationScope.Organization, AuthorizationEffect.Allow, 0, null));

        Assert.Equal(PermissionPolicyErrors.CodeAlreadyExists, exception.Code);
    }

    [Fact]
    public async Task AssignRolePermissionPolicy_WhenDuplicate_ThrowsConflict()
    {
        var policyId = await CreateActivePolicyAsync("ROLE-POLICY", "Organization.View");
        await _unitOfWork.SaveChangesAsync();

        await _rolePolicyService.AssignAsync(_roleId, policyId);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _rolePolicyService.AssignAsync(_roleId, policyId));

        Assert.Equal(RolePermissionPolicyErrors.AlreadyExists, exception.Code);
    }

    [Fact]
    public async Task CreateUserOverride_WhenPolicyInactive_ThrowsBadRequest()
    {
        var policyId = await _policyService.CreateAsync(
            "INACTIVE", "Inactive", null, "Organization.View", "Organizations",
            AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, AuthorizationEffect.Allow, 0, null);
        await _unitOfWork.SaveChangesAsync();
        await _policyService.DeactivateAsync(policyId);
        await _unitOfWork.SaveChangesAsync();

        _identityUsers.AddActiveUser(_userId);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _userOverrideService.CreateAsync(_userId, policyId, AuthorizationEffect.Allow, null, null));

        Assert.Equal(UserPermissionPolicyOverrideErrors.PolicyInactive, exception.Code);
    }

    [Fact]
    public async Task CreateMatrixEntry_WhenDuplicate_ThrowsConflict()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Tenant, AuthorizationActions.View,
            AuthorizationScope.Global, "Tenant.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _matrixService.CreateAsync(
                "Organizations", AuthorizationResourceTypes.Tenant, AuthorizationActions.View,
                AuthorizationScope.Global, "Tenant.View", null, null));

        Assert.Equal(AuthorizationMatrixErrors.AlreadyExists, exception.Code);
    }

    [Fact]
    public async Task Evaluate_GlobalScope_WithPermission_Allows()
    {
        SetUserContext(["Tenant.View"]);
        var result = await _evaluator.CheckAsync(
            _userId, "Tenant.View", new AuthorizationResourceContext(), cancellationToken: default);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_TenantScope_WhenUserInTenant_Allows()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Tenant, AuthorizationActions.View,
            AuthorizationScope.Tenant, "Tenant.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Tenant.View"], tenantIds: [_tenantId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { TenantId = _tenantId },
            AuthorizationActions.View, AuthorizationResourceTypes.Tenant, "Organizations");
        Assert.True(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_TenantScope_WhenUserNotInTenant_Denies()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Tenant, AuthorizationActions.View,
            AuthorizationScope.Tenant, "Tenant.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Tenant.View"], tenantIds: []);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { TenantId = _tenantId },
            AuthorizationActions.View, AuthorizationResourceTypes.Tenant, "Organizations");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_OrganizationScope_WhenUserInOrganization_Allows()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Organization.View"], organizationIds: [_organizationId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { OrganizationId = _organizationId },
            AuthorizationActions.View, AuthorizationResourceTypes.Organization, "Organizations");
        Assert.True(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_OrganizationScope_WhenUserNotInOrganization_Denies()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Organization.View"], organizationIds: []);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { OrganizationId = _organizationId },
            AuthorizationActions.View, AuthorizationResourceTypes.Organization, "Organizations");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_WorkspaceScope_WhenUserInWorkspace_Allows()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Workspace, AuthorizationActions.View,
            AuthorizationScope.Workspace, "Workspace.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Workspace.View"], workspaceIds: [_workspaceId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { WorkspaceId = _workspaceId },
            AuthorizationActions.View, AuthorizationResourceTypes.Workspace, "Organizations");
        Assert.True(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_WorkspaceScope_WhenUserNotInWorkspace_Denies()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Workspace, AuthorizationActions.View,
            AuthorizationScope.Workspace, "Workspace.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Workspace.View"], workspaceIds: []);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { WorkspaceId = _workspaceId },
            AuthorizationActions.View, AuthorizationResourceTypes.Workspace, "Organizations");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_OwnerOnly_WhenOwner_Allows()
    {
        await _matrixService.CreateAsync(
            "Files", AuthorizationResourceTypes.File, AuthorizationActions.View,
            AuthorizationScope.OwnerOnly, "File.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["File.View"]);
        var result = await _evaluator.CheckAsync(
            _userId,
            null,
            new AuthorizationResourceContext { OwnerUserId = _userId },
            AuthorizationActions.View,
            AuthorizationResourceTypes.File,
            "Files");
        Assert.True(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_OwnerOnly_WhenNotOwner_Denies()
    {
        await _matrixService.CreateAsync(
            "Files", AuthorizationResourceTypes.File, AuthorizationActions.View,
            AuthorizationScope.OwnerOnly, "File.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["File.View"]);
        var result = await _evaluator.CheckAsync(
            _userId,
            null,
            new AuthorizationResourceContext { OwnerUserId = Guid.NewGuid() },
            AuthorizationActions.View,
            AuthorizationResourceTypes.File,
            "Files");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_AssignedOnly_WhenAssigned_Allows()
    {
        await _matrixService.CreateAsync(
            "Notifications", AuthorizationResourceTypes.Notification, AuthorizationActions.View,
            AuthorizationScope.AssignedOnly, "Notification.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Notification.View"]);
        var result = await _evaluator.CheckAsync(
            _userId,
            null,
            new AuthorizationResourceContext { AssignedUserIds = [_userId] },
            AuthorizationActions.View,
            AuthorizationResourceTypes.Notification,
            "Notifications");
        Assert.True(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_AssignedOnly_WhenNotAssigned_Denies()
    {
        await _matrixService.CreateAsync(
            "Notifications", AuthorizationResourceTypes.Notification, AuthorizationActions.View,
            AuthorizationScope.AssignedOnly, "Notification.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Notification.View"]);
        var result = await _evaluator.CheckAsync(
            _userId,
            null,
            new AuthorizationResourceContext { AssignedUserIds = [Guid.NewGuid()] },
            AuthorizationActions.View,
            AuthorizationResourceTypes.Notification,
            "Notifications");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_UserDenyOverride_OverridesAllow()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        var policyId = await CreateActivePolicyAsync("DENY-POLICY", "Organization.View");
        await _unitOfWork.SaveChangesAsync();

        _identityUsers.AddActiveUser(_userId);
        await _userOverrideService.CreateAsync(_userId, policyId, AuthorizationEffect.Deny, null, "test deny");
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Organization.View"], organizationIds: [_organizationId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { OrganizationId = _organizationId },
            AuthorizationActions.View, AuthorizationResourceTypes.Organization, "Organizations");
        Assert.False(result.Data!.IsAllowed);
        Assert.Equal(AuthorizationEffect.Deny, result.Data.Effect);
    }

    [Fact]
    public async Task Evaluate_ExpiredOverride_Ignored()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        var policyId = await CreateActivePolicyAsync("EXP-POLICY", "Organization.View");
        await _unitOfWork.SaveChangesAsync();

        _identityUsers.AddActiveUser(_userId);
        var expiredOverride = UserPermissionPolicyOverride.Create(
            _userId, policyId, AuthorizationEffect.Deny, _dateTime.UtcNow.AddMinutes(-1), "expired", _dateTime.UtcNow);
        await _dbContext.UserPermissionPolicyOverrides.AddAsync(expiredOverride);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Organization.View"], organizationIds: [_organizationId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { OrganizationId = _organizationId },
            AuthorizationActions.View, AuthorizationResourceTypes.Organization, "Organizations");
        Assert.True(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task DeactivatePolicy_ShouldAffectEvaluation()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Workspace, AuthorizationActions.View,
            AuthorizationScope.Workspace, "Workspace.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        var policyId = await _policyService.CreateAsync(
            "DEACT-DENY", "Deactivate Deny", null, "Workspace.View", "Organizations",
            AuthorizationResourceTypes.Workspace, AuthorizationActions.View,
            AuthorizationScope.Workspace, AuthorizationEffect.Deny, 10, null);
        await _unitOfWork.SaveChangesAsync();
        await _rolePolicyService.AssignAsync(_roleId, policyId);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Workspace.View"], workspaceIds: [_workspaceId]);
        var before = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { WorkspaceId = _workspaceId },
            AuthorizationActions.View, AuthorizationResourceTypes.Workspace, "Organizations");
        Assert.False(before.Data!.IsAllowed);

        await _policyService.DeactivateAsync(policyId);
        await _unitOfWork.SaveChangesAsync();

        var after = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { WorkspaceId = _workspaceId },
            AuthorizationActions.View, AuthorizationResourceTypes.Workspace, "Organizations");
        Assert.True(after.Data!.IsAllowed);
    }

    [Fact]
    public async Task DeleteMatrixEntry_WithExistingPolicy_ShouldFollowDefinedRule()
    {
        await SeedMatrixEntryAsync();
        var entry = await _dbContext.AuthorizationMatrixEntries.SingleAsync();
        await _matrixService.DeleteAsync(entry.Id);
        await _unitOfWork.SaveChangesAsync();

        var deleted = await _dbContext.AuthorizationMatrixEntries.SingleAsync(e => e.Id == entry.Id);
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserDenyOverride_ShouldDeny()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        var policyId = await CreateActivePolicyAsync("AUTH-DENY", "Organization.View");
        await _unitOfWork.SaveChangesAsync();

        _identityUsers.AddActiveUser(_userId);
        await _userOverrideService.CreateAsync(_userId, policyId, AuthorizationEffect.Deny, null, "deny override");
        await _unitOfWork.SaveChangesAsync();

        var userContext = BuildUserContext(["Organization.View"], organizationIds: [_organizationId]);
        var result = await _evaluator.AuthorizeAsync(
            userContext,
            "Organization.View",
            new AuthorizationResourceContext { OrganizationId = _organizationId },
            AuthorizationScope.Organization);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldUseSameLogicAsCheckAsync()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Organization.View"], organizationIds: []);
        var userContext = BuildUserContext(["Organization.View"], organizationIds: []);
        var resourceContext = new AuthorizationResourceContext { OrganizationId = _organizationId };

        var checkResult = await _evaluator.CheckAsync(
            _userId, null, resourceContext,
            AuthorizationActions.View, AuthorizationResourceTypes.Organization, "Organizations");
        var authorizeResult = await _evaluator.AuthorizeAsync(
            userContext, "Organization.View", resourceContext, AuthorizationScope.Organization);

        Assert.False(checkResult.Data!.IsAllowed);
        Assert.False(authorizeResult.IsSuccess);
    }

    [Fact]
    public async Task Evaluate_RoleDenyPolicy_OverridesAllow()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        var denyPolicyId = await _policyService.CreateAsync(
            "ROLE-DENY", "Role Deny", null, "Organization.View", "Organizations",
            AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, AuthorizationEffect.Deny, 5, null);
        await _unitOfWork.SaveChangesAsync();
        await _rolePolicyService.AssignAsync(_roleId, denyPolicyId);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Organization.View"], organizationIds: [_organizationId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { OrganizationId = _organizationId },
            AuthorizationActions.View, AuthorizationResourceTypes.Organization, "Organizations");

        Assert.False(result.Data!.IsAllowed);
        Assert.Equal(AuthorizationEffect.Deny, result.Data.Effect);
    }

    [Fact]
    public async Task Evaluate_RoleDenyPolicy_UsesHighestPriority()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        var lowPriorityDenyId = await _policyService.CreateAsync(
            "DENY-LOW", "Low Deny", null, "Organization.View", "Organizations",
            AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, AuthorizationEffect.Deny, 1, null);
        var highPriorityDenyId = await _policyService.CreateAsync(
            "DENY-HIGH", "High Deny", null, "Organization.View", "Organizations",
            AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, AuthorizationEffect.Deny, 100, null);
        await _unitOfWork.SaveChangesAsync();
        await _rolePolicyService.AssignAsync(_roleId, lowPriorityDenyId);
        await _rolePolicyService.AssignAsync(_roleId, highPriorityDenyId);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Organization.View"], organizationIds: [_organizationId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { OrganizationId = _organizationId },
            AuthorizationActions.View, AuthorizationResourceTypes.Organization, "Organizations");

        Assert.False(result.Data!.IsAllowed);
        Assert.Equal("DENY-HIGH", result.Data.MatchedPolicyCode);
    }

    [Fact]
    public async Task Evaluate_SelfScope_WhenSelf_Allows()
    {
        await _matrixService.CreateAsync(
            "Identity", AuthorizationResourceTypes.User, AuthorizationActions.View,
            AuthorizationScope.Self, "User.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["User.View"]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { ResourceId = _userId },
            AuthorizationActions.View, AuthorizationResourceTypes.User, "Identity");
        Assert.True(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_SelfScope_WhenNotSelf_Denies()
    {
        await _matrixService.CreateAsync(
            "Identity", AuthorizationResourceTypes.User, AuthorizationActions.View,
            AuthorizationScope.Self, "User.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["User.View"]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { ResourceId = Guid.NewGuid() },
            AuthorizationActions.View, AuthorizationResourceTypes.User, "Identity");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_InactiveRolePolicyAssignment_Ignored()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        var denyPolicyId = await _policyService.CreateAsync(
            "INACT-ASSIGN-DENY", "Inactive Assignment Deny", null, "Organization.View", "Organizations",
            AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, AuthorizationEffect.Deny, 10, null);
        await _unitOfWork.SaveChangesAsync();
        var assignmentId = await _rolePolicyService.AssignAsync(_roleId, denyPolicyId);
        await _unitOfWork.SaveChangesAsync();
        await _rolePolicyService.DeactivateAsync(assignmentId);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Organization.View"], organizationIds: [_organizationId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { OrganizationId = _organizationId },
            AuthorizationActions.View, AuthorizationResourceTypes.Organization, "Organizations");
        Assert.True(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_TenantScope_WhenTenantIdMissing_DeniesOrBadRequest()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Tenant, AuthorizationActions.View,
            AuthorizationScope.Tenant, "Tenant.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Tenant.View"], tenantIds: [_tenantId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext(),
            AuthorizationActions.View, AuthorizationResourceTypes.Tenant, "Organizations");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_OrganizationScope_WhenOrganizationIdMissing_DeniesOrBadRequest()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, "Organization.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Organization.View"], organizationIds: [_organizationId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext(),
            AuthorizationActions.View, AuthorizationResourceTypes.Organization, "Organizations");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_WorkspaceScope_WhenWorkspaceIdMissing_DeniesOrBadRequest()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Workspace, AuthorizationActions.View,
            AuthorizationScope.Workspace, "Workspace.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Workspace.View"], workspaceIds: [_workspaceId]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext(),
            AuthorizationActions.View, AuthorizationResourceTypes.Workspace, "Organizations");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_OwnerOnly_WhenOwnerMissing_DeniesOrBadRequest()
    {
        await _matrixService.CreateAsync(
            "Files", AuthorizationResourceTypes.File, AuthorizationActions.View,
            AuthorizationScope.OwnerOnly, "File.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["File.View"]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext(),
            AuthorizationActions.View, AuthorizationResourceTypes.File, "Files");
        Assert.False(result.Data!.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_AssignedOnly_WhenAssignedUsersEmpty_DeniesOrBadRequest()
    {
        await _matrixService.CreateAsync(
            "Notifications", AuthorizationResourceTypes.Notification, AuthorizationActions.View,
            AuthorizationScope.AssignedOnly, "Notification.View", null, null);
        await _unitOfWork.SaveChangesAsync();

        SetUserContext(["Notification.View"]);
        var result = await _evaluator.CheckAsync(
            _userId, null, new AuthorizationResourceContext { AssignedUserIds = [] },
            AuthorizationActions.View, AuthorizationResourceTypes.Notification, "Notifications");
        Assert.False(result.Data!.IsAllowed);
    }

    private async Task<Guid> CreateActivePolicyAsync(string code, string permissionCode) =>
        await _policyService.CreateAsync(
            code, code, null, permissionCode, "Organizations",
            AuthorizationResourceTypes.Organization, AuthorizationActions.View,
            AuthorizationScope.Organization, AuthorizationEffect.Allow, 0, null);

    private async Task SeedMatrixEntryAsync()
    {
        await _matrixService.CreateAsync(
            "Organizations", AuthorizationResourceTypes.Tenant, AuthorizationActions.View,
            AuthorizationScope.Global, "Tenant.View", null, null);
        await _unitOfWork.SaveChangesAsync();
    }

    private void SetUserContext(
        string[] permissions,
        Guid[]? tenantIds = null,
        Guid[]? organizationIds = null,
        Guid[]? workspaceIds = null)
    {
        _membershipProvider.SetContext(_userId, BuildUserContext(permissions, tenantIds, organizationIds, workspaceIds));
    }

    private CurrentUserPermissionContext BuildUserContext(
        string[] permissions,
        Guid[]? tenantIds = null,
        Guid[]? organizationIds = null,
        Guid[]? workspaceIds = null) =>
        new()
        {
            UserId = _userId,
            RoleIds = [_roleId],
            PermissionCodes = permissions,
            TenantIds = tenantIds ?? [],
            OrganizationIds = organizationIds ?? [],
            WorkspaceIds = workspaceIds ?? [],
            IsAuthenticated = true
        };
}

internal sealed class FakeOrganizationMembershipProvider : ICurrentUserPermissionContextService
{
    private readonly Dictionary<Guid, CurrentUserPermissionContext> _contexts = new();

    public void SetContext(Guid userId, CurrentUserPermissionContext context) => _contexts[userId] = context;

    public Task<CurrentUserPermissionContext> GetContextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_contexts.TryGetValue(userId, out var context))
        {
            throw new NotFoundException(UserErrors.NotFound, "User not found.");
        }

        return Task.FromResult(context);
    }
}

internal sealed class FakeIdentityUserRepository : Identity.Application.Abstractions.IIdentityUserRepository
{
    private readonly HashSet<Guid> _activeUserIds = [];

    public void AddActiveUser(Guid userId) => _activeUserIds.Add(userId);

    public Task<Identity.Domain.Users.User?> FindActiveByUserNameOrEmailAsync(string normalizedUserNameOrEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult<Identity.Domain.Users.User?>(null);

    public Task<Identity.Domain.Users.User?> FindActiveByIdWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_activeUserIds.Contains(userId))
        {
            return Task.FromResult<Identity.Domain.Users.User?>(null);
        }

        return Task.FromResult<Identity.Domain.Users.User?>(
            Identity.Domain.Users.User.Create("testuser", "test@example.com", "hash", "Test User", DateTimeOffset.UtcNow));
    }

    public Task<Identity.Domain.Users.User?> FindActiveByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult<Identity.Domain.Users.User?>(null);

    public Task<Identity.Domain.Users.User?> FindActiveByIdForUpdateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_activeUserIds.Contains(userId))
        {
            return Task.FromResult<Identity.Domain.Users.User?>(null);
        }

        return Task.FromResult<Identity.Domain.Users.User?>(
            Identity.Domain.Users.User.Create("testuser", "test@example.com", "hash", "Test User", DateTimeOffset.UtcNow));
    }

    public Task<bool> EmailExistsForOtherUserAsync(string normalizedEmail, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
