using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Settings.Api.Contracts;
using Settings.Application.AccessPolicy;
using Settings.Application.AccessPolicy.GetLoginPolicy;
using Settings.Application.AccessPolicy.GetMaintenancePolicy;
using Settings.Application.AccessPolicy.GetPasswordPolicy;
using Settings.Application.AccessPolicy.GetSessionPolicy;
using Settings.Application.AccessPolicy.UpdateLoginPolicy;
using Settings.Application.AccessPolicy.UpdateMaintenancePolicy;
using Settings.Application.AccessPolicy.UpdatePasswordPolicy;
using Settings.Application.AccessPolicy.UpdateSessionPolicy;
using Settings.Application.Permissions;

namespace Settings.Api.Controllers;

[Authorize]
[Route("api/v1/access-policy")]
public sealed class AccessPolicyController : BaseApiController
{
    private readonly ISender _sender;

    public AccessPolicyController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("password")]
    [HasPermission(SettingsPermissionCodes.AccessPolicyView)]
    public async Task<ActionResult<ApiResponse<PasswordPolicyResponse>>> GetPasswordPolicy(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPasswordPolicyQuery(), cancellationToken);
        return FromResult(result);
    }

    [HttpPut("password")]
    [HasPermission(SettingsPermissionCodes.AccessPolicyUpdate)]
    public async Task<ActionResult<ApiResponse>> UpdatePasswordPolicy(
        [FromBody] Contracts.UpdatePasswordPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdatePasswordPolicyCommand(
                request.MinimumLength,
                request.RequireUppercase,
                request.RequireLowercase,
                request.RequireDigit,
                request.RequireSpecialCharacter,
                request.PasswordExpirationDays,
                request.PreventPasswordReuseCount),
            cancellationToken);

        return FromResult(result);
    }

    [HttpGet("login")]
    [HasPermission(SettingsPermissionCodes.AccessPolicyView)]
    public async Task<ActionResult<ApiResponse<LoginPolicyResponse>>> GetLoginPolicy(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLoginPolicyQuery(), cancellationToken);
        return FromResult(result);
    }

    [HttpPut("login")]
    [HasPermission(SettingsPermissionCodes.AccessPolicyUpdate)]
    public async Task<ActionResult<ApiResponse>> UpdateLoginPolicy(
        [FromBody] Contracts.UpdateLoginPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateLoginPolicyCommand(
                request.MaxFailedLoginAttempts,
                request.LockoutDurationMinutes,
                request.EnableLockout,
                request.RequireConfirmedEmail),
            cancellationToken);

        return FromResult(result);
    }

    [HttpGet("session")]
    [HasPermission(SettingsPermissionCodes.AccessPolicyView)]
    public async Task<ActionResult<ApiResponse<SessionPolicyResponse>>> GetSessionPolicy(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSessionPolicyQuery(), cancellationToken);
        return FromResult(result);
    }

    [HttpPut("session")]
    [HasPermission(SettingsPermissionCodes.AccessPolicyUpdate)]
    public async Task<ActionResult<ApiResponse>> UpdateSessionPolicy(
        [FromBody] Contracts.UpdateSessionPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateSessionPolicyCommand(
                request.AccessTokenExpirationMinutes,
                request.RefreshTokenExpirationDays,
                request.SessionTimeoutMinutes,
                request.RefreshTokenReuseDetectionEnabled),
            cancellationToken);

        return FromResult(result);
    }

    [HttpGet("maintenance")]
    [HasPermission(SettingsPermissionCodes.MaintenanceView)]
    public async Task<ActionResult<ApiResponse<MaintenancePolicyResponse>>> GetMaintenancePolicy(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMaintenancePolicyQuery(), cancellationToken);
        return FromResult(result);
    }

    [HttpPut("maintenance")]
    [HasPermission(SettingsPermissionCodes.MaintenanceUpdate)]
    public async Task<ActionResult<ApiResponse>> UpdateMaintenancePolicy(
        [FromBody] Contracts.UpdateMaintenancePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateMaintenancePolicyCommand(
                request.Enabled,
                request.Message,
                request.StartAt,
                request.EndAt,
                request.AllowAdminBypass),
            cancellationToken);

        return FromResult(result);
    }
}
