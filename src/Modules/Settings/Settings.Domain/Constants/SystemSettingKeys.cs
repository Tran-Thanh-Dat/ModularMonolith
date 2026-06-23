namespace Settings.Domain.Constants;

public static class SystemSettingKeys
{
    public static class Password
    {
        public const string MinimumLength = "Security.Password.MinimumLength";
        public const string RequireUppercase = "Security.Password.RequireUppercase";
        public const string RequireLowercase = "Security.Password.RequireLowercase";
        public const string RequireDigit = "Security.Password.RequireDigit";
        public const string RequireSpecialCharacter = "Security.Password.RequireSpecialCharacter";
        public const string ExpirationDays = "Security.Password.ExpirationDays";
        public const string PreventReuseCount = "Security.Password.PreventReuseCount";
    }

    public static class Login
    {
        public const string MaxFailedAttempts = "Security.Login.MaxFailedAttempts";
        public const string LockoutDurationMinutes = "Security.Login.LockoutDurationMinutes";
        public const string EnableLockout = "Security.Login.EnableLockout";
        public const string RequireConfirmedEmail = "Security.Login.RequireConfirmedEmail";
    }

    public static class Session
    {
        public const string AccessTokenExpirationMinutes = "Security.Session.AccessTokenExpirationMinutes";
        public const string RefreshTokenExpirationDays = "Security.Session.RefreshTokenExpirationDays";
        public const string SessionTimeoutMinutes = "Security.Session.SessionTimeoutMinutes";
        public const string RefreshTokenReuseDetectionEnabled = "Security.Session.RefreshTokenReuseDetectionEnabled";
    }

    public static class FileStorage
    {
        public const string MaxFileSizeMb = "FileStorage.MaxFileSizeMb";
        public const string AllowedExtensions = "FileStorage.AllowedExtensions";
        public const string AllowedContentTypes = "FileStorage.AllowedContentTypes";
    }

    public static class Notification
    {
        public const string EmailEnabled = "Notification.Email.Enabled";
        public const string InAppEnabled = "Notification.InApp.Enabled";
    }

    public static class Maintenance
    {
        public const string Enabled = "Maintenance.Enabled";
        public const string Message = "Maintenance.Message";
        public const string StartAt = "Maintenance.StartAt";
        public const string EndAt = "Maintenance.EndAt";
        public const string AllowAdminBypass = "Maintenance.AllowAdminBypass";
    }
}
