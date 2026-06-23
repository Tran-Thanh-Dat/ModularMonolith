using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using Categories.Api.Contracts;
using Categories.Application.Categories;
using Categories.Application.Categories.ActivateCategory;
using Categories.Application.Categories.CreateCategory;
using Categories.Application.Categories.DeactivateCategory;
using Categories.Application.Categories.DeleteCategory;
using Categories.Application.Categories.GetCategories;
using Categories.Application.Categories.GetCategoryById;
using Categories.Application.Categories.UpdateCategory;
using Categories.Application.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Categories.Api.Controllers;

[Authorize]
[Route("api/v1/categories")]
public sealed class CategoriesController : BaseApiController
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HasPermission(CategoriesPermissionCodes.View)]
    public async Task<ActionResult<PagedResponse<CategoryListItemResponse>>> GetCategories(
        [FromQuery] string? keyword,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetCategoriesQuery(keyword, isActive, pageIndex, pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(CategoriesPermissionCodes.View)]
    public async Task<ActionResult<ApiResponse<CategoryDetailResponse>>> GetCategoryById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(CategoriesPermissionCodes.Create)]
    [ProducesResponseType(typeof(ApiResponse<CreateCategoryResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateCategoryResponse>>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateCategoryCommand(
                request.Code,
                request.Name,
                request.Description,
                request.SortOrder),
            cancellationToken);

        return CreatedFromResult(
            nameof(GetCategoryById),
            new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty },
            result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(CategoriesPermissionCodes.Update)]
    public async Task<ActionResult<ApiResponse>> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateCategoryCommand(id, request.Name, request.Description, request.SortOrder),
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(CategoriesPermissionCodes.Delete)]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(CategoriesPermissionCodes.Activate)]
    public async Task<ActionResult<ApiResponse>> ActivateCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateCategoryCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(CategoriesPermissionCodes.Deactivate)]
    public async Task<ActionResult<ApiResponse>> DeactivateCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateCategoryCommand(id), cancellationToken);
        return FromResult(result);
    }
}
