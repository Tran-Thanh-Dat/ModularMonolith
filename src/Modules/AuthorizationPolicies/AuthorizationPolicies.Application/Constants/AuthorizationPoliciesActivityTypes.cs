namespace AuthorizationPolicies.Application.Constants;

public static class AuthorizationPoliciesActivityTypes
{
    public const string PermissionPolicyCreated = "PermissionPolicyCreated";
    public const string PermissionPolicyUpdated = "PermissionPolicyUpdated";
    public const string PermissionPolicyActivated = "PermissionPolicyActivated";
    public const string PermissionPolicyDeactivated = "PermissionPolicyDeactivated";
    public const string PermissionPolicyDeleted = "PermissionPolicyDeleted";
    public const string RolePermissionPolicyAssigned = "RolePermissionPolicyAssigned";
    public const string RolePermissionPolicyActivated = "RolePermissionPolicyActivated";
    public const string RolePermissionPolicyDeactivated = "RolePermissionPolicyDeactivated";
    public const string RolePermissionPolicyRemoved = "RolePermissionPolicyRemoved";
    public const string UserPermissionPolicyOverrideCreated = "UserPermissionPolicyOverrideCreated";
    public const string UserPermissionPolicyOverrideActivated = "UserPermissionPolicyOverrideActivated";
    public const string UserPermissionPolicyOverrideDeactivated = "UserPermissionPolicyOverrideDeactivated";
    public const string UserPermissionPolicyOverrideRemoved = "UserPermissionPolicyOverrideRemoved";
    public const string AuthorizationMatrixEntryCreated = "AuthorizationMatrixEntryCreated";
    public const string AuthorizationMatrixEntryUpdated = "AuthorizationMatrixEntryUpdated";
    public const string AuthorizationMatrixEntryEnabled = "AuthorizationMatrixEntryEnabled";
    public const string AuthorizationMatrixEntryDisabled = "AuthorizationMatrixEntryDisabled";
    public const string AuthorizationMatrixEntryDeleted = "AuthorizationMatrixEntryDeleted";
    public const string AuthorizationCheckEvaluated = "AuthorizationCheckEvaluated";
    public const string AuthorizationCheckExplained = "AuthorizationCheckExplained";
}

public static class AuthorizationPoliciesModuleConstants
{
    public const string ModuleName = "AuthorizationPolicies";
}
