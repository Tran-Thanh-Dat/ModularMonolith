using AuditLogs.Domain.ActivityLogs;
using AuditLogs.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditLogs.Infrastructure.Persistence.Configurations;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs", AuditLogConstants.SchemaName);

        builder.HasKey(activityLog => activityLog.Id);

        builder.Property(activityLog => activityLog.UserId).HasColumnName("user_id");
        builder.Property(activityLog => activityLog.UserName).HasColumnName("user_name").HasMaxLength(255);
        builder.Property(activityLog => activityLog.ActivityType).HasColumnName("activity_type").HasMaxLength(100).IsRequired();
        builder.Property(activityLog => activityLog.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
        builder.Property(activityLog => activityLog.ModuleName).HasColumnName("module_name").HasMaxLength(100).IsRequired();
        builder.Property(activityLog => activityLog.RequestPath).HasColumnName("request_path").HasMaxLength(500);
        builder.Property(activityLog => activityLog.HttpMethod).HasColumnName("http_method").HasMaxLength(20);
        builder.Property(activityLog => activityLog.IpAddress).HasColumnName("ip_address").HasMaxLength(100);
        builder.Property(activityLog => activityLog.UserAgent).HasColumnName("user_agent").HasColumnType("text");
        builder.Property(activityLog => activityLog.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(activityLog => activityLog.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(activityLog => activityLog.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(activityLog => activityLog.UserId);
        builder.HasIndex(activityLog => activityLog.ActivityType);
        builder.HasIndex(activityLog => activityLog.CreatedAt).IsDescending();
        builder.HasIndex(activityLog => activityLog.ModuleName);
    }
}
