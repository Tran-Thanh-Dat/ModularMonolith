using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BuildingBlocks.Infrastructure.Persistence;

public abstract class AuditableDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    protected AuditableDbContext(
        DbContextOptions options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    private void ApplyAuditInformation()
    {
        var now = _dateTimeProvider.UtcNow;
        var userId = _currentUserService.UserId;

        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ApplyCreated(entry, userId, now);
                    break;
                case EntityState.Modified:
                    ApplyUpdated(entry, userId, now);
                    break;
            }
        }
    }

    private static void ApplyCreated(EntityEntry entry, Guid? userId, DateTimeOffset now)
    {
        if (entry.Entity is BaseEntity baseEntity)
        {
            if (baseEntity.CreatedAt == default)
            {
                baseEntity.SetCreated(userId, now);
            }
        }
    }

    private static void ApplyUpdated(EntityEntry entry, Guid? userId, DateTimeOffset now)
    {
        if (entry.Entity is BaseEntity baseEntity)
        {
            baseEntity.SetUpdated(userId, now);
        }

        if (entry.Entity is SoftDeleteEntity softDeleteEntity &&
            entry.Property(nameof(SoftDeleteEntity.IsDeleted)).IsModified &&
            softDeleteEntity.IsDeleted)
        {
            softDeleteEntity.MarkDeleted(userId, now);
        }
    }
}
