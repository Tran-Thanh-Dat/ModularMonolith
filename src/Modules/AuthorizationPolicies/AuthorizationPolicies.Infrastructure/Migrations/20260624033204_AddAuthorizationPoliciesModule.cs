using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthorizationPolicies.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationPoliciesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "authorization");

            migrationBuilder.CreateTable(
                name: "authorization_matrix_entries",
                schema: "authorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    RequiredPermissionCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_authorization_matrix_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permission_policies",
                schema: "authorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PermissionCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Effect = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Conditions = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_permission_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_permission_policies",
                schema: "authorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_role_permission_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_permission_policies_permission_policies_PermissionPoli~",
                        column: x => x.PermissionPolicyId,
                        principalSchema: "authorization",
                        principalTable: "permission_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_permission_policy_overrides",
                schema: "authorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Effect = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_user_permission_policy_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_permission_policy_overrides_permission_policies_Permis~",
                        column: x => x.PermissionPolicyId,
                        principalSchema: "authorization",
                        principalTable: "permission_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_authorization_matrix_entries_Action",
                schema: "authorization",
                table: "authorization_matrix_entries",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_matrix_entries_IsEnabled",
                schema: "authorization",
                table: "authorization_matrix_entries",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_matrix_entries_ModuleCode",
                schema: "authorization",
                table: "authorization_matrix_entries",
                column: "ModuleCode");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_matrix_entries_ModuleCode_ResourceType_Action~",
                schema: "authorization",
                table: "authorization_matrix_entries",
                columns: new[] { "ModuleCode", "ResourceType", "Action", "Scope" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_matrix_entries_RequiredPermissionCode",
                schema: "authorization",
                table: "authorization_matrix_entries",
                column: "RequiredPermissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_matrix_entries_ResourceType",
                schema: "authorization",
                table: "authorization_matrix_entries",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_matrix_entries_Scope",
                schema: "authorization",
                table: "authorization_matrix_entries",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_permission_policies_Action",
                schema: "authorization",
                table: "permission_policies",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_permission_policies_Code",
                schema: "authorization",
                table: "permission_policies",
                column: "Code",
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_permission_policies_Effect",
                schema: "authorization",
                table: "permission_policies",
                column: "Effect");

            migrationBuilder.CreateIndex(
                name: "IX_permission_policies_IsActive",
                schema: "authorization",
                table: "permission_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_permission_policies_ModuleCode",
                schema: "authorization",
                table: "permission_policies",
                column: "ModuleCode");

            migrationBuilder.CreateIndex(
                name: "IX_permission_policies_PermissionCode",
                schema: "authorization",
                table: "permission_policies",
                column: "PermissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_permission_policies_ResourceType",
                schema: "authorization",
                table: "permission_policies",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_permission_policies_Scope",
                schema: "authorization",
                table: "permission_policies",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_role_permission_policies_IsActive",
                schema: "authorization",
                table: "role_permission_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_role_permission_policies_PermissionPolicyId",
                schema: "authorization",
                table: "role_permission_policies",
                column: "PermissionPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permission_policies_RoleId",
                schema: "authorization",
                table: "role_permission_policies",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permission_policies_RoleId_PermissionPolicyId",
                schema: "authorization",
                table: "role_permission_policies",
                columns: new[] { "RoleId", "PermissionPolicyId" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_policy_overrides_Effect",
                schema: "authorization",
                table: "user_permission_policy_overrides",
                column: "Effect");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_policy_overrides_ExpiresAt",
                schema: "authorization",
                table: "user_permission_policy_overrides",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_policy_overrides_IsActive",
                schema: "authorization",
                table: "user_permission_policy_overrides",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_policy_overrides_PermissionPolicyId",
                schema: "authorization",
                table: "user_permission_policy_overrides",
                column: "PermissionPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_policy_overrides_UserId",
                schema: "authorization",
                table: "user_permission_policy_overrides",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_policy_overrides_UserId_PermissionPolicyId",
                schema: "authorization",
                table: "user_permission_policy_overrides",
                columns: new[] { "UserId", "PermissionPolicyId" },
                unique: true,
                filter: "\"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authorization_matrix_entries",
                schema: "authorization");

            migrationBuilder.DropTable(
                name: "role_permission_policies",
                schema: "authorization");

            migrationBuilder.DropTable(
                name: "user_permission_policy_overrides",
                schema: "authorization");

            migrationBuilder.DropTable(
                name: "permission_policies",
                schema: "authorization");
        }
    }
}
