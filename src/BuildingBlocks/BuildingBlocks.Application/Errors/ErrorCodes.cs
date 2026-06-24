namespace BuildingBlocks.Application.Errors;

public static class CommonErrors
{
    public const string Success = "Common.Success";
    public const string UnknownError = "Common.UnknownError";
    public const string ValidationError = "Common.ValidationError";
    public const string NotFound = "Common.NotFound";
    public const string BadRequest = "Common.BadRequest";
    public const string Unauthorized = "Common.Unauthorized";
    public const string Forbidden = "Common.Forbidden";
    public const string Conflict = "Common.Conflict";
}

public static class AuthErrors
{
    public const string InvalidCredentials = "Auth.InvalidCredentials";
    public const string UserInactive = "Auth.UserInactive";
    public const string TokenExpired = "Auth.TokenExpired";
    public const string RefreshTokenInvalid = "Auth.RefreshTokenInvalid";
    public const string RefreshTokenReuseDetected = "Auth.RefreshTokenReuseDetected";
    public const string PermissionDenied = "Auth.PermissionDenied";
}

public static class AccountErrors
{
    public const string CurrentPasswordInvalid = "Account.CurrentPasswordInvalid";
    public const string PasswordResetTokenInvalid = "Account.PasswordResetTokenInvalid";
}

public static class UserErrors
{
    public const string NotFound = "User.NotFound";
    public const string EmailAlreadyExists = "User.EmailAlreadyExists";
    public const string UserNameAlreadyExists = "User.UserNameAlreadyExists";
    public const string CannotDeleteSelf = "User.CannotDeleteSelf";
}

public static class RoleErrors
{
    public const string NotFound = "Role.NotFound";
    public const string CodeAlreadyExists = "Role.CodeAlreadyExists";
    public const string PermissionInvalid = "Role.PermissionInvalid";
}

public static class PermissionErrors
{
    public const string NotFound = "Permission.NotFound";
    public const string InvalidCode = "Permission.InvalidCode";
    public const string AlreadyExists = "Permission.AlreadyExists";
}

public static class CategoryErrors
{
    public const string NotFound = "Category.NotFound";
    public const string CodeAlreadyExists = "Category.CodeAlreadyExists";
    public const string AlreadyDeleted = "Category.AlreadyDeleted";
    public const string AlreadyActive = "Category.AlreadyActive";
    public const string AlreadyInactive = "Category.AlreadyInactive";
}

public static class FileErrors
{
    public const string NotFound = "File.NotFound";
    public const string Empty = "File.Empty";
    public const string TooLarge = "File.TooLarge";
    public const string ExtensionNotAllowed = "File.ExtensionNotAllowed";
    public const string ContentTypeNotAllowed = "File.ContentTypeNotAllowed";
    public const string UploadFailed = "File.UploadFailed";
    public const string DownloadFailed = "File.DownloadFailed";
    public const string StorageProviderNotFound = "File.StorageProviderNotFound";
    public const string InvalidFileName = "File.InvalidFileName";
    public const string AlreadyDeleted = "File.AlreadyDeleted";
}

public static class EmailErrors
{
    public const string SendFailed = "Email.SendFailed";
    public const string InvalidRecipient = "Email.InvalidRecipient";
    public const string TemplateNotFound = "Email.TemplateNotFound";
    public const string TemplateCodeAlreadyExists = "Email.TemplateCodeAlreadyExists";
    public const string TemplateInactive = "Email.TemplateInactive";
    public const string MessageNotFound = "Email.MessageNotFound";
    public const string ProviderNotConfigured = "Email.ProviderNotConfigured";
    public const string InvalidTemplateData = "Email.InvalidTemplateData";
}

public static class NotificationErrors
{
    public const string NotFound = "Notification.NotFound";
    public const string UserRequired = "Notification.UserRequired";
    public const string Forbidden = "Notification.Forbidden";
    public const string ViewAllRequired = "Notification.ViewAllRequired";
    public const string ManageRequired = "Notification.ManageRequired";
    public const string AlreadyRead = "Notification.AlreadyRead";
    public const string AlreadyArchived = "Notification.AlreadyArchived";
}

public static class BackgroundJobErrors
{
    public const string NotFound = "BackgroundJob.NotFound";
    public const string Disabled = "BackgroundJob.Disabled";
    public const string RunFailed = "BackgroundJob.RunFailed";
    public const string InvalidJobName = "BackgroundJob.InvalidJobName";
    public const string DashboardForbidden = "BackgroundJob.DashboardForbidden";
    public const string AlreadyRunning = "BackgroundJob.AlreadyRunning";
}

public static class CacheErrors
{
    public const string ProviderNotConfigured = "Cache.ProviderNotConfigured";
    public const string SerializationFailed = "Cache.SerializationFailed";
    public const string ConnectionFailed = "Cache.ConnectionFailed";
}

public static class MonitoringErrors
{
    public const string HealthCheckFailed = "Monitoring.HealthCheckFailed";
    public const string DependencyUnavailable = "Monitoring.DependencyUnavailable";
    public const string Forbidden = "Monitoring.Forbidden";
    public const string InvalidConfiguration = "Monitoring.InvalidConfiguration";
}

public static class SettingErrors
{
    public const string NotFound = "Setting.NotFound";
    public const string KeyAlreadyExists = "Setting.KeyAlreadyExists";
    public const string InvalidKey = "Setting.InvalidKey";
    public const string InvalidGroup = "Setting.InvalidGroup";
    public const string InvalidDataType = "Setting.InvalidDataType";
    public const string InvalidValue = "Setting.InvalidValue";
    public const string NotEditable = "Setting.NotEditable";
    public const string SystemSettingCannotBeDeleted = "Setting.SystemSettingCannotBeDeleted";
    public const string SensitiveValueHidden = "Setting.SensitiveValueHidden";
    public const string EncryptedValueNotReadable = "Setting.EncryptedValueNotReadable";
}

public static class AccessPolicyErrors
{
    public const string InvalidPasswordPolicy = "AccessPolicy.InvalidPasswordPolicy";
    public const string InvalidLoginPolicy = "AccessPolicy.InvalidLoginPolicy";
    public const string InvalidSessionPolicy = "AccessPolicy.InvalidSessionPolicy";
    public const string InvalidMaintenancePolicy = "AccessPolicy.InvalidMaintenancePolicy";
}

public static class TenantErrors
{
    public const string NotFound = "Tenant.NotFound";
    public const string InvalidCode = "Tenant.InvalidCode";
    public const string InvalidName = "Tenant.InvalidName";
    public const string CodeAlreadyExists = "Tenant.CodeAlreadyExists";
    public const string AlreadyDeleted = "Tenant.AlreadyDeleted";
    public const string AlreadyActive = "Tenant.AlreadyActive";
    public const string AlreadyInactive = "Tenant.AlreadyInactive";
    public const string HasActiveDependencies = "Tenant.HasActiveDependencies";
}

public static class OrganizationErrors
{
    public const string NotFound = "Organization.NotFound";
    public const string InvalidCode = "Organization.InvalidCode";
    public const string InvalidName = "Organization.InvalidName";
    public const string InvalidTenant = "Organization.InvalidTenant";
    public const string InvalidSortOrder = "Organization.InvalidSortOrder";
    public const string CodeAlreadyExists = "Organization.CodeAlreadyExists";
    public const string AlreadyDeleted = "Organization.AlreadyDeleted";
    public const string AlreadyActive = "Organization.AlreadyActive";
    public const string AlreadyInactive = "Organization.AlreadyInactive";
    public const string ParentSelfReference = "Organization.ParentSelfReference";
    public const string ParentNotFound = "Organization.ParentNotFound";
    public const string ParentDifferentTenant = "Organization.ParentDifferentTenant";
    public const string ParentInactive = "Organization.ParentInactive";
    public const string CircularParent = "Organization.CircularParent";
    public const string TenantInactive = "Organization.TenantInactive";
    public const string HasActiveDependencies = "Organization.HasActiveDependencies";
    public const string Inactive = "Organization.Inactive";
}

public static class OrganizationUserErrors
{
    public const string NotFound = "OrganizationUser.NotFound";
    public const string InvalidUser = "OrganizationUser.InvalidUser";
    public const string UserNotFound = "OrganizationUser.UserNotFound";
    public const string AlreadyExists = "OrganizationUser.AlreadyExists";
    public const string AlreadyActive = "OrganizationUser.AlreadyActive";
    public const string AlreadyInactive = "OrganizationUser.AlreadyInactive";
    public const string AlreadyRemoved = "OrganizationUser.AlreadyRemoved";
    public const string TenantInactive = "OrganizationUser.TenantInactive";
    public const string OrganizationInactive = "OrganizationUser.OrganizationInactive";
}

public static class WorkspaceErrors
{
    public const string NotFound = "Workspace.NotFound";
    public const string InvalidCode = "Workspace.InvalidCode";
    public const string InvalidName = "Workspace.InvalidName";
    public const string InvalidTenant = "Workspace.InvalidTenant";
    public const string CodeAlreadyExists = "Workspace.CodeAlreadyExists";
    public const string AlreadyDeleted = "Workspace.AlreadyDeleted";
    public const string AlreadyActive = "Workspace.AlreadyActive";
    public const string AlreadyInactive = "Workspace.AlreadyInactive";
    public const string TenantInactive = "Workspace.TenantInactive";
    public const string OrganizationInactive = "Workspace.OrganizationInactive";
    public const string OrganizationDifferentTenant = "Workspace.OrganizationDifferentTenant";
    public const string OrganizationNotFound = "Workspace.OrganizationNotFound";
    public const string HasActiveDependencies = "Workspace.HasActiveDependencies";
    public const string Inactive = "Workspace.Inactive";
}

public static class WorkspaceUserErrors
{
    public const string NotFound = "WorkspaceUser.NotFound";
    public const string InvalidUser = "WorkspaceUser.InvalidUser";
    public const string UserNotFound = "WorkspaceUser.UserNotFound";
    public const string AlreadyExists = "WorkspaceUser.AlreadyExists";
    public const string AlreadyActive = "WorkspaceUser.AlreadyActive";
    public const string AlreadyInactive = "WorkspaceUser.AlreadyInactive";
    public const string AlreadyRemoved = "WorkspaceUser.AlreadyRemoved";
    public const string TenantInactive = "WorkspaceUser.TenantInactive";
    public const string WorkspaceInactive = "WorkspaceUser.WorkspaceInactive";
    public const string OrganizationMembershipRequired = "WorkspaceUser.OrganizationMembershipRequired";
}
