using AuthorizationPolicies.Application.PermissionPolicies;

namespace AuthorizationPolicies.Application.Abstractions;

public interface IAuthorizationCheckRequestValidator
{
    Task ValidateAsync(
        string? permissionCode,
        AuthorizationResourceContextDto resourceContext,
        string? action,
        string? resourceType,
        string? moduleCode,
        CancellationToken cancellationToken = default);
}
