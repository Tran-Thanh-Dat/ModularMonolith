using BuildingBlocks.Application.Abstractions;
using MasterData.Application.Abstractions;
using MasterData.Domain.Entities;
using MasterData.Domain.Enums;
using MasterData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MasterData.Infrastructure.Seeders;

public sealed class MasterDataSeeder : IMasterDataSeeder
{
    private readonly MasterDataDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<MasterDataSeeder> _logger;

    public MasterDataSeeder(
        MasterDataDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<MasterDataSeeder> logger)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        foreach (var definition in SystemDefinitions)
        {
            var group = await _dbContext.MasterDataGroups
                .FirstOrDefaultAsync(
                    g => !g.IsDeleted &&
                         g.Scope == MasterDataScope.Global &&
                         g.TenantId == null &&
                         g.OrganizationId == null &&
                         g.Code == definition.Code,
                    cancellationToken);

            if (group is null)
            {
                group = MasterDataGroup.Create(
                    definition.Code,
                    definition.Name,
                    definition.Description,
                    MasterDataScope.Global,
                    null,
                    null,
                    isSystem: true,
                    definition.SortOrder,
                    null,
                    now);

                await _dbContext.MasterDataGroups.AddAsync(group, cancellationToken);
                _logger.LogInformation("Seeded master data group {Code}", definition.Code);
            }

            foreach (var itemDef in definition.Items)
            {
                var exists = await _dbContext.MasterDataItems
                    .AnyAsync(
                        i => !i.IsDeleted && i.GroupId == group.Id && i.Code == itemDef.Code,
                        cancellationToken);

                if (exists)
                {
                    continue;
                }

                var item = MasterDataItem.Create(
                    group.Id,
                    itemDef.Code,
                    itemDef.Name,
                    null,
                    null,
                    null,
                    isSystem: true,
                    itemDef.IsDefault,
                    itemDef.SortOrder,
                    null,
                    null,
                    null,
                    now);

                await _dbContext.MasterDataItems.AddAsync(item, cancellationToken);
                _logger.LogInformation("Seeded master data item {Group}/{Code}", definition.Code, itemDef.Code);
            }

            if (definition.DefaultCode is not null)
            {
                var defaultItem = await _dbContext.MasterDataItems
                    .FirstOrDefaultAsync(
                        i => !i.IsDeleted && i.GroupId == group.Id && i.Code == definition.DefaultCode,
                        cancellationToken);

                if (defaultItem is not null && !defaultItem.IsDefault)
                {
                    var others = await _dbContext.MasterDataItems
                        .Where(i => i.GroupId == group.Id && !i.IsDeleted && i.IsDefault && i.Id != defaultItem.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var other in others)
                    {
                        other.SetDefault(false);
                    }

                    defaultItem.SetDefault(true);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("MasterData seed completed.");
    }

    private static IReadOnlyList<GroupSeedDefinition> SystemDefinitions { get; } =
    [
        new("USER_STATUS", "User Status", "System user status lookup", 1, "ACTIVE",
        [
            Item("ACTIVE", "Active", 1, true),
            Item("INACTIVE", "Inactive", 2),
            Item("LOCKED", "Locked", 3),
            Item("PENDING_VERIFICATION", "Pending Verification", 4)
        ]),
        new("FILE_TYPE", "File Type", "System file type lookup", 2, null,
        [
            Item("IMAGE", "Image", 1),
            Item("DOCUMENT", "Document", 2),
            Item("SPREADSHEET", "Spreadsheet", 3),
            Item("PDF", "PDF", 4),
            Item("AUDIO", "Audio", 5),
            Item("VIDEO", "Video", 6),
            Item("ARCHIVE", "Archive", 7),
            Item("OTHER", "Other", 8)
        ]),
        new("APPROVAL_STATUS", "Approval Status", "System approval status lookup", 3, "DRAFT",
        [
            Item("DRAFT", "Draft", 1, true),
            Item("PENDING", "Pending", 2),
            Item("APPROVED", "Approved", 3),
            Item("REJECTED", "Rejected", 4),
            Item("CANCELLED", "Cancelled", 5)
        ]),
        new("PRIORITY", "Priority", "System priority lookup", 4, "MEDIUM",
        [
            Item("LOW", "Low", 1),
            Item("MEDIUM", "Medium", 2, true),
            Item("HIGH", "High", 3),
            Item("CRITICAL", "Critical", 4)
        ]),
        new("LANGUAGE", "Language", "System language lookup", 5, "VI",
        [
            Item("VI", "Vietnamese", 1, true),
            Item("EN", "English", 2),
            Item("JA", "Japanese", 3)
        ]),
        new("JOB_STATUS", "Job Status", "System background job status lookup", 6, null,
        [
            Item("QUEUED", "Queued", 1),
            Item("PROCESSING", "Processing", 2),
            Item("SUCCEEDED", "Succeeded", 3),
            Item("FAILED", "Failed", 4),
            Item("CANCELLED", "Cancelled", 5)
        ]),
        new("IMPORT_STATUS", "Import Status", "System import status lookup", 7, null,
        [
            Item("PENDING", "Pending", 1),
            Item("PROCESSING", "Processing", 2),
            Item("COMPLETED", "Completed", 3),
            Item("COMPLETED_WITH_ERRORS", "Completed With Errors", 4),
            Item("FAILED", "Failed", 5)
        ]),
        new("EXPORT_STATUS", "Export Status", "System export status lookup", 8, null,
        [
            Item("PENDING", "Pending", 1),
            Item("PROCESSING", "Processing", 2),
            Item("COMPLETED", "Completed", 3),
            Item("FAILED", "Failed", 4)
        ])
    ];

    private static ItemSeedDefinition Item(string code, string name, int sortOrder, bool isDefault = false) =>
        new(code, name, sortOrder, isDefault);

    private sealed record GroupSeedDefinition(
        string Code,
        string Name,
        string Description,
        int SortOrder,
        string? DefaultCode,
        IReadOnlyList<ItemSeedDefinition> Items);

    private sealed record ItemSeedDefinition(string Code, string Name, int SortOrder, bool IsDefault = false);
}
