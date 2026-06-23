using BackgroundJobs.Domain.Constants;
using BackgroundJobs.Domain.JobExecutions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackgroundJobs.Infrastructure.Persistence.Configurations;

public sealed class BackgroundJobExecutionConfiguration : IEntityTypeConfiguration<BackgroundJobExecution>
{
    public void Configure(EntityTypeBuilder<BackgroundJobExecution> builder)
    {
        builder.ToTable("job_executions", BackgroundJobsConstants.SchemaName);

        builder.HasKey(execution => execution.Id);

        builder.Property(execution => execution.JobName).HasMaxLength(200).IsRequired();
        builder.Property(execution => execution.JobType).HasMaxLength(100).IsRequired();
        builder.Property(execution => execution.Status).HasMaxLength(50).IsRequired();
        builder.Property(execution => execution.StartedAt).IsRequired();
        builder.Property(execution => execution.TriggeredBy).HasMaxLength(200);
        builder.Property(execution => execution.TriggerSource).HasMaxLength(50).IsRequired();
        builder.Property(execution => execution.Parameters).HasColumnType("jsonb");
        builder.Property(execution => execution.ResultMessage).HasMaxLength(2000);
        builder.Property(execution => execution.ErrorMessage).HasMaxLength(2000);
        builder.Property(execution => execution.ErrorDetails).HasMaxLength(8000);
        builder.Property(execution => execution.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(execution => execution.CreatedBy).HasColumnName("created_by");
        builder.Property(execution => execution.UpdatedAt).HasColumnName("updated_at");
        builder.Property(execution => execution.UpdatedBy).HasColumnName("updated_by");
        builder.Property(execution => execution.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(execution => execution.DeletedAt).HasColumnName("deleted_at");
        builder.Property(execution => execution.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(execution => execution.JobName);
        builder.HasIndex(execution => execution.JobType);
        builder.HasIndex(execution => execution.Status);
        builder.HasIndex(execution => execution.TriggerSource);
        builder.HasIndex(execution => execution.StartedAt);
        builder.HasIndex(execution => execution.IsDeleted);
    }
}
