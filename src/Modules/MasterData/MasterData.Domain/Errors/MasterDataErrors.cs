namespace MasterData.Domain.Errors;

public static class MasterDataGroupErrors
{
    public const string NotFound = "MasterDataGroup.NotFound";
    public const string CodeAlreadyExists = "MasterDataGroup.CodeAlreadyExists";
    public const string AlreadyDeleted = "MasterDataGroup.AlreadyDeleted";
    public const string AlreadyActive = "MasterDataGroup.AlreadyActive";
    public const string AlreadyInactive = "MasterDataGroup.AlreadyInactive";
    public const string Inactive = "MasterDataGroup.Inactive";
    public const string HasActiveItems = "MasterDataGroup.HasActiveItems";
    public const string SystemProtected = "MasterDataGroup.SystemProtected";
    public const string CodeChangeNotAllowed = "MasterDataGroup.CodeChangeNotAllowed";
    public const string InvalidScope = "MasterDataGroup.InvalidScope";
    public const string InvalidTenant = "MasterDataGroup.InvalidTenant";
    public const string TenantInactive = "MasterDataGroup.TenantInactive";
    public const string InvalidOrganization = "MasterDataGroup.InvalidOrganization";
    public const string OrganizationInactive = "MasterDataGroup.OrganizationInactive";
    public const string OrganizationTenantMismatch = "MasterDataGroup.OrganizationTenantMismatch";
}

public static class MasterDataItemErrors
{
    public const string NotFound = "MasterDataItem.NotFound";
    public const string CodeAlreadyExists = "MasterDataItem.CodeAlreadyExists";
    public const string AlreadyDeleted = "MasterDataItem.AlreadyDeleted";
    public const string AlreadyActive = "MasterDataItem.AlreadyActive";
    public const string AlreadyInactive = "MasterDataItem.AlreadyInactive";
    public const string GroupInactive = "MasterDataItem.GroupInactive";
    public const string SystemProtected = "MasterDataItem.SystemProtected";
    public const string CodeChangeNotAllowed = "MasterDataItem.CodeChangeNotAllowed";
    public const string InvalidParent = "MasterDataItem.InvalidParent";
    public const string ParentSelfReference = "MasterDataItem.ParentSelfReference";
    public const string CircularParent = "MasterDataItem.CircularParent";
    public const string InvalidEffectiveRange = "MasterDataItem.InvalidEffectiveRange";
    public const string InactiveCannotBeDefault = "MasterDataItem.InactiveCannotBeDefault";
}

public static class LookupErrors
{
    public const string GroupCodeRequired = "Lookup.GroupCodeRequired";
    public const string GroupCodesRequired = "Lookup.GroupCodesRequired";
    public const string TooManyGroupCodes = "Lookup.TooManyGroupCodes";
    public const string GroupNotFound = "Lookup.GroupNotFound";
}
