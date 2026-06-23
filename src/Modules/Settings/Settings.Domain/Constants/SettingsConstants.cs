namespace Settings.Domain.Constants;

public static class SettingsConstants
{
    public const string SchemaName = "settings";
}

public static class SettingDataTypes
{
    public const string String = "String";
    public const string Number = "Number";
    public const string Boolean = "Boolean";
    public const string Decimal = "Decimal";
    public const string Json = "Json";
    public const string TimeSpan = "TimeSpan";
    public const string Email = "Email";
    public const string Url = "Url";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        String, Number, Boolean, Decimal, Json, TimeSpan, Email, Url
    };
}

public static class SettingGroups
{
    public const string System = "System";
    public const string Security = "Security";
    public const string PasswordPolicy = "PasswordPolicy";
    public const string LoginPolicy = "LoginPolicy";
    public const string SessionPolicy = "SessionPolicy";
    public const string Notification = "Notification";
    public const string FileStorage = "FileStorage";
    public const string Maintenance = "Maintenance";
    public const string Business = "Business";
}
