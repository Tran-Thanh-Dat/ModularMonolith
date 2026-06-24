using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organizations.Api.Contracts;
using Organizations.Application.Permissions;
using Organizations.Application.Tenants;
using Organizations.Application.Tenants.ActivateTenant;
using Organizations.Application.Tenants.CreateTenant;
using Organizations.Application.Tenants.DeactivateTenant;
using Organizations.Application.Tenants.DeleteTenant;
using Organizations.Application.Tenants.GetTenantById;
using Organizations.Application.Tenants.GetTenants;
using Organizations.Application.Tenants.UpdateTenant;

namespace Organizations.Api.Controllers;

[Authorize]
[Route("api/v1/tenants")]
public sealed class TenantsController : BaseApiController
{
    private readonly ISender _sender;

    public TenantsController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(OrganizationsPermissionCodes.TenantView)]
    public async Task<ActionResult<PagedResponse<TenantListItemResponse>>> GetTenants(
        [FromQuery] string? keyword,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetTenantsQuery(keyword, isActive, pageIndex, pageSize), cancellationToken);
        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.TenantView)]
    public async Task<ActionResult<ApiResponse<TenantDetailResponse>>> GetTenantById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTenantByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(OrganizationsPermissionCodes.TenantManage)]
    [ProducesResponseType(typeof(ApiResponse<CreateTenantResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateTenantResponse>>> CreateTenant(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateTenantCommand(request.Code, request.Name, request.Description, request.Metadata),
            cancellationToken);

        return CreatedFromResult(nameof(GetTenantById), new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.TenantManage)]
    public async Task<ActionResult<ApiResponse>> UpdateTenant(
        Guid id,
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateTenantCommand(id, request.Name, request.Description, request.Metadata), cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.TenantManage)]
    public async Task<ActionResult<ApiResponse>> DeleteTenant(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteTenantCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(OrganizationsPermissionCodes.TenantManage)]
    public async Task<ActionResult<ApiResponse>> ActivateTenant(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateTenantCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(OrganizationsPermissionCodes.TenantManage)]
    public async Task<ActionResult<ApiResponse>> DeactivateTenant(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateTenantCommand(id), cancellationToken);
        return FromResult(result);
    }
}
