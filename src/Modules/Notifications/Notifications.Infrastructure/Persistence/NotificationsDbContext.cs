using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Infrastructure.Persistence;
using Notifications.Domain.EmailMessages;
using Notifications.Domain.EmailTemplates;
using Notifications.Domain.Constants;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext : AuditableDbContext
{
    public NotificationsDbContext(
        DbContextOptions<NotificationsDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options, currentUserService, dateTimeProvider)
    {
    }

    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(NotificationsConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmailMessageConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class NotificationsUnitOfWork : EfUnitOfWork<NotificationsDbContext>
{
    public NotificationsUnitOfWork(NotificationsDbContext dbContext)
        : base(dbContext)
    {
    }
}
