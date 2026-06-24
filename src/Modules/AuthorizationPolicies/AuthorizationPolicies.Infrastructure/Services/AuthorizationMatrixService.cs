using AuditLogs.Application.Abstractions;
using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.Constants;
using AuthorizationPolicies.Application.Models;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.AuthorizationMatrix;
using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Domain.Errors;
using AuthorizationPolicies.Domain.PermissionPolicies;
using AuthorizationPolicies.Domain.RolePermissionPolicies;
using AuthorizationPolicies.Domain.UserPermissionPolicyOverrides;
using AuthorizationPolicies.Infrastructure.Persistence;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthorizationPolicies.Infrastructure.Services;

public sealed class AuthorizationMatrixService : IAuthorizationMatrixService
{
    private readonly AuthorizationPoliciesUnitOfWork _unitOfWork;
    private readonly ICurrentUserPermissionContextService _contextService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<AuthorizationMatrixService> _logger;

    public AuthorizationMatrixService(
        AuthorizationPoliciesUnitOfWork unitOfWork,
        ICurrentUserPermissionContextService contextService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<AuthorizationMatrixService> logger)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Result<AuthorizationDecisionResponse>> CheckAsync(
        Guid userId,
        string? permissionCode,
        IAuthorizationResourceContext resourceContext,
        string? action = null,
        string? resourceType = null,
        string? moduleCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userContext = await _contextService.GetContextAsync(userId, cancellationToken);
            var (decision, _) = await EvaluateInternalAsync(
                userContext, permissionCode, resourceContext, action, resourceType, moduleCode, includeExplanation: false, cancellationToken);

            await _activityLogService.EnqueuePostCommitAsync(
                AuthorizationPoliciesActivityTypes.AuthorizationCheckEvaluated,
                $"Authorization evaluated for user {userId}: {(decision.IsAllowed ? "Allow" : "Deny")}",
                AuthorizationPoliciesModuleConstants.ModuleName,
                cancellationToken: cancellationToken);

            return Result<AuthorizationDecisionResponse>.Success(MapDecision(decision));
        }
        catch (BuildingBlocks.Application.Exceptions.NotFoundException)
        {
            return Result<AuthorizationDecisionResponse>.Failure(UserErrors.NotFound, "User not found.");
        }
    }

    public async Task<Result> AuthorizeAsync(
        CurrentUserPermissionContext userContext,
        string requiredPermission,
        IAuthorizationResourceContext resourceContext,
        AuthorizationScope scope,
        CancellationToken cancellationToken = default)
    {
        var (decision, _) = await EvaluateInternalAsync(
            userContext,
            requiredPermission.Trim(),
            resourceContext,
            action: null,
            resourceType: null,
            moduleCode: null,
            includeExplanation: false,
            cancellationToken,
            explicitScope: scope);

        return decision.IsAllowed
            ? Result.Success()
            : Result.Failure(AuthErrors.PermissionDenied, decision.Reason);
    }

    public async Task<IReadOnlyList<EffectivePolicyInfo>> GetEffectivePoliciesAsync(
        Guid userId,
        string? moduleCode = null,
        string? resourceType = null,
        string? action = null,
        CancellationToken cancellationToken = default)
    {
        var userContext = await _contextService.GetContextAsync(userId, cancellationToken);
        return await LoadEffectivePoliciesAsync(userContext, moduleCode, resourceType, action, cancellationToken);
    }

    public async Task<Result<AuthorizationExplanationResponse>> ExplainAsync(
        Guid userId,
        string? permissionCode,
        IAuthorizationResourceContext resourceContext,
        string? action = null,
        string? resourceType = null,
        string? moduleCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userContext = await _contextService.GetContextAsync(userId, cancellationToken);
            var (decision, explanation) = await EvaluateInternalAsync(
                userContext, permissionCode, resourceContext, action, resourceType, moduleCode, includeExplanation: true, cancellationToken);

            await _activityLogService.EnqueuePostCommitAsync(
                AuthorizationPoliciesActivityTypes.AuthorizationCheckExplained,
                $"Authorization explained for user {userId}: {(decision.IsAllowed ? "Allow" : "Deny")}",
                AuthorizationPoliciesModuleConstants.ModuleName,
                cancellationToken: cancellationToken);

            return Result<AuthorizationExplanationResponse>.Success(new AuthorizationExplanationResponse
            {
                Decision = MapDecision(decision),
                Steps = explanation?.Steps ?? [],
                MatchedPolicies = explanation?.MatchedPolicies.Select(MapPolicy).ToArray() ?? []
            });
        }
        catch (BuildingBlocks.Application.Exceptions.NotFoundException)
        {
            return Result<AuthorizationExplanationResponse>.Failure(UserErrors.NotFound, "User not found.");
        }
    }

    private async Task<(AuthorizationDecision Decision, AuthorizationExplanation? Explanation)> EvaluateInternalAsync(
        CurrentUserPermissionContext userContext,
        string? permissionCode,
        IAuthorizationResourceContext resourceContext,
        string? action,
        string? resourceType,
        string? moduleCode,
        bool includeExplanation,
        CancellationToken cancellationToken,
        AuthorizationScope? explicitScope = null)
    {
        var steps = includeExplanation ? new List<string>() : null;
        AuthorizationMatrixEntry? matrixEntry = null;

        if (!string.IsNullOrWhiteSpace(action) && !string.IsNullOrWhiteSpace(resourceType))
        {
            matrixEntry = await FindMatrixEntryAsync(moduleCode, resourceType, action, cancellationToken);
            steps?.Add(matrixEntry is null
                ? "No enabled matrix entry found for action/resource."
                : $"Matrix entry found: requires {matrixEntry.RequiredPermissionCode} with scope {matrixEntry.Scope}.");
        }

        var requiredPermission = !string.IsNullOrWhiteSpace(permissionCode)
            ? permissionCode.Trim()
            : matrixEntry?.RequiredPermissionCode;

        if (string.IsNullOrWhiteSpace(requiredPermission))
        {
            steps?.Add("Missing required permission code.");
            return (Deny("Missing permission or action/resourceType.", requiredPermission, matrixEntry?.Scope, resourceContext), BuildExplanation(steps, []));
        }

        var scope = explicitScope ?? matrixEntry?.Scope ?? AuthorizationScope.Global;

        if (!ValidateResourceContextRequirements(scope, resourceContext, out var contextReason))
        {
            steps?.Add(contextReason);
            return (Deny(contextReason, requiredPermission, scope, resourceContext), BuildExplanation(steps, []));
        }

        var matchedPolicies = await LoadEffectivePoliciesAsync(
            userContext, moduleCode, resourceType, action, cancellationToken);

        var relevantPolicies = matchedPolicies
            .Where(p => string.Equals(p.PermissionCode, requiredPermission, StringComparison.Ordinal))
            .ToList();

        var activeOverrides = await LoadUserOverridesAsync(userContext.UserId, requiredPermission, cancellationToken);
        var denyOverride = activeOverrides
            .Where(o => o.Effect == AuthorizationEffect.Deny)
            .OrderByDescending(o => o.Priority)
            .ThenBy(o => o.PolicyCode, StringComparer.Ordinal)
            .FirstOrDefault();
        if (denyOverride is not null)
        {
            steps?.Add($"User deny override applied for policy {denyOverride.PolicyCode}.");
            return (Deny("Denied by user override.", requiredPermission, scope, resourceContext, denyOverride.PolicyCode), BuildExplanation(steps, relevantPolicies));
        }

        var denyPolicy = relevantPolicies
            .Where(p => p.Effect == AuthorizationEffect.Deny)
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.PolicyCode, StringComparer.Ordinal)
            .FirstOrDefault();
        if (denyPolicy is not null)
        {
            steps?.Add($"Deny policy matched: {denyPolicy.PolicyCode}.");
            return (Deny("Denied by policy.", requiredPermission, scope, resourceContext, denyPolicy.PolicyCode), BuildExplanation(steps, relevantPolicies));
        }

        if (!userContext.PermissionCodes.Contains(requiredPermission, StringComparer.Ordinal))
        {
            steps?.Add($"User missing required permission {requiredPermission}.");
            return (Deny("Missing required permission.", requiredPermission, scope, resourceContext), BuildExplanation(steps, relevantPolicies));
        }

        steps?.Add($"User has permission {requiredPermission}.");

        var allowPolicy = relevantPolicies
            .Where(p => p.Effect == AuthorizationEffect.Allow)
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.PolicyCode, StringComparer.Ordinal)
            .FirstOrDefault();

        var policyScope = allowPolicy?.Scope ?? scope;

        if (!EvaluateScope(userContext, policyScope, resourceContext, out var scopeReason))
        {
            steps?.Add(scopeReason);
            return (Deny(scopeReason, requiredPermission, policyScope, resourceContext), BuildExplanation(steps, relevantPolicies));
        }

        steps?.Add($"Scope {policyScope} satisfied.");
        return (Allow(requiredPermission, policyScope, resourceContext, allowPolicy?.PolicyCode), BuildExplanation(steps, relevantPolicies));
    }

    private async Task<AuthorizationMatrixEntry?> FindMatrixEntryAsync(
        string? moduleCode,
        string resourceType,
        string action,
        CancellationToken cancellationToken)
    {
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

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<EffectivePolicyInfo>> LoadEffectivePoliciesAsync(
        CurrentUserPermissionContext userContext,
        string? moduleCode,
        string? resourceType,
        string? action,
        CancellationToken cancellationToken)
    {
        var roleIds = userContext.RoleIds.ToArray();
        if (roleIds.Length == 0)
        {
            return [];
        }

        var roleIdList = roleIds.ToList();
        var assignments = await _unitOfWork.Repository<RolePermissionPolicy, Guid>()
            .Query()
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.IsActive && roleIdList.Contains(r.RoleId))
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return [];
        }

        var policyIds = assignments.Select(a => a.PermissionPolicyId).Distinct().ToArray();
        var policyIdList = policyIds.ToList();
        var policiesQuery = _unitOfWork.Repository<PermissionPolicy, Guid>()
            .Query()
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive && policyIdList.Contains(p.Id));

        if (!string.IsNullOrWhiteSpace(moduleCode))
        {
            policiesQuery = policiesQuery.Where(p => p.ModuleCode == moduleCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            policiesQuery = policiesQuery.Where(p => p.ResourceType == resourceType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            policiesQuery = policiesQuery.Where(p => p.Action == action.Trim());
        }

        var policies = await policiesQuery.ToListAsync(cancellationToken);
        var policyLookup = policies.ToDictionary(p => p.Id);

        return assignments
            .Where(a => policyLookup.ContainsKey(a.PermissionPolicyId))
            .Select(a =>
            {
                var policy = policyLookup[a.PermissionPolicyId];
                return new EffectivePolicyInfo
                {
                    PolicyId = policy.Id,
                    PolicyCode = policy.Code,
                    PermissionCode = policy.PermissionCode,
                    Scope = policy.Scope,
                    Effect = policy.Effect,
                    Priority = policy.Priority,
                    Source = "Role"
                };
            })
            .ToList();
    }

    private async Task<IReadOnlyList<EffectivePolicyInfo>> LoadUserOverridesAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var overrides = await _unitOfWork.Repository<UserPermissionPolicyOverride, Guid>()
            .Query()
            .AsNoTracking()
            .Where(o => !o.IsDeleted && o.IsActive && o.UserId == userId &&
                        (!o.ExpiresAt.HasValue || o.ExpiresAt > now))
            .ToListAsync(cancellationToken);

        if (overrides.Count == 0)
        {
            return [];
        }

        var policyIds = overrides.Select(o => o.PermissionPolicyId).Distinct().ToArray();
        var policyIdList = policyIds.ToList();
        var policies = await _unitOfWork.Repository<PermissionPolicy, Guid>()
            .Query()
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive && policyIdList.Contains(p.Id) && p.PermissionCode == permissionCode)
            .ToListAsync(cancellationToken);

        var policyLookup = policies.ToDictionary(p => p.Id);

        return overrides
            .Where(o => policyLookup.ContainsKey(o.PermissionPolicyId))
            .Select(o =>
            {
                var policy = policyLookup[o.PermissionPolicyId];
                return new EffectivePolicyInfo
                {
                    PolicyId = policy.Id,
                    PolicyCode = policy.Code,
                    PermissionCode = policy.PermissionCode,
                    Scope = policy.Scope,
                    Effect = o.Effect,
                    Priority = policy.Priority + 10000,
                    Source = "UserOverride"
                };
            })
            .ToList();
    }

    internal static bool EvaluateScope(
        CurrentUserPermissionContext userContext,
        AuthorizationScope scope,
        IAuthorizationResourceContext resourceContext,
        out string reason)
    {
        reason = string.Empty;
        switch (scope)
        {
            case AuthorizationScope.Global:
                return true;

            case AuthorizationScope.Tenant:
                if (!resourceContext.TenantId.HasValue)
                {
                    reason = "Tenant scope requires TenantId in resource context.";
                    return false;
                }

                if (!userContext.TenantIds.Contains(resourceContext.TenantId.Value))
                {
                    reason = "User is not a member of the tenant.";
                    return false;
                }

                return true;

            case AuthorizationScope.Organization:
                if (!resourceContext.OrganizationId.HasValue)
                {
                    reason = "Organization scope requires OrganizationId in resource context.";
                    return false;
                }

                if (!userContext.OrganizationIds.Contains(resourceContext.OrganizationId.Value))
                {
                    reason = "User is not a member of the organization.";
                    return false;
                }

                return true;

            case AuthorizationScope.Workspace:
                if (!resourceContext.WorkspaceId.HasValue)
                {
                    reason = "Workspace scope requires WorkspaceId in resource context.";
                    return false;
                }

                if (!userContext.WorkspaceIds.Contains(resourceContext.WorkspaceId.Value))
                {
                    reason = "User is not a member of the workspace.";
                    return false;
                }

                return true;

            case AuthorizationScope.OwnerOnly:
                if (resourceContext.OwnerUserId == userContext.UserId || resourceContext.CreatedBy == userContext.UserId)
                {
                    return true;
                }

                reason = "User is not the resource owner.";
                return false;

            case AuthorizationScope.AssignedOnly:
                if (resourceContext.AssignedUserIds.Contains(userContext.UserId))
                {
                    return true;
                }

                reason = "User is not assigned to the resource.";
                return false;

            case AuthorizationScope.Self:
                if (resourceContext.ResourceId == userContext.UserId ||
                    resourceContext.OwnerUserId == userContext.UserId ||
                    resourceContext.CreatedBy == userContext.UserId)
                {
                    return true;
                }

                reason = "Resource does not belong to the user.";
                return false;

            case AuthorizationScope.Department:
            case AuthorizationScope.Custom:
                reason = $"Scope {scope} is not supported in this phase.";
                return false;

            default:
                reason = "Unknown authorization scope.";
                return false;
        }
    }

    internal static bool ValidateResourceContextRequirements(
        AuthorizationScope scope,
        IAuthorizationResourceContext resourceContext,
        out string reason)
    {
        reason = string.Empty;
        switch (scope)
        {
            case AuthorizationScope.Tenant:
                if (!resourceContext.TenantId.HasValue)
                {
                    reason = "Tenant scope requires TenantId in resource context.";
                    return false;
                }

                break;

            case AuthorizationScope.Organization:
                if (!resourceContext.OrganizationId.HasValue)
                {
                    reason = "Organization scope requires OrganizationId in resource context.";
                    return false;
                }

                break;

            case AuthorizationScope.Workspace:
                if (!resourceContext.WorkspaceId.HasValue)
                {
                    reason = "Workspace scope requires WorkspaceId in resource context.";
                    return false;
                }

                break;

            case AuthorizationScope.OwnerOnly:
                if (!resourceContext.OwnerUserId.HasValue && !resourceContext.CreatedBy.HasValue)
                {
                    reason = "OwnerOnly scope requires OwnerUserId or CreatedBy in resource context.";
                    return false;
                }

                break;

            case AuthorizationScope.AssignedOnly:
                if (resourceContext.AssignedUserIds.Count == 0)
                {
                    reason = "AssignedOnly scope requires AssignedUserIds in resource context.";
                    return false;
                }

                break;

            case AuthorizationScope.Self:
                if (!resourceContext.ResourceId.HasValue &&
                    !resourceContext.OwnerUserId.HasValue &&
                    !resourceContext.CreatedBy.HasValue)
                {
                    reason = "Self scope requires ResourceId, OwnerUserId, or CreatedBy in resource context.";
                    return false;
                }

                break;

            case AuthorizationScope.Department:
            case AuthorizationScope.Custom:
                reason = $"Scope {scope} is not supported in this phase.";
                return false;
        }

        return true;
    }

    private static AuthorizationDecision Allow(
        string permissionCode,
        AuthorizationScope scope,
        IAuthorizationResourceContext context,
        string? policyCode) =>
        new()
        {
            IsAllowed = true,
            Effect = AuthorizationEffect.Allow,
            Reason = "Access granted.",
            MatchedPermissionCode = permissionCode,
            MatchedPolicyCode = policyCode,
            Scope = scope,
            ResourceType = context.ResourceType,
            ResourceId = context.ResourceId,
            TenantId = context.TenantId,
            OrganizationId = context.OrganizationId,
            WorkspaceId = context.WorkspaceId
        };

    private static AuthorizationDecision Deny(
        string reason,
        string? permissionCode,
        AuthorizationScope? scope,
        IAuthorizationResourceContext context,
        string? policyCode = null) =>
        new()
        {
            IsAllowed = false,
            Effect = AuthorizationEffect.Deny,
            Reason = reason,
            MatchedPermissionCode = permissionCode,
            MatchedPolicyCode = policyCode,
            Scope = scope,
            ResourceType = context.ResourceType,
            ResourceId = context.ResourceId,
            TenantId = context.TenantId,
            OrganizationId = context.OrganizationId,
            WorkspaceId = context.WorkspaceId
        };

    private static AuthorizationDecisionResponse MapDecision(AuthorizationDecision d) => new()
    {
        IsAllowed = d.IsAllowed,
        Effect = d.Effect,
        Reason = d.Reason,
        MatchedPermissionCode = d.MatchedPermissionCode,
        MatchedPolicyCode = d.MatchedPolicyCode,
        Scope = d.Scope,
        ResourceType = d.ResourceType,
        ResourceId = d.ResourceId,
        TenantId = d.TenantId,
        OrganizationId = d.OrganizationId,
        WorkspaceId = d.WorkspaceId
    };

    private static EffectivePolicyInfoResponse MapPolicy(EffectivePolicyInfo p) => new()
    {
        PolicyId = p.PolicyId,
        PolicyCode = p.PolicyCode,
        PermissionCode = p.PermissionCode,
        Scope = p.Scope,
        Effect = p.Effect,
        Priority = p.Priority,
        Source = p.Source
    };

    private static AuthorizationExplanation? BuildExplanation(List<string>? steps, IReadOnlyList<EffectivePolicyInfo> policies) =>
        steps is null ? null : new AuthorizationExplanation
        {
            Decision = new AuthorizationDecision(),
            Steps = steps,
            MatchedPolicies = policies
        };
}
