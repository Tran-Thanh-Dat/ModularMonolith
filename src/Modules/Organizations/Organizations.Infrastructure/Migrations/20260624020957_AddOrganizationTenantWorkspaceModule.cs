using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Organizations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationTenantWorkspaceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organizations");

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organizations_organizations_ParentOrganizationId",
                        column: x => x.ParentOrganizationId,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organizations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "organizations",
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_users",
                schema: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_organization_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_users_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_users_tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "organizations",
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                schema: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workspaces_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organizations",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workspaces_tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "organizations",
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workspace_users",
                schema: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_workspace_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workspace_users_tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "organizations",
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workspace_users_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalSchema: "organizations",
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_users_OrganizationId",
                schema: "organizations",
                table: "organization_users",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_users_OrganizationId_UserId",
                schema: "organizations",
                table: "organization_users",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_organization_users_TenantId",
                schema: "organizations",
                table: "organization_users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_users_TenantId_UserId_IsDefault",
                schema: "organizations",
                table: "organization_users",
                columns: new[] { "TenantId", "UserId", "IsDefault" },
                unique: true,
                filter: "\"is_deleted\" = false AND \"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_organization_users_UserId",
                schema: "organizations",
                table: "organization_users",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_IsActive",
                schema: "organizations",
                table: "organizations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_ParentOrganizationId",
                schema: "organizations",
                table: "organizations",
                column: "ParentOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_SortOrder",
                schema: "organizations",
                table: "organizations",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_TenantId",
                schema: "organizations",
                table: "organizations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_TenantId_Code",
                schema: "organizations",
                table: "organizations",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Type",
                schema: "organizations",
                table: "organizations",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_Code",
                schema: "organizations",
                table: "tenants",
                column: "Code",
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_created_at",
                schema: "organizations",
                table: "tenants",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_IsActive",
                schema: "organizations",
                table: "tenants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_users_TenantId",
                schema: "organizations",
                table: "workspace_users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_users_UserId",
                schema: "organizations",
                table: "workspace_users",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_users_WorkspaceId",
                schema: "organizations",
                table: "workspace_users",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_users_WorkspaceId_UserId",
                schema: "organizations",
                table: "workspace_users",
                columns: new[] { "WorkspaceId", "UserId" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_IsActive",
                schema: "organizations",
                table: "workspaces",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_OrganizationId",
                schema: "organizations",
                table: "workspaces",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_TenantId",
                schema: "organizations",
                table: "workspaces",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_TenantId_Code",
                schema: "organizations",
                table: "workspaces",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "\"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_users",
                schema: "organizations");

            migrationBuilder.DropTable(
                name: "workspace_users",
                schema: "organizations");

            migrationBuilder.DropTable(
                name: "workspaces",
                schema: "organizations");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "organizations");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "organizations");
        }
    }
}
