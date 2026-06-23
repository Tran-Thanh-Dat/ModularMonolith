namespace AuditLogs.Domain.Constants;

public static class AuditLogConstants
{
    public const string SchemaName = "audit";

    public static class Modules
    {
        public const string Identity = "Identity";
        public const string Users = "Users";
        public const string AuditLogs = "AuditLogs";
        public const string Categories = "Categories";
        public const string Files = "Files";
        public const string Notifications = "Notifications";
        public const string BackgroundJobs = "BackgroundJobs";
        public const string Settings = "Settings";
    }

    public static class Actions
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
    }

    public static class ExcludedEntityTypes
    {
        public static readonly HashSet<string> Names = new(StringComparer.Ordinal)
        {
            "AuditLog",
            "ActivityLog",
            "UserRefreshToken",
            "PasswordResetToken",
            "RoleUser",
            "PermissionUser"
        };
    }
}
