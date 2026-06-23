# Module Development Guide

Step-by-step guide for adding a new module (example: **Products**).

Khi triển khai **business module mới**, đọc thêm:

- [BUSINESS_MODULE_RULES.md](./BUSINESS_MODULE_RULES.md) — quy chuẩn bắt buộc
- [BUSINESS_MODULE_WORKFLOW_GUIDE.md](./BUSINESS_MODULE_WORKFLOW_GUIDE.md) — quy trình từng bước

## Checklist

- [ ] Create `Products.Domain`, `.Application`, `.Infrastructure`, `.Api` projects
- [ ] Add projects to `ModularMonolith.sln`
- [ ] Define domain entity/aggregates
- [ ] Define `ProductsDbContext` + EF configurations
- [ ] Define typed `ProductsUnitOfWork`
- [ ] Register `ProductsUnitOfWork` **and** `IUnitOfWork` alias in DI
- [ ] **Do not** inject plain `IUnitOfWork` into module services — use `ProductsUnitOfWork`
- [ ] Define Application DTOs, commands, queries, validators
- [ ] Define `ProductsPermissionCodes` + add to Identity seed
- [ ] Define error codes in `ErrorCodes.cs`
- [ ] Implement handlers/services using typed UnitOfWork
- [ ] Add `ProductsController` under `/api/v1/products`
- [ ] Register module in `ApiHost/Program.cs` (`AddProductsInfrastructure`, `AddProductsApi`, presentation)
- [ ] Add EF migration
- [ ] Add audit entity mapping if tracked
- [ ] Add activity logging for mutations
- [ ] Add cache keys + invalidation if read-heavy
- [ ] Register post-commit hooks if needed
- [ ] Add unit/integration tests
- [ ] Document in [MODULES.md](./MODULES.md)

## Project structure

```
src/Modules/Products/
  Products.Domain/
    Products/
      Product.cs
  Products.Application/
    Products/
      CreateProduct/
        CreateProductCommand.cs
        CreateProductCommandValidator.cs
    Permissions/
      ProductsPermissionCodes.cs
    Abstractions/
      IProductService.cs
  Products.Infrastructure/
    Persistence/
      ProductsDbContext.cs
      ProductsUnitOfWork.cs
      Configurations/
    Services/
      ProductService.cs
    DependencyInjection.cs
  Products.Api/
    Controllers/
      ProductsController.cs
    Contracts/
      CreateProductRequest.cs
```

## Domain entity (sketch)

```csharp
public sealed class Product : Entity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    // factory methods, domain rules...
}
```

## Typed UnitOfWork

Use `EfUnitOfWork<TDbContext>` from BuildingBlocks (same pattern as `CategoriesUnitOfWork`):

```csharp
using BuildingBlocks.Infrastructure.Persistence;

public sealed class ProductsUnitOfWork : EfUnitOfWork<ProductsDbContext>
{
    public ProductsUnitOfWork(ProductsDbContext dbContext) : base(dbContext) { }
}
```

Register the typed UnitOfWork **and** expose it as `IUnitOfWork` so `TransactionBehavior` can save all contexts on commit. Module services inject **`ProductsUnitOfWork`**, not plain `IUnitOfWork`:

```csharp
services.AddDbContext<ProductsDbContext>(...);
services.AddScoped<ProductsUnitOfWork>();
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProductsUnitOfWork>());
```

## Command + handler

```csharp
public sealed record CreateProductCommand(string Code, string Name) : ICommand<ProductDetailResponse>;

public sealed class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, Result<ProductDetailResponse>>
{
    private readonly ProductService _service;
    // inject ProductsUnitOfWork via service, not IUnitOfWork directly
}
```

## Controller

```csharp
[Authorize]
[Route("api/v1/products")]
public sealed class ProductsController : BaseApiController
{
    [HttpPost]
    [HasPermission(ProductsPermissionCodes.Create)]
    public async Task<ActionResult<ApiResponse<ProductDetailResponse>>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateProductCommand(request.Code, request.Name), cancellationToken);
        return CreatedFromResult(nameof(GetById), new { id = result.Data?.Id }, result);
    }
}
```

## Permissions

```csharp
public static class ProductsPermissionCodes
{
    public const string View = "Product.View";
    public const string Create = "Product.Create";
    // ...
}
```

Seed in `IdentitySeeder` (idempotent `AnyAsync` check before insert).

## Error codes

```csharp
public static class ProductErrors
{
    public const string NotFound = "Product.NotFound";
    public const string CodeAlreadyExists = "Product.CodeAlreadyExists";
}
```

## Activity logging

```csharp
await _activityLogService.EnqueuePostCommitAsync(
    ActivityTypes.Create,
    $"Product created: {product.Code}",
    AuditLogConstants.Modules.Products,
    userId,
    userName,
    cancellationToken);
```

## Cache (optional)

```csharp
// Read
var cached = await _cache.GetAsync<ProductDetail>(CacheKeys.ProductDetail(id));

// Invalidate on mutation (post-commit buffer)
_cacheInvalidation.EnqueueRemove(CacheKeys.ProductDetail(id));
_cacheInvalidation.EnqueueRemoveByPrefix(CacheKeys.ProductListPrefix);
```

## Migration

```bash
dotnet ef migrations add InitialProducts \
  --project src/Modules/Products/Products.Infrastructure \
  --startup-project src/ApiHost \
  --context ProductsDbContext
```

## Tests

- Application: validator rules
- Infrastructure: service with in-memory DB or fakes
- Integration (optional): controller with `WebApplicationFactory`

Use fakes from `BuildingBlocks.Testing` — see [TESTING_GUIDE.md](./TESTING_GUIDE.md).

## Pitfalls

| Don't | Do |
|-------|-----|
| Inject `IUnitOfWork` in services | Inject `ProductsUnitOfWork` |
| Return EF entities from API | Map to response DTOs |
| Flush cache before commit | Use `ICacheOperationBuffer` + post-commit hook |
| Log activity before commit for transactional ops | Enqueue + post-commit flush |
| Reference another module's Infrastructure | Use Application abstractions or MediatR |

See [POST_COMMIT_HOOKS.md](./POST_COMMIT_HOOKS.md) and [ARCHITECTURE.md](./ARCHITECTURE.md).
