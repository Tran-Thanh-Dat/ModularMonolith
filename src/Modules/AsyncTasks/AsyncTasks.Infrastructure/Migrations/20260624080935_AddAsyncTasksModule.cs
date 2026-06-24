using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsyncTasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAsyncTasksModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "async_tasks");

            migrationBuilder.CreateTable(
                name: "async_tasks",
                schema: "async_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    Result = table.Column<string>(type: "jsonb", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetryCount = table.Column<int>(type: "integer", nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    QueueName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConsumerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_async_tasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_CompletedAt",
                schema: "async_tasks",
                table: "async_tasks",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_CorrelationId",
                schema: "async_tasks",
                table: "async_tasks",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_created_at",
                schema: "async_tasks",
                table: "async_tasks",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_FailedAt",
                schema: "async_tasks",
                table: "async_tasks",
                column: "FailedAt");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_MessageId",
                schema: "async_tasks",
                table: "async_tasks",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_OrganizationId",
                schema: "async_tasks",
                table: "async_tasks",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_RequestedByUserId",
                schema: "async_tasks",
                table: "async_tasks",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_StartedAt",
                schema: "async_tasks",
                table: "async_tasks",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_Status",
                schema: "async_tasks",
                table: "async_tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_TaskNo",
                schema: "async_tasks",
                table: "async_tasks",
                column: "TaskNo",
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_TenantId",
                schema: "async_tasks",
                table: "async_tasks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_async_tasks_Type",
                schema: "async_tasks",
                table: "async_tasks",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "async_tasks",
                schema: "async_tasks");
        }
    }
}
