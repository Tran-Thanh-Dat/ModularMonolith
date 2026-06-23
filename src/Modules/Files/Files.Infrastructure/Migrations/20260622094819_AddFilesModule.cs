using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Files.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFilesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "files");

            migrationBuilder.CreateTable(
                name: "file_resources",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PublicUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ModuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsTemporary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_file_resources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_file_resources_ContentType",
                schema: "files",
                table: "file_resources",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_file_resources_created_at",
                schema: "files",
                table: "file_resources",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_file_resources_FileExtension",
                schema: "files",
                table: "file_resources",
                column: "FileExtension");

            migrationBuilder.CreateIndex(
                name: "IX_file_resources_is_deleted",
                schema: "files",
                table: "file_resources",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_file_resources_IsActive",
                schema: "files",
                table: "file_resources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_file_resources_IsTemporary",
                schema: "files",
                table: "file_resources",
                column: "IsTemporary");

            migrationBuilder.CreateIndex(
                name: "IX_file_resources_ModuleName",
                schema: "files",
                table: "file_resources",
                column: "ModuleName");

            migrationBuilder.CreateIndex(
                name: "IX_file_resources_ReferenceId",
                schema: "files",
                table: "file_resources",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_file_resources_ReferenceType",
                schema: "files",
                table: "file_resources",
                column: "ReferenceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_resources",
                schema: "files");
        }
    }
}
