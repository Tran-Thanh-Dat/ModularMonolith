using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditLogs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10AuditAndActivityLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_logs_correlation_id",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_Module",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "Module",
                schema: "audit",
                table: "audit_logs",
                newName: "module_name");

            migrationBuilder.AddColumn<string>(
                name: "changed_columns",
                schema: "audit",
                table: "audit_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "http_method",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_path",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_module_name",
                schema: "audit",
                table: "audit_logs",
                column: "module_name");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityName",
                schema: "audit",
                table: "audit_logs",
                column: "entity_name");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityId",
                schema: "audit",
                table: "audit_logs",
                column: "entity_id");

            migrationBuilder.CreateTable(
                name: "activity_logs",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    activity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    module_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    request_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    http_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_activity_type",
                schema: "audit",
                table: "activity_logs",
                column: "activity_type");

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_created_at",
                schema: "audit",
                table: "activity_logs",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_module_name",
                schema: "audit",
                table: "activity_logs",
                column: "module_name");

            migrationBuilder.CreateIndex(
                name: "IX_activity_logs_user_id",
                schema: "audit",
                table: "activity_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_logs",
                schema: "audit");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_module_name",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_EntityName",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_EntityId",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "changed_columns",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "error_message",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "http_method",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "request_path",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "module_name",
                schema: "audit",
                table: "audit_logs",
                newName: "Module");

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_correlation_id",
                schema: "audit",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_Module",
                schema: "audit",
                table: "audit_logs",
                column: "Module");
        }
    }
}
