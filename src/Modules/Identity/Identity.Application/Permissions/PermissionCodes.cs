namespace Identity.Application.Permissions;

public static class PermissionCodes
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
    public const string CategoryView = "Category.View";
    public const string CategoryCreate = "Category.Create";
    public const string CategoryUpdate = "Category.Update";
    public const string CategoryDelete = "Category.Delete";
    public const string CategoryActivate = "Category.Activate";
    public const string CategoryDeactivate = "Category.Deactivate";
    public const string FileView = "File.View";
    public const string FileUpload = "File.Upload";
    public const string FileDownload = "File.Download";
    public const string FileDelete = "File.Delete";
    public const string FileMarkPermanent = "File.MarkPermanent";
    public const string EmailView = "Email.View";
    public const string EmailSend = "Email.Send";
    public const string EmailTemplateView = "Email.TemplateView";
    public const string EmailTemplateCreate = "Email.TemplateCreate";
    public const string EmailTemplateUpdate = "Email.TemplateUpdate";
    public const string EmailTemplateDelete = "Email.TemplateDelete";
    public const string EmailTemplateActivate = "Email.TemplateActivate";
    public const string EmailTemplateDeactivate = "Email.TemplateDeactivate";
    public const string NotificationView = "Notification.View";
    public const string NotificationViewAll = "Notification.ViewAll";
    public const string NotificationManage = "Notification.Manage";
    public const string NotificationCreate = "Notification.Create";
    public const string NotificationMarkRead = "Notification.MarkRead";
    public const string NotificationArchive = "Notification.Archive";
    public const string BackgroundJobView = "BackgroundJob.View";
    public const string BackgroundJobRun = "BackgroundJob.Run";
    public const string BackgroundJobDashboard = "BackgroundJob.Dashboard";
    public const string MonitoringHealthView = "Monitoring.HealthView";
    public const string MonitoringSystemInfoView = "Monitoring.SystemInfoView";
    public const string SettingView = "Setting.View";
    public const string SettingCreate = "Setting.Create";
    public const string SettingUpdate = "Setting.Update";
    public const string SettingDelete = "Setting.Delete";
    public const string SettingActivate = "Setting.Activate";
    public const string SettingDeactivate = "Setting.Deactivate";
    public const string SettingViewSensitive = "Setting.ViewSensitive";
    public const string AccessPolicyView = "AccessPolicy.View";
    public const string AccessPolicyUpdate = "AccessPolicy.Update";
    public const string MaintenanceView = "Maintenance.View";
    public const string MaintenanceUpdate = "Maintenance.Update";
    public const string TenantView = "Tenant.View";
    public const string TenantManage = "Tenant.Manage";
    public const string OrganizationView = "Organization.View";
    public const string OrganizationManage = "Organization.Manage";
    public const string WorkspaceView = "Workspace.View";
    public const string WorkspaceManage = "Workspace.Manage";
    public const string PermissionPolicyView = "PermissionPolicy.View";
    public const string PermissionPolicyManage = "PermissionPolicy.Manage";
    public const string AuthorizationMatrixView = "AuthorizationMatrix.View";
    public const string AuthorizationMatrixManage = "AuthorizationMatrix.Manage";
    public const string AuthorizationCheckExecute = "AuthorizationCheck.Execute";
    public const string AuthorizationCheckExplain = "AuthorizationCheck.Explain";
    public const string MasterDataGroupView = "MasterDataGroup.View";
    public const string MasterDataGroupManage = "MasterDataGroup.Manage";
    public const string MasterDataItemView = "MasterDataItem.View";
    public const string MasterDataItemManage = "MasterDataItem.Manage";
    public const string LookupView = "Lookup.View";
    public const string AsyncTaskView = "AsyncTask.View";
    public const string AsyncTaskSubmit = "AsyncTask.Submit";
    public const string AsyncTaskCancel = "AsyncTask.Cancel";
    public const string AsyncTaskRetry = "AsyncTask.Retry";
    public const string AsyncTaskManage = "AsyncTask.Manage";

    public static IReadOnlyCollection<string> All { get; } =
    [
        UsersView,
        UsersCreate,
        UsersUpdate,
        UsersDelete,
        UsersActivate,
        UsersDeactivate,
        UsersAssignRole,
        UsersAssignPermission,
        AuditLogsView,
        AuditLogView,
        ActivityLogView,
        CategoryView,
        CategoryCreate,
        CategoryUpdate,
        CategoryDelete,
        CategoryActivate,
        CategoryDeactivate,
        FileView,
        FileUpload,
        FileDownload,
        FileDelete,
        FileMarkPermanent,
        EmailView,
        EmailSend,
        EmailTemplateView,
        EmailTemplateCreate,
        EmailTemplateUpdate,
        EmailTemplateDelete,
        EmailTemplateActivate,
        EmailTemplateDeactivate,
        NotificationView,
        NotificationViewAll,
        NotificationManage,
        NotificationCreate,
        NotificationMarkRead,
        NotificationArchive,
        BackgroundJobView,
        BackgroundJobRun,
        BackgroundJobDashboard,
        MonitoringHealthView,
        MonitoringSystemInfoView,
        SettingView,
        SettingCreate,
        SettingUpdate,
        SettingDelete,
        SettingActivate,
        SettingDeactivate,
        SettingViewSensitive,
        AccessPolicyView,
        AccessPolicyUpdate,
        MaintenanceView,
        MaintenanceUpdate,
        TenantView,
        TenantManage,
        OrganizationView,
        OrganizationManage,
        WorkspaceView,
        WorkspaceManage,
        PermissionPolicyView,
        PermissionPolicyManage,
        AuthorizationMatrixView,
        AuthorizationMatrixManage,
        AuthorizationCheckExecute,
        AuthorizationCheckExplain,
        MasterDataGroupView,
        MasterDataGroupManage,
        MasterDataItemView,
        MasterDataItemManage,
        LookupView,
        AsyncTaskView,
        AsyncTaskSubmit,
        AsyncTaskCancel,
        AsyncTaskRetry,
        AsyncTaskManage
    ];
}
