# API Layer

```csharp
[Authorize]
[Route("api/v1/products")]
public sealed class ProductsController : BaseApiController
{
    [HttpGet]
    [HasPermission(ProductsPermissionCodes.View)]
    public async Task<ActionResult<PagedResponse<ProductListItem>>> GetList(...)
        => FromPagedResult(await _sender.Send(query, ct));

    [HttpPost]
    [HasPermission(ProductsPermissionCodes.Create)]
    public async Task<ActionResult<ApiResponse<CreateProductResponse>>> Create(...)
        => CreatedFromResult(nameof(GetById), new { id = result.Data!.Id }, result);
}
```

- Route: `/api/v1/{resource}` (audit logs legacy: `/api/audit-logs`)
- Request/Response DTO riêng — **never** return EF entities
- `FromResult` / `FromPagedResult` / `CreatedFromResult`
- Mọi protected endpoint: `[Authorize]` + `[HasPermission]`
- List API: pagination (`pageIndex` default 1, `pageSize` default 20, max 100)
- Sort chỉ document nếu endpoint thực sự expose `sortBy`

Permissions: `{Module}.View|Create|Update|Delete|Activate|Deactivate|Archive`; thêm `ViewAll`/`Manage` nếu cross-user.
