using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Testing.Fakes;
using MasterData.Application.Dtos;
using MasterData.Domain.Entities;
using MasterData.Domain.Enums;
using MasterData.Domain.Errors;
using MasterData.Infrastructure.Persistence;
using MasterData.Infrastructure.Seeders;
using MasterData.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Organizations.Application.Abstractions;
using Organizations.Application.Organizations;
using Organizations.Application.Tenants;
using Organizations.Domain.Enums;
using Xunit;

namespace MasterData.Infrastructure.Tests;

public sealed class MasterDataModuleTests : IDisposable
{
    private readonly MasterDataDbContext _dbContext;
    private readonly MasterDataUnitOfWork _unitOfWork;
    private readonly FakeActivityLogService _activityLog;
    private readonly RecordingCacheInvalidationBuffer _cacheInvalidation;
    private readonly RecordingCacheService _cacheService;
    private readonly FakeTenantOrganizationServices _tenantOrgServices;
    private readonly MasterDataGroupService _groupService;
    private readonly MasterDataItemService _itemService;
    private readonly LookupService _lookupService;
    private readonly MasterDataSeeder _seeder;
    private readonly FixedDateTimeProvider _dateTime;
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _organizationId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _wrongTenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public MasterDataModuleTests()
    {
        var currentUser = new FakeCurrentUserService();
        _dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 6, 23, 12, 0, 0, TimeSpan.Zero));
        _tenantOrgServices = new FakeTenantOrganizationServices(_tenantId, _organizationId);

        var options = new DbContextOptionsBuilder<MasterDataDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new MasterDataDbContext(options, currentUser, _dateTime);
        _unitOfWork = new MasterDataUnitOfWork(_dbContext, NullLogger<MasterDataUnitOfWork>.Instance);
        _activityLog = new FakeActivityLogService();
        _cacheInvalidation = new RecordingCacheInvalidationBuffer();
        _cacheService = new RecordingCacheService();

        var scopeValidator = new MasterDataScopeValidator(_tenantOrgServices.Tenant, _tenantOrgServices.Organization);
        _groupService = new MasterDataGroupService(
            _unitOfWork, scopeValidator, currentUser, _dateTime, _activityLog, _cacheInvalidation,
            NullLogger<MasterDataGroupService>.Instance);
        _itemService = new MasterDataItemService(
            _unitOfWork, currentUser, _dateTime, _activityLog, _cacheInvalidation,
            NullLogger<MasterDataItemService>.Instance);
        _lookupService = new LookupService(_unitOfWork, _cacheService, _dateTime);
        _seeder = new MasterDataSeeder(_dbContext, _dateTime, NullLogger<MasterDataSeeder>.Instance);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task CreateGroup_WhenValid_Succeeds()
    {
        var id = await _groupService.CreateAsync("USER_STATUS", "User Status", null, MasterDataScope.Global, null, null, 1, null);
        await _unitOfWork.SaveChangesAsync();

        var group = await _dbContext.MasterDataGroups.SingleAsync(g => g.Id == id);
        Assert.Equal("USER_STATUS", group.Code);
        Assert.Single(_activityLog.PostCommitEntries);
    }

    [Fact]
    public async Task CreateGroup_WhenDuplicateCodeSameScope_ThrowsConflict()
    {
        await _groupService.CreateAsync("PRIORITY", "Priority", null, MasterDataScope.Global, null, null, 1, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _groupService.CreateAsync("priority", "Duplicate", null, MasterDataScope.Global, null, null, 2, null));

        Assert.Equal(MasterDataGroupErrors.CodeAlreadyExists, exception.Code);
    }

    [Fact]
    public async Task CreateGroup_WhenSameCodeDifferentTenantScope_Allowed()
    {
        await _groupService.CreateAsync("CUSTOM", "Global", null, MasterDataScope.Global, null, null, 1, null);
        await _unitOfWork.SaveChangesAsync();

        var id = await _groupService.CreateAsync("CUSTOM", "Tenant", null, MasterDataScope.Tenant, _tenantId, null, 1, null);
        await _unitOfWork.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task CreateGroup_WhenOrganizationScopeWrongTenant_ThrowsBadRequest()
    {
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _groupService.CreateAsync("ORG_LOOKUP", "Org Lookup", null, MasterDataScope.Organization, _wrongTenantId, _organizationId, 1, null));

        Assert.Equal(MasterDataGroupErrors.OrganizationTenantMismatch, exception.Code);
    }

    [Fact]
    public async Task DeleteGroup_WhenHasActiveItems_ThrowsConflict()
    {
        var groupId = await _groupService.CreateAsync("DEL_GROUP", "Delete Group", null, MasterDataScope.Global, null, null, 1, null);
        await _unitOfWork.SaveChangesAsync();
        await _itemService.CreateAsync(groupId, "A", "Item A", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() => _groupService.DeleteAsync(groupId));
        Assert.Equal(MasterDataGroupErrors.HasActiveItems, exception.Code);
    }

    [Fact]
    public async Task DeleteSystemGroup_ThrowsBadRequest()
    {
        await _seeder.SeedAsync();
        var systemGroup = await _dbContext.MasterDataGroups.SingleAsync(g => g.Code == "USER_STATUS");

        var exception = await Assert.ThrowsAsync<BadRequestException>(() => _groupService.DeleteAsync(systemGroup.Id));
        Assert.Equal(MasterDataGroupErrors.SystemProtected, exception.Code);
    }

    [Fact]
    public async Task UpdateSystemGroupCode_ThrowsBadRequest()
    {
        await _seeder.SeedAsync();
        var systemGroup = await _dbContext.MasterDataGroups.SingleAsync(g => g.Code == "USER_STATUS");

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _groupService.UpdateAsync(systemGroup.Id, "NEW_CODE", systemGroup.Name, null, 1, null));

        Assert.Equal(MasterDataGroupErrors.CodeChangeNotAllowed, exception.Code);
    }

    [Fact]
    public async Task CreateItem_WhenValid_Succeeds()
    {
        var groupId = await CreateGlobalGroupAsync("ITEM_GROUP");
        var id = await _itemService.CreateAsync(groupId, "ACTIVE", "Active", null, null, null, true, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var item = await _dbContext.MasterDataItems.SingleAsync(i => i.Id == id);
        Assert.True(item.IsDefault);
    }

    [Fact]
    public async Task CreateItem_WhenDuplicateCodeInGroup_ThrowsConflict()
    {
        var groupId = await CreateGlobalGroupAsync("DUP_ITEM_GROUP");
        await _itemService.CreateAsync(groupId, "CODE", "First", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _itemService.CreateAsync(groupId, "code", "Second", null, null, null, false, 2, null, null, null));

        Assert.Equal(MasterDataItemErrors.CodeAlreadyExists, exception.Code);
    }

    [Fact]
    public async Task CreateItem_WhenSameCodeDifferentGroup_Allowed()
    {
        var group1 = await CreateGlobalGroupAsync("GROUP_ONE");
        var group2 = await CreateGlobalGroupAsync("GROUP_TWO");
        await _itemService.CreateAsync(group1, "SHARED", "One", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var id = await _itemService.CreateAsync(group2, "SHARED", "Two", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task CreateItem_WhenGroupInactive_ThrowsBadRequest()
    {
        var groupId = await CreateGlobalGroupAsync("INACTIVE_GROUP");
        await _unitOfWork.SaveChangesAsync();
        await _groupService.DeactivateAsync(groupId);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _itemService.CreateAsync(groupId, "X", "X", null, null, null, false, 1, null, null, null));

        Assert.Equal(MasterDataItemErrors.GroupInactive, exception.Code);
    }

    [Fact]
    public async Task UpdateSystemItemCode_ThrowsBadRequest()
    {
        await _seeder.SeedAsync();
        var item = await _dbContext.MasterDataItems.FirstAsync(i => i.Code == "ACTIVE");

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _itemService.UpdateAsync(item.Id, "NEW_ACTIVE", item.Name, null, null, null, 1, null, null, null));

        Assert.Equal(MasterDataItemErrors.CodeChangeNotAllowed, exception.Code);
    }

    [Fact]
    public async Task DeleteSystemItem_ThrowsBadRequest()
    {
        await _seeder.SeedAsync();
        var item = await _dbContext.MasterDataItems.FirstAsync(i => i.Code == "ACTIVE");

        var exception = await Assert.ThrowsAsync<BadRequestException>(() => _itemService.DeleteAsync(item.Id));
        Assert.Equal(MasterDataItemErrors.SystemProtected, exception.Code);
    }

    [Fact]
    public async Task SetDefaultItem_UnsetsPreviousDefault()
    {
        var groupId = await CreateGlobalGroupAsync("DEFAULT_GROUP");
        var firstId = await _itemService.CreateAsync(groupId, "FIRST", "First", null, null, null, true, 1, null, null, null);
        var secondId = await _itemService.CreateAsync(groupId, "SECOND", "Second", null, null, null, false, 2, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        await _itemService.SetDefaultAsync(secondId);
        await _unitOfWork.SaveChangesAsync();

        var first = await _dbContext.MasterDataItems.SingleAsync(i => i.Id == firstId);
        var second = await _dbContext.MasterDataItems.SingleAsync(i => i.Id == secondId);
        Assert.False(first.IsDefault);
        Assert.True(second.IsDefault);
    }

    [Fact]
    public async Task SetDefaultItem_WhenInactive_ThrowsBadRequest()
    {
        var groupId = await CreateGlobalGroupAsync("DEFAULT_INACTIVE");
        var itemId = await _itemService.CreateAsync(groupId, "INACTIVE", "Inactive", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();
        await _itemService.DeactivateAsync(itemId);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() => _itemService.SetDefaultAsync(itemId));
        Assert.Equal(MasterDataItemErrors.InactiveCannotBeDefault, exception.Code);
    }

    [Fact]
    public async Task CreateItem_WhenParentDifferentGroup_ThrowsBadRequest()
    {
        var group1 = await CreateGlobalGroupAsync("PARENT_G1");
        var group2 = await CreateGlobalGroupAsync("PARENT_G2");
        var parentId = await _itemService.CreateAsync(group1, "PARENT", "Parent", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _itemService.CreateAsync(group2, "CHILD", "Child", null, null, parentId, false, 1, null, null, null));

        Assert.Equal(MasterDataItemErrors.InvalidParent, exception.Code);
    }

    [Fact]
    public async Task UpdateItem_WhenParentSelf_ThrowsBadRequest()
    {
        var groupId = await CreateGlobalGroupAsync("SELF_PARENT");
        var itemId = await _itemService.CreateAsync(groupId, "SELF", "Self", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _itemService.UpdateAsync(itemId, null, "Self", null, null, itemId, 1, null, null, null));

        Assert.Equal(MasterDataItemErrors.ParentSelfReference, exception.Code);
    }

    [Fact]
    public async Task UpdateItem_WhenCircularParent_ThrowsBadRequest()
    {
        var groupId = await CreateGlobalGroupAsync("CIRCULAR");
        var a = await _itemService.CreateAsync(groupId, "A", "A", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();
        var b = await _itemService.CreateAsync(groupId, "B", "B", null, null, a, false, 2, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _itemService.UpdateAsync(a, null, "A", null, null, b, 1, null, null, null));

        Assert.Equal(MasterDataItemErrors.CircularParent, exception.Code);
    }

    [Fact]
    public async Task Lookup_GetActiveItems_ReturnsOnlyActiveGroupAndItems()
    {
        await _seeder.SeedAsync();
        var items = await _lookupService.GetByGroupCodeAsync(
            new LookupQuery("USER_STATUS", null, MasterDataScope.Global, null, null, false, null, true));

        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.True(i.Code is "ACTIVE" or "INACTIVE" or "LOCKED" or "PENDING_VERIFICATION"));

        var group = await _dbContext.MasterDataGroups.SingleAsync(g => g.Code == "USER_STATUS");
        var inactiveItem = await _dbContext.MasterDataItems.FirstAsync(i => i.GroupId == group.Id && i.Code == "INACTIVE");
        await _itemService.DeactivateAsync(inactiveItem.Id);
        await _unitOfWork.SaveChangesAsync();
        await _cacheService.RemoveByPrefixAsync(CacheKeys.MasterDataLookupPrefix);

        var filtered = await _lookupService.GetByGroupCodeAsync(
            new LookupQuery("USER_STATUS", null, MasterDataScope.Global, null, null, false, null, true));

        Assert.DoesNotContain(filtered, i => i.Code == "INACTIVE");
    }

    [Fact]
    public async Task Lookup_EffectiveDate_FiltersItemsCorrectly()
    {
        var groupId = await CreateGlobalGroupAsync("EFFECTIVE");
        var from = _dateTime.UtcNow.AddDays(1);
        await _itemService.CreateAsync(groupId, "FUTURE", "Future", null, null, null, false, 1, from, null, null);
        await _unitOfWork.SaveChangesAsync();

        var nowItems = await _lookupService.GetByGroupCodeAsync(
            new LookupQuery("EFFECTIVE", null, MasterDataScope.Global, null, null, false, _dateTime.UtcNow, true));
        Assert.Empty(nowItems);

        var futureItems = await _lookupService.GetByGroupCodeAsync(
            new LookupQuery("EFFECTIVE", null, MasterDataScope.Global, null, null, false, from.AddHours(1), true));
        Assert.Single(futureItems);
    }

    [Fact]
    public async Task Lookup_Batch_ReturnsMultipleGroups()
    {
        await _seeder.SeedAsync();

        var batch = await _lookupService.GetBatchAsync(
            new LookupQuery(null, ["USER_STATUS", "PRIORITY"], MasterDataScope.Global, null, null, false, null, false));

        Assert.Equal(2, batch.Groups.Count);
        Assert.True(batch.Groups.ContainsKey("USER_STATUS"));
        Assert.True(batch.Groups.ContainsKey("PRIORITY"));
    }

    [Fact]
    public async Task Cache_Invalidated_WhenItemUpdated()
    {
        var groupId = await CreateGlobalGroupAsync("CACHE_GROUP");
        var itemId = await _itemService.CreateAsync(groupId, "CACHE", "Cache", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();
        _cacheInvalidation.RemovedPrefixes.Clear();

        await _itemService.UpdateAsync(itemId, null, "Updated", null, null, null, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        Assert.Contains(CacheKeys.MasterDataLookupPrefix, _cacheInvalidation.RemovedPrefixes);
    }

    [Fact]
    public async Task DeactivateGroup_WhenHasActiveItems_ThrowsConflict()
    {
        var groupId = await CreateGlobalGroupAsync("DEACT_GROUP");
        await _itemService.CreateAsync(groupId, "A", "Item A", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() => _groupService.DeactivateAsync(groupId));
        Assert.Equal(MasterDataGroupErrors.HasActiveItems, exception.Code);
    }

    [Fact]
    public async Task Lookup_WhenGroupInactive_ReturnsEmpty()
    {
        var groupId = await CreateGlobalGroupAsync("INACTIVE_LOOKUP");
        await _itemService.CreateAsync(groupId, "X", "Item X", null, null, null, false, 1, null, null, null);
        await _unitOfWork.SaveChangesAsync();

        var group = await _dbContext.MasterDataGroups.SingleAsync(g => g.Id == groupId);
        group.Deactivate();
        await _unitOfWork.SaveChangesAsync();
        await _cacheService.RemoveByPrefixAsync(CacheKeys.MasterDataLookupPrefix);

        var items = await _lookupService.GetByGroupCodeAsync(
            new LookupQuery("INACTIVE_LOOKUP", null, MasterDataScope.Global, null, null, false, null, true));

        Assert.Empty(items);
        Assert.True(await _dbContext.MasterDataItems.AnyAsync(i => i.GroupId == groupId && i.IsActive));
    }

    [Fact]
    public async Task CreateGroup_WhenTenantInactive_ThrowsBadRequest()
    {
        var inactiveTenant = new FakeTenantService(_tenantId, isActive: false);
        var scopeValidator = new MasterDataScopeValidator(inactiveTenant, _tenantOrgServices.Organization);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MasterDataGroupService(
                _unitOfWork, scopeValidator, new FakeCurrentUserService(), _dateTime, _activityLog, _cacheInvalidation,
                NullLogger<MasterDataGroupService>.Instance)
                .CreateAsync("TENANT_GRP", "Tenant Group", null, MasterDataScope.Tenant, _tenantId, null, 1, null));

        Assert.Equal(MasterDataGroupErrors.TenantInactive, exception.Code);
    }

    [Fact]
    public async Task CreateGroup_WhenOrganizationInactive_ThrowsBadRequest()
    {
        var inactiveOrg = new FakeOrganizationService(_tenantId, _organizationId, isActive: false);
        var scopeValidator = new MasterDataScopeValidator(_tenantOrgServices.Tenant, inactiveOrg);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            new MasterDataGroupService(
                _unitOfWork, scopeValidator, new FakeCurrentUserService(), _dateTime, _activityLog, _cacheInvalidation,
                NullLogger<MasterDataGroupService>.Instance)
                .CreateAsync("ORG_GRP", "Org Group", null, MasterDataScope.Organization, _tenantId, _organizationId, 1, null));

        Assert.Equal(MasterDataGroupErrors.OrganizationInactive, exception.Code);
    }

    [Fact]
    public async Task Lookup_UsesDateTimeProvider_WhenEffectiveAtMissing()
    {
        var groupId = await CreateGlobalGroupAsync("PROVIDER_TIME");
        var inRangeFrom = _dateTime.UtcNow.AddHours(-1);
        var inRangeTo = _dateTime.UtcNow.AddHours(1);
        var futureFrom = _dateTime.UtcNow.AddDays(1);

        await _itemService.CreateAsync(groupId, "IN_RANGE", "In Range", null, null, null, false, 1, inRangeFrom, inRangeTo, null);
        await _itemService.CreateAsync(groupId, "FUTURE", "Future", null, null, null, false, 2, futureFrom, null, null);
        await _unitOfWork.SaveChangesAsync();
        await _cacheService.RemoveByPrefixAsync(CacheKeys.MasterDataLookupPrefix);

        var items = await _lookupService.GetByGroupCodeAsync(
            new LookupQuery("PROVIDER_TIME", null, MasterDataScope.Global, null, null, false, null, false));

        Assert.Single(items);
        Assert.Equal("IN_RANGE", items[0].Code);
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        await _seeder.SeedAsync();
        var groupCount = await _dbContext.MasterDataGroups.CountAsync();
        var itemCount = await _dbContext.MasterDataItems.CountAsync();

        await _seeder.SeedAsync();

        Assert.Equal(groupCount, await _dbContext.MasterDataGroups.CountAsync());
        Assert.Equal(itemCount, await _dbContext.MasterDataItems.CountAsync());
    }

    private async Task<Guid> CreateGlobalGroupAsync(string code)
    {
        var id = await _groupService.CreateAsync(code, code, null, MasterDataScope.Global, null, null, 1, null);
        await _unitOfWork.SaveChangesAsync();
        return id;
    }

    private sealed class FakeTenantOrganizationServices
    {
        public FakeTenantOrganizationServices(Guid tenantId, Guid organizationId)
        {
            Tenant = new FakeTenantService(tenantId);
            Organization = new FakeOrganizationService(tenantId, organizationId);
        }

        public FakeTenantService Tenant { get; }
        public FakeOrganizationService Organization { get; }
    }

    private sealed class FakeTenantService : ITenantService
    {
        private readonly Guid _tenantId;
        private readonly bool _isActive;
        private readonly bool _includeWrongTenantAlias;

        public FakeTenantService(Guid tenantId, bool isActive = true, bool includeWrongTenantAlias = true)
        {
            _tenantId = tenantId;
            _isActive = isActive;
            _includeWrongTenantAlias = includeWrongTenantAlias;
        }

        public Task<TenantDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == _tenantId || (_includeWrongTenantAlias && id == Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")))
            {
                return Task.FromResult<TenantDetailResponse?>(new TenantDetailResponse
                {
                    Id = id,
                    Code = "T",
                    Name = "Tenant",
                    IsActive = _isActive
                });
            }

            return Task.FromResult<TenantDetailResponse?>(null);
        }

        public Task<Guid> CreateAsync(string code, string name, string? description, string? metadata, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(Guid id, string name, string? description, string? metadata, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task ActivateAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<TenantListItemResponse>> GetListAsync(string? keyword, bool? isActive, int pageIndex, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeOrganizationService : IOrganizationService
    {
        private readonly Guid _tenantId;
        private readonly Guid _organizationId;
        private readonly bool _isActive;

        public FakeOrganizationService(Guid tenantId, Guid organizationId, bool isActive = true)
        {
            _tenantId = tenantId;
            _organizationId = organizationId;
            _isActive = isActive;
        }

        public Task<OrganizationDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganizationDetailResponse?>(id == _organizationId
                ? new OrganizationDetailResponse
                {
                    Id = _organizationId,
                    TenantId = _tenantId,
                    Code = "ORG1",
                    Name = "Org",
                    Type = OrganizationType.Department,
                    IsActive = _isActive
                }
                : null);

        public Task<Guid> CreateAsync(Guid tenantId, Guid? parentOrganizationId, string code, string name, string? description, OrganizationType type, int sortOrder, string? metadata, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(Guid id, string name, string? description, OrganizationType type, int sortOrder, string? metadata, Guid? parentOrganizationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task ActivateAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<OrganizationListItemResponse>> GetListAsync(Guid? tenantId, Guid? parentOrganizationId, string? keyword, bool? isActive, OrganizationType? type, int pageIndex, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<OrganizationTreeNodeResponse>> GetTreeAsync(Guid tenantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
