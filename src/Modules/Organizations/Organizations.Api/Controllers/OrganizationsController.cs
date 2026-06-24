using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organizations.Api.Contracts;
using Organizations.Application.Organizations;
using Organizations.Application.Organizations.ActivateOrganization;
using Organizations.Application.Organizations.CreateOrganization;
using Organizations.Application.Organizations.DeactivateOrganization;
using Organizations.Application.Organizations.DeleteOrganization;
using Organizations.Application.Organizations.GetOrganizationById;
using Organizations.Application.Organizations.GetOrganizations;
using Organizations.Application.Organizations.GetOrganizationTree;
using Organizations.Application.Organizations.UpdateOrganization;
using Organizations.Application.Permissions;
using Organizations.Domain.Enums;

namespace Organizations.Api.Controllers;

[Authorize]
[Route("api/v1/organizations")]
public sealed class OrganizationsController : BaseApiController
{
    private readonly ISender _sender;

    public OrganizationsController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(OrganizationsPermissionCodes.OrganizationView)]
    public async Task<ActionResult<PagedResponse<OrganizationListItemResponse>>> GetOrganizations(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? parentOrganizationId,
        [FromQuery] string? keyword,
        [FromQuery] bool? isActive,
        [FromQuery] OrganizationType? type,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetOrganizationsQuery(tenantId, parentOrganizationId, keyword, isActive, type, pageIndex, pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("tree")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrganizationTreeNodeResponse>>>> GetOrganizationTree(
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrganizationTreeQuery(tenantId), cancellationToken);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationView)]
    public async Task<ActionResult<ApiResponse<OrganizationDetailResponse>>> GetOrganizationById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrganizationByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    [ProducesResponseType(typeof(ApiResponse<CreateOrganizationResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateOrganizationResponse>>> CreateOrganization(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateOrganizationCommand(
                request.TenantId,
                request.ParentOrganizationId,
                request.Code,
                request.Name,
                request.Description,
                request.Type,
                request.SortOrder,
                request.Metadata),
            cancellationToken);

        return CreatedFromResult(nameof(GetOrganizationById), new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    public async Task<ActionResult<ApiResponse>> UpdateOrganization(
        Guid id,
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateOrganizationCommand(id, request.Name, request.Description, request.Type, request.SortOrder, request.Metadata, request.ParentOrganizationId),
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    public async Task<ActionResult<ApiResponse>> DeleteOrganization(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteOrganizationCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    public async Task<ActionResult<ApiResponse>> ActivateOrganization(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateOrganizationCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    public async Task<ActionResult<ApiResponse>> DeactivateOrganization(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateOrganizationCommand(id), cancellationToken);
        return FromResult(result);
    }
}
