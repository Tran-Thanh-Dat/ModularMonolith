using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.AuthorizationChecks;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.AuthorizationMatrix;
using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthorizationPolicies.Infrastructure.Services;

public sealed class AuthorizationCheckRequestValidator : IAuthorizationCheckRequestValidator
{
    private readonly AuthorizationPoliciesUnitOfWork _unitOfWork;

    public AuthorizationCheckRequestValidator(AuthorizationPoliciesUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task ValidateAsync(
        string? permissionCode,
        AuthorizationResourceContextDto resourceContext,
        string? action,
        string? resourceType,
        string? moduleCode,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(action, resourceType, moduleCode, cancellationToken);
        AuthorizationCheckScopeRules.ValidateResourceContext(scope, resourceContext);
    }

    private async Task<AuthorizationScope> ResolveScopeAsync(
        string? action,
        string? resourceType,
        string? moduleCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(resourceType))
        {
            return AuthorizationScope.Global;
        }

        var query = _unitOfWork.Repository<AuthorizationMatrixEntry, Guid>()
            .Query()
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.IsEnabled &&
                        e.ResourceType == resourceType.Trim() &&
                        e.Action == action.Trim());

        if (!string.IsNullOrWhiteSpace(moduleCode))
        {
            query = query.Where(e => e.ModuleCode == moduleCode.Trim());
        }

        var entry = await query.FirstOrDefaultAsync(cancellationToken);
        return entry?.Scope ?? AuthorizationScope.Global;
    }
}
