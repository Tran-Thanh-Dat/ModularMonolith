using AuditLogs.Domain.ActivityLogs;
using AuditLogs.Domain.AuditLogs;
using AuditLogs.Domain.Constants;
using AuditLogs.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AuditLogs.Infrastructure.Persistence;

public sealed class AuditLogsDbContext : DbContext
{
    public AuditLogsDbContext(DbContextOptions<AuditLogsDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(AuditLogConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditLogsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
