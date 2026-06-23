using AuditLogs.Domain.AuditLogs;
using AuditLogs.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuditLogs.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", AuditLogConstants.SchemaName);

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.UserId).HasColumnName("user_id");
        builder.Property(auditLog => auditLog.UserName).HasColumnName("user_name").HasMaxLength(255);
        builder.Property(auditLog => auditLog.Action).HasMaxLength(100).IsRequired();
        builder.Property(auditLog => auditLog.EntityName).HasColumnName("entity_name").HasMaxLength(255);
        builder.Property(auditLog => auditLog.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(auditLog => auditLog.ModuleName).HasColumnName("module_name").HasMaxLength(100).IsRequired();
        builder.Property(auditLog => auditLog.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(auditLog => auditLog.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        builder.Property(auditLog => auditLog.ChangedColumns).HasColumnName("changed_columns").HasColumnType("jsonb");
        builder.Property(auditLog => auditLog.RequestPath).HasColumnName("request_path").HasMaxLength(500);
        builder.Property(auditLog => auditLog.HttpMethod).HasColumnName("http_method").HasMaxLength(20);
        builder.Property(auditLog => auditLog.IpAddress).HasColumnName("ip_address").HasMaxLength(100);
        builder.Property(auditLog => auditLog.UserAgent).HasColumnName("user_agent").HasColumnType("text");
        builder.Property(auditLog => auditLog.Status).HasMaxLength(50).IsRequired();
        builder.Property(auditLog => auditLog.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(auditLog => auditLog.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(auditLog => auditLog.UserId);
        builder.HasIndex(auditLog => auditLog.EntityName);
        builder.HasIndex(auditLog => auditLog.EntityId);
        builder.HasIndex(auditLog => auditLog.Action);
        builder.HasIndex(auditLog => auditLog.CreatedAt).IsDescending();
        builder.HasIndex(auditLog => auditLog.ModuleName);
    }
}
