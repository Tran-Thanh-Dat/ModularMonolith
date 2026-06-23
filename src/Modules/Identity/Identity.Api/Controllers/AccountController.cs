using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using Identity.Api.Contracts;
using Identity.Application.Account.ChangePassword;
using Identity.Application.Account.ForgotPassword;
using Identity.Application.Account.Profile;
using Identity.Application.Account.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[Route("api/v1/account")]
public sealed class AccountController : BaseApiController
{
    private readonly ISender _sender;

    public AccountController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AccountProfileResponse>>> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAccountProfileQuery(), cancellationToken);
        return FromResult(result);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AccountProfileResponse>>> UpdateProfile(
        [FromBody] UpdateAccountProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateAccountProfileCommand(request.Email, request.FullName),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ChangePasswordCommand(
                request.CurrentPassword,
                request.NewPassword,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ForgotPasswordCommand(
                request.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ResetPasswordCommand(
                request.Token,
                request.NewPassword,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return FromResult(result);
    }
}
