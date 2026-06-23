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
