namespace AuditLogs.Domain.Constants;

public static class ActivityTypes
{
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string RefreshToken = "RefreshToken";
    public const string View = "View";
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Activate = "Activate";
    public const string Deactivate = "Deactivate";
    public const string AssignRole = "AssignRole";
    public const string AssignPermission = "AssignPermission";
    public const string RemoveRole = "RemoveRole";
    public const string RemovePermission = "RemovePermission";
    public const string ChangePassword = "ChangePassword";
    public const string FailedLogin = "FailedLogin";
    public const string AuthorizationFailed = "AuthorizationFailed";
    public const string RefreshTokenReuseDetected = "RefreshTokenReuseDetected";
    public const string SystemError = "SystemError";
}
