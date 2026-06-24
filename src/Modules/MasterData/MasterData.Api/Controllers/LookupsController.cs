using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MasterData.Api.Contracts;
using MasterData.Application.Dtos;
using MasterData.Application.Lookups;
using MasterData.Application.Permissions;
using MasterData.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MasterData.Api.Controllers;

[Authorize]
[Route("api/v1/lookups")]
public sealed class LookupsController : BaseApiController
{
    private readonly ISender _sender;

    public LookupsController(ISender sender) => _sender = sender;

    [HttpGet("{groupCode}")]
    [HasPermission(MasterDataPermissionCodes.LookupView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemDto>>>> GetLookupByGroupCode(
        string groupCode,
        [FromQuery] MasterDataScope scope = MasterDataScope.Global,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] DateTimeOffset? effectiveAt = null,
        [FromQuery] bool includeMetadata = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetLookupByGroupCodeQuery(groupCode, scope, tenantId, organizationId, includeInactive, effectiveAt, includeMetadata),
            cancellationToken);

        return FromResult(result);
    }

    [HttpGet]
    [HasPermission(MasterDataPermissionCodes.LookupView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupGroupDto>>>> GetLookups(
        [FromQuery] string groupCodes,
        [FromQuery] MasterDataScope scope = MasterDataScope.Global,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] DateTimeOffset? effectiveAt = null,
        [FromQuery] bool includeMetadata = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetLookupsQuery(groupCodes, scope, tenantId, organizationId, includeInactive, effectiveAt, includeMetadata),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPost("batch")]
    [HasPermission(MasterDataPermissionCodes.LookupView)]
    public async Task<ActionResult<ApiResponse<BatchLookupResponse>>> BatchLookup(
        [FromBody] BatchLookupRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MasterDataScope>(request.Scope, true, out var scope))
        {
            return BadRequest(ApiResponse.Fail("Lookup.InvalidScope", "Invalid scope value."));
        }

        var result = await _sender.Send(
            new BatchLookupQuery(
                request.GroupCodes,
                scope,
                request.TenantId,
                request.OrganizationId,
                request.IncludeInactive,
                request.EffectiveAt,
                request.IncludeMetadata),
            cancellationToken);

        return FromResult(result);
    }
}
