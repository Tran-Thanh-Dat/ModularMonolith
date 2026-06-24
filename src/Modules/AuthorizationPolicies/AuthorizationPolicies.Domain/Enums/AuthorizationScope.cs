namespace AuthorizationPolicies.Domain.Enums;

public enum AuthorizationScope
{
    Global = 0,
    Tenant = 1,
    Organization = 2,
    Workspace = 3,
    Department = 4,
    OwnerOnly = 5,
    AssignedOnly = 6,
    Self = 7,
    Custom = 8
}
