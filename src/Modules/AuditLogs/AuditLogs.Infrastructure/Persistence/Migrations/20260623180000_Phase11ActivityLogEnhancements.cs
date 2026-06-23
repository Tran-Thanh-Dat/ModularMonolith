using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditLogs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase11ActivityLogEnhancements : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Description",
            schema: "audit",
            table: "activity_logs",
            newName: "description");

        migrationBuilder.RenameColumn(
            name: "Status",
            schema: "audit",
            table: "activity_logs",
            newName: "status");

        migrationBuilder.AddColumn<string>(
            name: "error_message",
            schema: "audit",
            table: "activity_logs",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "error_message",
            schema: "audit",
            table: "activity_logs");

        migrationBuilder.RenameColumn(
            name: "description",
            schema: "audit",
            table: "activity_logs",
            newName: "Description");

        migrationBuilder.RenameColumn(
            name: "status",
            schema: "audit",
            table: "activity_logs",
            newName: "Status");
    }
}
