# Typed UnitOfWork

```csharp
// ✅ Infrastructure
public sealed class ProductsUnitOfWork : EfUnitOfWork<ProductsDbContext>
{
    public ProductsUnitOfWork(ProductsDbContext dbContext) : base(dbContext) { }
}

services.AddScoped<ProductsUnitOfWork>();
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProductsUnitOfWork>());
```

```csharp
// ✅ Service injects typed UoW
private readonly ProductsUnitOfWork _unitOfWork;

// ❌ Never in module services
private readonly IUnitOfWork _unitOfWork;
```

`IUnitOfWork` chỉ cho `TransactionBehavior` (`IEnumerable<IUnitOfWork>`).

- Không gọi `SaveChangesAsync()` trong handler/service — `TransactionBehavior` commit
- Read: `QueryReadOnly()` / `AsNoTracking()`
- Return `Result` / `Result<T>` cho business failures; dùng centralized `ErrorCodes`

**Users:** inject `IdentityUnitOfWork`, không tạo DbContext riêng.
