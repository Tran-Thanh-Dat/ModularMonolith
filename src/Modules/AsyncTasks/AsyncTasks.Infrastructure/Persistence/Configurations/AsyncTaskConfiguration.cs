using AsyncTasks.Domain.Constants;
using AsyncTasks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AsyncTasks.Infrastructure.Persistence.Configurations;

public sealed class AsyncTaskConfiguration : IEntityTypeConfiguration<AsyncTask>
{
    public void Configure(EntityTypeBuilder<AsyncTask> builder)
    {
        builder.ToTable("async_tasks", AsyncTasksConstants.SchemaName);
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TaskNo).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Type).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Status).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Payload).HasColumnType("jsonb");
        builder.Property(t => t.Result).HasColumnType("jsonb");
        builder.Property(t => t.QueueName).HasMaxLength(200);
        builder.Property(t => t.ConsumerName).HasMaxLength(200);
        builder.Property(t => t.LastErrorCode).HasMaxLength(200);
        builder.Property(t => t.LastErrorMessage).HasMaxLength(2000);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(t => t.TaskNo).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.MessageId);
        builder.HasIndex(t => t.CorrelationId);
        builder.HasIndex(t => t.RequestedByUserId);
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.OrganizationId);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => t.StartedAt);
        builder.HasIndex(t => t.CompletedAt);
        builder.HasIndex(t => t.FailedAt);
    }
}
