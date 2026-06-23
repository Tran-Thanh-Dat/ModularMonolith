# Business Module Development Rules  
## Modular Monolith .NET 8 API

## 1. Mục tiêu

Bộ rule này dùng để tạo một business module hoàn chỉnh trong source base .NET 8 Modular Monolith.

Mỗi module business phải chứng minh được toàn bộ foundation đang hoạt động end-to-end:

- Domain behavior
- Application service / CQRS
- Validation
- ErrorCodes
- Permissions
- Typed UnitOfWork
- Repository pattern
- TransactionBehavior
- Post-commit hooks
- AuditLog
- ActivityLog
- Cache
- Optional file integration
- Optional notification integration
- API response convention
- Tests
- Documentation

Module phải đủ chuẩn để dùng làm reference cho các module sau.

---

# 2. Module Structure Rule

Mỗi business module bắt buộc có 4 project:

```text
src/Modules/{ModuleName}/{ModuleName}.Domain
src/Modules/{ModuleName}/{ModuleName}.Application
src/Modules/{ModuleName}/{ModuleName}.Infrastructure
src/Modules/{ModuleName}/{ModuleName}.Api
```

Dependency direction bắt buộc:

```text
Domain
  → BuildingBlocks.Domain only

Application
  → Domain
  → BuildingBlocks.Application

Infrastructure
  → Application
  → Domain
  → BuildingBlocks.Infrastructure

Api
  → Application
  → BuildingBlocks.Web
```

Không được tạo circular dependency.

Không để business logic trong `Api`.

Không để EF-specific logic trong `Domain`.

Không expose EF entity ra API response.

---

# 3. Naming Rule

Tên module dùng PascalCase.

Ví dụ:

```text
Products
Schools
Lessons
Curriculums
Orders
Documents
```

Tên schema DB dùng snake_case hoặc lowercase theo convention hiện tại.

Ví dụ:

```text
products
schools
lessons
```

Tên table nên rõ ràng, không viết tắt khó hiểu.

Ví dụ:

```text
products.products
lessons.lessons
schools.schools
```

---

# 4. Domain Rule

Domain entity phải nằm trong `{ModuleName}.Domain`.

Entity phải kế thừa base entity phù hợp:

- `AuditableEntity`
- `SoftDeletableEntity`
- Hoặc base entity hiện có trong `BuildingBlocks.Domain`

Entity phải có domain methods, không chỉ là class chứa property.

Ví dụ:

```csharp
Create(...)
Update(...)
Activate()
Deactivate()
Archive()
SoftDelete()
```

Không update trực tiếp trạng thái quan trọng từ Application nếu trạng thái đó có business rule.

Sai:

```csharp
product.Status = "Active";
```

Đúng:

```csharp
product.Activate();
```

Business rules phải ưu tiên nằm trong Domain method nếu thuộc bản chất entity.

Ví dụ:

- Code không đổi sau khi tạo.
- Price không được âm.
- Deleted entity không được update.
- Entity inactive không được archive nếu rule không cho phép.

---

# 5. Status Rule

Nếu entity có trạng thái, phải dùng constants hoặc enum-like constants.

Ví dụ:

```csharp
ProductStatuses.Draft
ProductStatuses.Active
ProductStatuses.Inactive
ProductStatuses.Archived
```

Không hardcode string rải rác.

Sai:

```csharp
if (status == "Active")
```

Đúng:

```csharp
if (status == ProductStatuses.Active)
```

Nếu status ảnh hưởng business behavior, phải có domain method tương ứng.

---

# 6. Database / EF Rule

Mỗi module có DbContext riêng:

```csharp
{ModuleName}DbContext
```

DbContext nằm trong Infrastructure.

Mỗi entity phải có EF configuration riêng.

Bắt buộc cấu hình:

- Table name
- Schema
- Max length
- Required fields
- Decimal precision nếu có money/amount
- Indexes
- Soft-delete filtered unique index nếu có unique code

Ví dụ unique code:

```csharp
HasIndex(x => x.Code)
    .IsUnique()
    .HasFilter("is_deleted = false");
```

Read queries phải dùng:

```csharp
QueryReadOnly()
```

hoặc

```csharp
AsNoTracking()
```

Không dùng tracking cho query chỉ đọc.

Migration phải đặt tên rõ:

```text
AddProductsModule
AddSchoolsModule
AddLessonsModule
```

---

# 7. UnitOfWork Rule

Mỗi module phải có typed UnitOfWork:

```csharp
public sealed class ProductsUnitOfWork : EfUnitOfWork<ProductsDbContext>
```

DI bắt buộc:

```csharp
services.AddScoped<ProductsUnitOfWork>();
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProductsUnitOfWork>());
```

Module service phải inject typed UnitOfWork.

Đúng:

```csharp
private readonly ProductsUnitOfWork _unitOfWork;
```

Sai:

```csharp
private readonly IUnitOfWork _unitOfWork;
```

Lý do: source có multi-DbContext. Inject plain `IUnitOfWork` dễ resolve sai DbContext.

`IUnitOfWork` chỉ dùng cho `TransactionBehavior` thông qua `IEnumerable<IUnitOfWork>`.

---

# 8. Repository Rule

Module phải dùng repository/UoW pattern hiện có.

Không inject trực tiếp DbContext vào Application service nếu module convention không cho phép.

Không gọi `SaveChangesAsync()` trong service/handler nếu transaction đã do `TransactionBehavior` quản lý.

Sai:

```csharp
await _dbContext.SaveChangesAsync();
```

Đúng:

```csharp
return Result.Success(response);
```

`TransactionBehavior` sẽ commit.

Ngoại lệ chỉ khi module/job đặc biệt đã có pattern riêng và được document rõ.

---

# 9. Application Layer Rule

Application layer chứa:

- DTOs
- Validators
- Services
- Commands
- Queries
- Handlers
- Interfaces
- Error handling logic
- Mapping entity → response DTO

Không chứa EF configuration.

Không chứa Controller.

Không expose EF entity.

Mọi handler/service phải trả về:

```csharp
Result
Result<T>
PagedResponse<T>
```

Không throw exception cho business validation thông thường nếu project convention dùng `Result.Failure`.

Exception chỉ dùng cho lỗi bất thường hoặc pattern đã có sẵn.

---

# 10. DTO Rule

Mỗi API phải dùng Request DTO và Response DTO riêng.

Không dùng entity làm response.

Bắt buộc có các nhóm DTO:

```text
Create{Entity}Request
Update{Entity}Request
Get{Entity}ListRequest
{Entity}ListItemResponse
{Entity}DetailResponse
Create{Entity}Response
Update{Entity}Response
```

Response DTO không được chứa field nhạy cảm.

Không trả internal storage path, secret, password hash, token, connection string.

---

# 11. Validation Rule

Mọi request write phải có FluentValidation.

Bắt buộc validate:

- Required fields
- Max length
- Min/max numeric
- Date range
- PageIndex >= 1
- PageSize between 1 and 100
- Enum/status values
- Email format nếu có email
- File metadata nếu có file

Không để validation rơi xuống DB rồi mới lỗi.

Ví dụ:

```csharp
RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1);
RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
```

---

# 12. ErrorCode Rule

Mỗi module phải có error codes riêng trong centralized `ErrorCodes`.

Ví dụ:

```csharp
Product.NotFound
Product.CodeAlreadyExists
Product.InvalidStatus
Product.AlreadyActive
Product.AlreadyInactive
Product.AlreadyDeleted
Product.InvalidPrice
```

Không hardcode error string rải rác.

Sai:

```csharp
return Result.Failure("Product not found");
```

Đúng:

```csharp
return Result.Failure(ProductErrors.NotFound);
```

HTTP mapping phải theo convention:

| Error | HTTP |
|---|---|
| NotFound | 404 |
| AlreadyExists / Conflict | 409 |
| Unauthorized | 401 |
| Forbidden | 403 |
| Validation / invalid business input | 400 |
| Unknown | 500 |

---

# 13. Permission Rule

Mỗi module business phải có permission riêng.

Format:

```text
{Module}.View
{Module}.Create
{Module}.Update
{Module}.Delete
{Module}.Activate
{Module}.Deactivate
{Module}.Archive
```

Nếu có file:

```text
{Module}.AttachFile
{Module}.RemoveFile
```

Nếu có admin/cross-user operation:

```text
{Module}.ViewAll
{Module}.Manage
```

Permissions phải được:

- Define trong permission constants
- Add vào `PermissionCodes.All`
- Seed idempotently
- Assign cho Admin/SuperAdmin theo convention hiện tại
- Apply bằng `[HasPermission]` trên controller

Không được chỉ check permission trong UI/frontend.

Backend phải enforce permission.

---

# 14. Controller Rule

Controller nằm trong `{ModuleName}.Api`.

Route bắt buộc:

```text
/api/v1/{resource-name}
```

Ví dụ:

```text
/api/v1/products
/api/v1/products/{id}
```

Controller phải:

- Inherit `BaseApiController`
- Dùng `[Authorize]`
- Dùng `[HasPermission]`
- Dùng `FromResult`
- Dùng `FromPagedResult`
- Dùng `CreatedFromResult`
- Không access `result.Value` trực tiếp nếu chưa check success
- Không trả EF entity

Ví dụ:

```csharp
return FromResult(result);
```

hoặc:

```csharp
return CreatedFromResult(result, nameof(GetById), new { id = result.Data.Id });
```

---

# 15. API Response Rule

Mọi API response phải theo chuẩn:

```json
{
  "success": true,
  "code": "Common.Success",
  "message": "Success",
  "data": {},
  "traceId": "...",
  "timestamp": "..."
}
```

Paged response phải có metadata:

```json
{
  "items": [],
  "pageIndex": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5
}
```

Không trả response freestyle.

Không trả raw exception.

---

# 16. Cache Rule

Nếu module có read-heavy data, phải xem xét cache.

Cache key phải khai báo trong `CacheKeys`.

Ví dụ:

```csharp
ProductDetail(id)
ProductList(queryHash)
```

Không tự build cache key rải rác bằng string thủ công.

Cache invalidation phải chạy post-commit.

Khi create/update/delete/activate/deactivate/archive:

```text
Remove detail key
Remove list prefix
```

Không invalidate cache trước commit.

Nếu transaction rollback, cache không được bị xóa sai.

Không cache:

- Access token
- Refresh token
- Password hash
- File binary stream
- Secret/config sensitive data

---

# 17. ActivityLog Rule

Mọi successful business action quan trọng phải ghi ActivityLog post-commit.

Ví dụ:

```text
Product created
Product updated
Product deleted
Product activated
Product archived
```

Không ghi success activity log trước khi transaction commit.

Dùng:

```csharp
EnqueuePostCommitAsync(...)
```

Nếu transaction fail, không được có false success activity log.

ModuleName phải đúng:

```text
Products
Schools
Lessons
```

---

# 18. AuditLog Rule

Mọi entity chính của module phải được audit qua EF ChangeTracking.

Bắt buộc:

- DbContext kế thừa hoặc tích hợp audit interceptor theo convention hiện tại.
- Module mapping được thêm nếu cần.
- Create/update/delete/status changes được audit.
- Soft delete được ghi nhận đúng.
- Không audit recursion.
- Sensitive data masking vẫn hoạt động.

AuditLog dùng cho metadata/entity changes.

ActivityLog dùng cho business action/user operation.

Không lẫn hai loại log.

---

# 19. Post-Commit Rule

Các side effects sau phải chạy sau commit:

- ActivityLog success
- Cache invalidation
- Cache set nếu phụ thuộc transaction
- Notification success nếu liên quan transaction
- File compensation clear

Các side effects sau không được chạy trước commit:

- Gửi email business critical
- Ghi success activity
- Xóa cache chính thức
- Mark external operation success

Rollback phải:

- Clear pending activity log
- Clear pending cache operation
- Compensate pending file nếu cần

---

# 20. File Integration Rule

Nếu business module cần file, ưu tiên dùng Files module hiện có.

Không tự lưu binary trong business module.

Không duplicate file storage logic.

Dùng `FileResource` với:

```text
ModuleName = {ModuleName}
ReferenceType = {EntityName}
ReferenceId = {EntityId}
```

Ví dụ:

```text
ModuleName = Products
ReferenceType = Product
ReferenceId = productId
```

Business module có thể:

- Expose API attach file
- Expose API list files
- Hoặc document frontend dùng Files API filter theo reference

Nếu có ownership/tenant rule, business module phải enforce. Files module hiện tại permission-based, chưa tự enforce per-user ownership.

Không trả:

- StoragePath
- StoredFileName
- Absolute path

---

# 21. Notification / Email Rule

Notification/email là optional.

Chỉ dùng khi business event thật sự cần.

Ví dụ:

- Product activated
- Document approved
- Lesson published
- User assigned

Không gửi email trực tiếp bên trong transaction nếu email phụ thuộc DB commit.

Ưu tiên:

- Persist event/message
- Gửi sau commit
- Hoặc để BackgroundJob retry

Không expose SMTP/provider raw error cho client.

---

# 22. Background Job Rule

Nếu module cần job nền:

- Không tự tạo scheduler riêng.
- Dùng BackgroundJobs/Hangfire foundation.
- Job phải có execution history nếu là job quan trọng.
- Job failure phải được ghi nhận.
- Không để job overlap nếu có rủi ro.
- Không expose secret trong job result.

Manual trigger API phải có permission riêng.

---

# 23. Security Rule

Mỗi module business phải tuân thủ:

- `[Authorize]` cho API không public.
- `[HasPermission]` cho operation.
- Không expose secret.
- Không log password/token/cookie.
- Không log binary content.
- Không expose internal path.
- Validate request size/file size.
- Validate ownership nếu có user/tenant scope.
- Avoid IDOR.
- Avoid mass assignment.
- No raw SQL nếu không cần.
- Nếu dùng raw SQL, parameterized query bắt buộc.

---

# 24. IDOR / Ownership Rule

Nếu entity thuộc user/tenant/school/company, service layer phải enforce ownership.

Không chỉ dựa vào frontend.

Ví dụ:

```text
User chỉ được xem record của mình.
Admin cần Permission.ViewAll.
Manager cần Permission.Manage.
```

Pattern đề xuất:

```text
View own item → {Module}.View
View all items → {Module}.ViewAll
Modify own item → {Module}.Update
Modify others → {Module}.Manage
```

Nếu module chưa có ownership, phải document rõ:

```text
Current module is permission-based only. Ownership is future/backlog.
```

---

# 25. Query / Pagination Rule

List API phải support pagination.

Default:

```text
pageIndex = 1
pageSize = 20
max pageSize = 100
```

Filter phải rõ ràng.

Ví dụ:

```text
keyword
status
isActive
fromDate
toDate
```

Sorting là endpoint-specific.

Không claim endpoint support `SortBy/SortDirection` nếu chưa implement.

Read query phải dùng `AsNoTracking` hoặc `QueryReadOnly`.

---

# 26. Testing Rule

Mỗi module business phải có test tối thiểu.

Required tests:

## Domain / Application

- Create success
- Create validation fail
- Duplicate code conflict
- Update success
- Immutable code behavior
- Delete / soft delete
- Activate/deactivate/archive transitions
- Not found
- Pagination
- Cache hit
- Cache invalidation
- ActivityLog enqueue post-commit
- Permission/ownership if applicable

## API / Integration nếu feasible

- Unauthorized → 401
- Missing permission → 403
- Duplicate → 409
- NotFound → 404
- Success create → 201
- List pagination → 200

## File nếu có

- Attach file success
- Missing file
- Invalid file
- Permission check
- Cache invalidation

Không test bằng real SMTP.

Không phụ thuộc real Redis/PostgreSQL trong default test run trừ khi dùng Testcontainers và document rõ.

---

# 27. Documentation Rule

Mỗi module business phải có docs.

Tối thiểu:

```text
docs/{MODULE}.md
```

Phải document:

- Purpose
- Entity fields
- Status model
- API routes
- Request/response examples
- Permissions
- Error codes
- Cache behavior
- Audit/Activity behavior
- File integration if any
- Notification integration if any
- Background job if any
- Known limitations

Update các docs chung nếu cần:

```text
docs/MODULES.md
docs/AUTHORIZATION.md
docs/ERROR_CODES.md
docs/CACHING.md
docs/FILES.md
docs/BACKGROUND_JOBS.md
docs/README.md
```

Không document feature chưa implement như đã hoàn tất.

Future item phải ghi rõ là backlog/future.

---

# 28. Migration Rule

Mỗi module có migration riêng.

Migration name phải rõ:

```text
AddProductsModule
AddLessonsModule
```

Migration phải build được.

Không chỉnh tay migration nếu không cần.

Production không auto-migrate.

Dev/Docker có thể auto-migrate theo config hiện tại.

---

# 29. DI Registration Rule

Mỗi module phải có extension methods:

```csharp
services.AddProductsInfrastructure(configuration);
services.AddProductsPresentation();
```

ApiHost chỉ gọi extension.

Không để Program.cs phình to.

Program.cs chỉ orchestration, không chứa module detail.

---

# 30. Swagger Rule

Mỗi controller phải có tag rõ.

Swagger phải hiển thị đúng:

- Route
- Request DTO
- Response DTO
- Auth requirement
- JWT Bearer

Không expose secret trong example.

Swagger Production vẫn theo policy hiện tại: disabled/protected.

---

# 31. Logging Rule

Log phải có:

- CorrelationId
- UserId nếu có
- ModuleName nếu phù hợp
- Safe message

Không log:

- Password
- Token
- Cookie
- Secret
- Connection string
- File binary
- Raw SMTP error ra client

Exception detail chỉ ở server log, không trả client production.

---

# 32. Performance Rule

Business module phải tránh:

- N+1 query
- Tracking query không cần thiết
- Load toàn bộ data rồi filter memory
- Unbounded list endpoint
- Large response không pagination
- Cache key quá rộng
- Cache stale không invalidation

List API bắt buộc pagination.

Read-heavy API nên có cache nếu phù hợp.

---

# 33. Production Rule

Module mới không được phá các production guarantees hiện có:

- Build pass
- Test pass
- No real secrets
- No unsafe Swagger Production
- No public Hangfire
- No unprotected monitoring details
- No wildcard CORS in Production
- No weak JWT config
- No unvalidated upload path
- No hidden startup dependency

---

# 34. Code Review Checklist

Trước khi merge module mới, kiểm tra:

## Structure

- [ ] Có đủ Domain/Application/Infrastructure/Api
- [ ] Dependency đúng chiều
- [ ] Không circular dependency

## Domain

- [ ] Entity có behavior
- [ ] Business rules nằm đúng layer
- [ ] Status constants rõ ràng

## DB

- [ ] DbContext riêng
- [ ] EF config rõ
- [ ] Indexes đủ
- [ ] Migration build được

## UoW

- [ ] Typed UnitOfWork
- [ ] Registered as itself
- [ ] Registered as IUnitOfWork
- [ ] Service không inject plain IUnitOfWork

## API

- [ ] Route `/api/v1/...`
- [ ] BaseApiController
- [ ] DTO only
- [ ] FromResult/FromPagedResult/CreatedFromResult
- [ ] No EF entity exposed

## Security

- [ ] Authorize
- [ ] HasPermission
- [ ] No IDOR
- [ ] No secret logged/exposed

## Cross-cutting

- [ ] ErrorCodes
- [ ] Validation
- [ ] AuditLog
- [ ] ActivityLog post-commit
- [ ] Cache post-commit invalidation
- [ ] File integration if needed
- [ ] Notification/job if needed

## Tests

- [ ] Unit tests
- [ ] Application tests
- [ ] Integration tests if feasible
- [ ] Build/test pass

## Docs

- [ ] Module doc
- [ ] Shared docs updated
- [ ] No inaccurate feature claims

---

# 35. Definition of Done

Một business module được xem là hoàn chỉnh khi:

- Build pass
- Test pass
- Migration build/apply được
- API xuất hiện trong Swagger Dev/Docker
- Permission seed đúng
- CRUD/status APIs hoạt động
- Validation đầy đủ
- ErrorCodes đầy đủ
- AuditLog hoạt động
- ActivityLog post-commit hoạt động
- Cache read/invalidation hoạt động nếu implemented
- File integration hoạt động hoặc được document rõ
- Security check pass
- Docs cập nhật
- Không phá regression module khác

If any mandatory item is skipped, it must be documented with reason and backlog.
