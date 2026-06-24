using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDataModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "master_data");

            migrationBuilder.CreateTable(
                name: "master_data_groups",
                schema: "master_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_master_data_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "master_data_items",
                schema: "master_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ParentItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_master_data_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_master_data_items_master_data_groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "master_data",
                        principalTable: "master_data_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_master_data_items_master_data_items_ParentItemId",
                        column: x => x.ParentItemId,
                        principalSchema: "master_data",
                        principalTable: "master_data_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_master_data_groups_Code",
                schema: "master_data",
                table: "master_data_groups",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_groups_created_at",
                schema: "master_data",
                table: "master_data_groups",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_groups_IsActive",
                schema: "master_data",
                table: "master_data_groups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_groups_IsSystem",
                schema: "master_data",
                table: "master_data_groups",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_groups_OrganizationId",
                schema: "master_data",
                table: "master_data_groups",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_groups_Scope",
                schema: "master_data",
                table: "master_data_groups",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "ix_master_data_groups_scope_tenant_org_code",
                schema: "master_data",
                table: "master_data_groups",
                columns: new[] { "Scope", "TenantId", "OrganizationId", "Code" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_groups_TenantId",
                schema: "master_data",
                table: "master_data_groups",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_items_Code",
                schema: "master_data",
                table: "master_data_items",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_items_EffectiveFrom",
                schema: "master_data",
                table: "master_data_items",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_items_EffectiveTo",
                schema: "master_data",
                table: "master_data_items",
                column: "EffectiveTo");

            migrationBuilder.CreateIndex(
                name: "ix_master_data_items_group_code",
                schema: "master_data",
                table: "master_data_items",
                columns: new[] { "GroupId", "Code" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_master_data_items_group_default",
                schema: "master_data",
                table: "master_data_items",
                columns: new[] { "GroupId", "IsDefault" },
                unique: true,
                filter: "\"is_deleted\" = false AND \"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_items_GroupId",
                schema: "master_data",
                table: "master_data_items",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_items_IsActive",
                schema: "master_data",
                table: "master_data_items",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_items_IsDefault",
                schema: "master_data",
                table: "master_data_items",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_items_IsSystem",
                schema: "master_data",
                table: "master_data_items",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_items_ParentItemId",
                schema: "master_data",
                table: "master_data_items",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_master_data_items_SortOrder",
                schema: "master_data",
                table: "master_data_items",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "master_data_items",
                schema: "master_data");

            migrationBuilder.DropTable(
                name: "master_data_groups",
                schema: "master_data");
        }
    }
}
