using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackgroundJobs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundJobsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "background_jobs");

            migrationBuilder.CreateTable(
                name: "job_executions",
                schema: "background_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JobType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    TriggeredBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TriggerSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Parameters = table.Column<string>(type: "jsonb", nullable: true),
                    ResultMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorDetails = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_executions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_executions_is_deleted",
                schema: "background_jobs",
                table: "job_executions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_job_executions_JobName",
                schema: "background_jobs",
                table: "job_executions",
                column: "JobName");

            migrationBuilder.CreateIndex(
                name: "IX_job_executions_JobType",
                schema: "background_jobs",
                table: "job_executions",
                column: "JobType");

            migrationBuilder.CreateIndex(
                name: "IX_job_executions_StartedAt",
                schema: "background_jobs",
                table: "job_executions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_job_executions_Status",
                schema: "background_jobs",
                table: "job_executions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_job_executions_TriggerSource",
                schema: "background_jobs",
                table: "job_executions",
                column: "TriggerSource");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_executions",
                schema: "background_jobs");
        }
    }
}
