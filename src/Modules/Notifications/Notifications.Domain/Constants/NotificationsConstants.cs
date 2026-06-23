namespace Notifications.Domain.Constants;

public static class NotificationsConstants
{
    public const string SchemaName = "notifications";
}

public static class EmailStatuses
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

public static class NotificationStatuses
{
    public const string New = "New";
    public const string Read = "Read";
    public const string Archived = "Archived";
}

public static class NotificationTypes
{
    public const string Info = "Info";
    public const string Success = "Success";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string System = "System";
}
