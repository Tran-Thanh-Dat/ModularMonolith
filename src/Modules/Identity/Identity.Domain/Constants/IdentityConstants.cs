namespace Identity.Domain.Constants;

public static class IdentityConstants
{
    public const string SchemaName = "identity";

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";
    }

    public static class Permissions
    {
        public const string UsersView = "Users.View";
        public const string UsersCreate = "Users.Create";
        public const string UsersUpdate = "Users.Update";
        public const string UsersDelete = "Users.Delete";
        public const string UsersActivate = "Users.Activate";
        public const string UsersDeactivate = "Users.Deactivate";
        public const string UsersAssignRole = "Users.AssignRole";
        public const string UsersAssignPermission = "Users.AssignPermission";
        public const string AuditLogsView = "AuditLogs.View";
        public const string AuditLogView = "AuditLog.View";
        public const string ActivityLogView = "ActivityLog.View";
    }
}
