using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Api.Contracts;
using Notifications.Application.EmailTemplates;
using Notifications.Application.EmailTemplates.ActivateEmailTemplate;
using Notifications.Application.EmailTemplates.CreateEmailTemplate;
using Notifications.Application.EmailTemplates.DeactivateEmailTemplate;
using Notifications.Application.EmailTemplates.DeleteEmailTemplate;
using Notifications.Application.EmailTemplates.GetEmailTemplateById;
using Notifications.Application.EmailTemplates.GetEmailTemplates;
using Notifications.Application.EmailTemplates.UpdateEmailTemplate;
using Notifications.Application.Permissions;

namespace Notifications.Api.Controllers;

[Authorize]
[Route("api/v1/email-templates")]
public sealed class EmailTemplatesController : BaseApiController
{
    private readonly ISender _sender;

    public EmailTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HasPermission(NotificationsPermissionCodes.EmailTemplateView)]
    public async Task<ActionResult<PagedResponse<EmailTemplateListItemResponse>>> GetEmailTemplates(
        [FromQuery] string? keyword,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetEmailTemplatesQuery(keyword, isActive, pageIndex, pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(NotificationsPermissionCodes.EmailTemplateView)]
    public async Task<ActionResult<ApiResponse<EmailTemplateDetailResponse>>> GetEmailTemplateById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEmailTemplateByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(NotificationsPermissionCodes.EmailTemplateCreate)]
    public async Task<ActionResult<ApiResponse<CreateEmailTemplateResponse>>> CreateEmailTemplate(
        [FromBody] CreateEmailTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateEmailTemplateCommand(
                request.Code,
                request.Name,
                request.Subject,
                request.Body,
                request.IsHtml,
                request.Description),
            cancellationToken);

        return CreatedFromResult(
            nameof(GetEmailTemplateById),
            new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty },
            result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(NotificationsPermissionCodes.EmailTemplateUpdate)]
    public async Task<ActionResult<ApiResponse>> UpdateEmailTemplate(
        Guid id,
        [FromBody] UpdateEmailTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateEmailTemplateCommand(
                id,
                request.Name,
                request.Subject,
                request.Body,
                request.IsHtml,
                request.Description),
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(NotificationsPermissionCodes.EmailTemplateDelete)]
    public async Task<ActionResult<ApiResponse>> DeleteEmailTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteEmailTemplateCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(NotificationsPermissionCodes.EmailTemplateActivate)]
    public async Task<ActionResult<ApiResponse>> ActivateEmailTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateEmailTemplateCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(NotificationsPermissionCodes.EmailTemplateDeactivate)]
    public async Task<ActionResult<ApiResponse>> DeactivateEmailTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateEmailTemplateCommand(id), cancellationToken);
        return FromResult(result);
    }
}
