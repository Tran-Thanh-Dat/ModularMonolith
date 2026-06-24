namespace AuthorizationPolicies.Domain.Errors;

public static class PermissionPolicyErrors
{
    public const string NotFound = "PermissionPolicy.NotFound";
    public const string CodeAlreadyExists = "PermissionPolicy.CodeAlreadyExists";
    public const string AlreadyDeleted = "PermissionPolicy.AlreadyDeleted";
    public const string AlreadyActive = "PermissionPolicy.AlreadyActive";
    public const string AlreadyInactive = "PermissionPolicy.AlreadyInactive";
    public const string Inactive = "PermissionPolicy.Inactive";
    public const string HasAssignments = "PermissionPolicy.HasAssignments";
}

public static class RolePermissionPolicyErrors
{
    public const string NotFound = "RolePermissionPolicy.NotFound";
    public const string AlreadyExists = "RolePermissionPolicy.AlreadyExists";
    public const string AlreadyActive = "RolePermissionPolicy.AlreadyActive";
    public const string AlreadyInactive = "RolePermissionPolicy.AlreadyInactive";
    public const string PolicyInactive = "RolePermissionPolicy.PolicyInactive";
}

public static class UserPermissionPolicyOverrideErrors
{
    public const string NotFound = "UserPermissionPolicyOverride.NotFound";
    public const string AlreadyExists = "UserPermissionPolicyOverride.AlreadyExists";
    public const string AlreadyActive = "UserPermissionPolicyOverride.AlreadyActive";
    public const string AlreadyInactive = "UserPermissionPolicyOverride.AlreadyInactive";
    public const string PolicyInactive = "UserPermissionPolicyOverride.PolicyInactive";
    public const string InvalidExpiresAt = "UserPermissionPolicyOverride.InvalidExpiresAt";
}

public static class AuthorizationMatrixErrors
{
    public const string NotFound = "AuthorizationMatrix.NotFound";
    public const string AlreadyExists = "AuthorizationMatrix.AlreadyExists";
    public const string AlreadyDeleted = "AuthorizationMatrix.AlreadyDeleted";
    public const string AlreadyEnabled = "AuthorizationMatrix.AlreadyEnabled";
    public const string AlreadyDisabled = "AuthorizationMatrix.AlreadyDisabled";
}

public static class AuthorizationCheckErrors
{
    public const string UserNotFound = "AuthorizationCheck.UserNotFound";
    public const string MissingPermissionOrAction = "AuthorizationCheck.MissingPermissionOrAction";
    public const string ScopeNotSupported = "AuthorizationCheck.ScopeNotSupported";
    public const string InvalidScope = "AuthorizationCheck.InvalidScope";
    public const string MissingTenantId = "AuthorizationCheck.MissingTenantId";
    public const string MissingOrganizationId = "AuthorizationCheck.MissingOrganizationId";
    public const string MissingWorkspaceId = "AuthorizationCheck.MissingWorkspaceId";
    public const string MissingOwnerContext = "AuthorizationCheck.MissingOwnerContext";
    public const string MissingAssignedUsers = "AuthorizationCheck.MissingAssignedUsers";
    public const string MissingSelfContext = "AuthorizationCheck.MissingSelfContext";
}
