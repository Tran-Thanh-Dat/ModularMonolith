namespace AuditLogs.Application.Permissions;

public static class AuditLogsPermissionCodes
{
    public const string View = "AuditLog.View";

    /// <summary>Legacy permission from Phase 8 — kept for backward compatibility.</summary>
    public const string LegacyView = "AuditLogs.View";
}
