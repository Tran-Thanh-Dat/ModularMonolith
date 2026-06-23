using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Api.Contracts;
using Notifications.Application.EmailMessages;
using Notifications.Application.EmailMessages.GetEmailMessageById;
using Notifications.Application.EmailMessages.GetEmailMessages;
using Notifications.Application.EmailMessages.SendEmail;
using Notifications.Application.EmailMessages.SendTemplateEmail;
using Notifications.Application.EmailMessages.SendTestEmail;
using Notifications.Application.Permissions;

namespace Notifications.Api.Controllers;

[Authorize]
[Route("api/v1/email-messages")]
public sealed class EmailMessagesController : BaseApiController
{
    private readonly ISender _sender;

    public EmailMessagesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HasPermission(NotificationsPermissionCodes.EmailView)]
    public async Task<ActionResult<PagedResponse<EmailMessageListItemResponse>>> GetEmailMessages(
        [FromQuery] string? keyword,
        [FromQuery] string? to,
        [FromQuery] string? status,
        [FromQuery] string? provider,
        [FromQuery] string? templateCode,
        [FromQuery] string? referenceType,
        [FromQuery] string? referenceId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetEmailMessagesQuery(
                keyword,
                to,
                status,
                provider,
                templateCode,
                referenceType,
                referenceId,
                fromDate,
                toDate,
                pageIndex,
                pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(NotificationsPermissionCodes.EmailView)]
    public async Task<ActionResult<ApiResponse<EmailMessageDetailResponse>>> GetEmailMessageById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEmailMessageByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("send-test")]
    [HasPermission(NotificationsPermissionCodes.EmailSend)]
    public async Task<ActionResult<ApiResponse<SendEmailResponse>>> SendTestEmail(
        [FromBody] SendTestEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SendTestEmailCommand(request.To, request.Subject, request.Body),
            cancellationToken);

        return FromSendEmailResult(result);
    }

    [HttpPost("send")]
    [HasPermission(NotificationsPermissionCodes.EmailSend)]
    public async Task<ActionResult<ApiResponse<SendEmailResponse>>> SendEmail(
        [FromBody] SendEmailRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SendEmailCommand(
                request.To,
                request.Cc,
                request.Bcc,
                request.Subject,
                request.Body,
                request.IsHtml,
                request.ReferenceType,
                request.ReferenceId),
            cancellationToken);

        return FromSendEmailResult(result);
    }

    [HttpPost("send-template")]
    [HasPermission(NotificationsPermissionCodes.EmailSend)]
    public async Task<ActionResult<ApiResponse<SendEmailResponse>>> SendTemplateEmail(
        [FromBody] SendTemplateEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SendTemplateEmailCommand(
                request.TemplateCode,
                request.To,
                request.Cc,
                request.Bcc,
                request.TemplateData,
                request.ReferenceType,
                request.ReferenceId),
            cancellationToken);

        return FromSendEmailResult(result);
    }

    private ActionResult<ApiResponse<SendEmailResponse>> FromSendEmailResult(Result<SendEmailResponse> result)
    {
        if (!result.IsSuccess)
        {
            return MapFailure(result);
        }

        if (!result.Data!.IsSuccess)
        {
            return StatusCode(
                ResultStatusMapper.MapStatusCode(EmailErrors.SendFailed),
                new ApiResponse<SendEmailResponse>
                {
                    Success = false,
                    Code = EmailErrors.SendFailed,
                    Message = result.Data.ErrorMessage ?? "Email delivery failed.",
                    Data = result.Data,
                    Timestamp = DateTime.UtcNow,
                    TraceId = TraceId
                });
        }

        return OkResponse(result.Data);
    }

    private ActionResult<ApiResponse<SendEmailResponse>> MapFailure(Result result)
    {
        var response = ApiResponse<SendEmailResponse>.Fail(
            result.Code,
            result.Message,
            MapErrors(result.Errors),
            TraceId);

        return StatusCode(ResultStatusMapper.MapStatusCode(result.Code), response);
    }

    private static IReadOnlyList<ApiError> MapErrors(IReadOnlyList<Error> errors) =>
        errors.Select(error => new ApiError
        {
            Code = error.Code,
            Message = error.Message,
            Field = error.Field
        }).ToList();
}
