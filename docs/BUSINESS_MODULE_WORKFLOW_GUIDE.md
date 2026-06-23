# Business Module Workflow Guide  
## Hướng dẫn thực hiện công việc khi phát sinh module/tính năng mới

> Tài liệu này dùng kèm với `BUSINESS_MODULE_RULES.md`.  
> `BUSINESS_MODULE_RULES.md` là bộ quy chuẩn bắt buộc.  
> File này là hướng dẫn thực hiện công việc theo từng bước khi phát sinh một module hoặc tính năng mới.

---

# 1. Mục tiêu tài liệu

Tài liệu này giúp Developer / Tech Lead / AI Coding Assistant / Cursor thực hiện một module hoặc tính năng mới theo đúng kiến trúc hiện tại của source base:

- .NET 8 Web API
- Modular Monolith
- PostgreSQL
- Multi-DbContext
- Typed UnitOfWork
- MediatR/CQRS
- Result pattern
- ApiResponse/PagedResponse
- FluentValidation
- Permission-based authorization
- AuditLog / ActivityLog
- Cache post-commit invalidation
- File integration
- Notification / BackgroundJob nếu cần
- Unit/Integration tests
- Documentation

Mục tiêu cuối cùng:  
**Mọi module/tính năng mới đều phải được triển khai nhất quán, an toàn, có test, có docs, không phá vỡ foundation hiện có.**

---

# 2. Khi nào dùng tài liệu này?

Dùng tài liệu này khi phát sinh:

- Module business mới  
  Ví dụ: `Products`, `Schools`, `Lessons`, `Orders`, `Documents`

- Tính năng lớn trong module hiện có  
  Ví dụ: thêm workflow approve, attach file, import/export, notification

- API mới có business rule rõ ràng

- Entity mới có persistence riêng

- Use case cần permission, validation, audit, cache, background job hoặc file storage

Không nhất thiết dùng full workflow này cho các thay đổi nhỏ như:

- Sửa text message
- Sửa typo docs
- Thêm field response đơn giản
- Refactor nhỏ không đổi behavior

Tuy nhiên, nếu thay đổi có ảnh hưởng đến business flow hoặc security, vẫn nên check theo checklist cuối tài liệu.

---

# 3. Quy trình tổng thể

Mỗi module/tính năng mới nên đi qua 10 bước:

```text
Step 01 — Requirement Clarification
Step 02 — Scope & Impact Analysis
Step 03 — Module Design
Step 04 — API & Permission Design
Step 05 — Data Model & Migration Design
Step 06 — Implementation
Step 07 — Cross-cutting Integration
Step 08 — Testing
Step 09 — Documentation
Step 10 — Review & Definition of Done
```

Không nên nhảy thẳng vào code khi chưa rõ:

- Entity chính là gì?
- Ai được dùng API?
- Permission nào cần có?
- Có cần cache không?
- Có cần audit/activity không?
- Có cần file/notification/background job không?
- Có ảnh hưởng module khác không?

---

# 4. Step 01 — Requirement Clarification

## 4.1. Mục tiêu

Xác định rõ module/tính năng mới cần giải quyết vấn đề gì.

## 4.2. Câu hỏi bắt buộc

Trước khi implement, cần trả lời:

| Câu hỏi | Ghi chú |
|---|---|
| Module/tính năng này phục vụ nghiệp vụ gì? | Mô tả ngắn gọn purpose |
| Entity chính là gì? | Product, Lesson, School, Document... |
| Người dùng nào thao tác? | Admin, User, Teacher, School Admin... |
| Có cần phân quyền không? | View/Create/Update/Delete/Manage... |
| Có cần trạng thái không? | Draft/Active/Inactive/Archived... |
| Có cần soft delete không? | Thường là có |
| Có cần audit log không? | Entity chính thường phải audit |
| Có cần activity log không? | Business action quan trọng phải có |
| Có cần cache không? | Read-heavy data nên cache |
| Có cần file không? | Attachment, document, image, import file... |
| Có cần notification/email không? | Notify khi publish/approve/assign... |
| Có cần background job không? | Retry, cleanup, scheduled processing |
| Có cần ownership/tenant không? | Tránh IDOR |
| API có cần pagination/filter không? | List API bắt buộc pagination |

## 4.3. Output của bước này

Tạo phần mô tả ngắn:

```text
Module Name:
Business Purpose:
Main Entity:
Main Actors:
Core Use Cases:
Out of Scope:
Security Notes:
```

Ví dụ:

```text
Module Name: Products
Business Purpose: Manage product master data.
Main Entity: Product
Main Actors: Admin, SuperAdmin
Core Use Cases:
- Create product
- Update product
- Activate/deactivate product
- Archive product
- Attach files to product
Out of Scope:
- Inventory
- Orders
- Pricing campaign
Security Notes:
- Permission-based access
- No per-user ownership in Phase 1
```

---

# 5. Step 02 — Scope & Impact Analysis

## 5.1. Mục tiêu

Xác định phạm vi triển khai và ảnh hưởng tới foundation hiện có.

## 5.2. Phân loại scope

| Loại | Ví dụ | Cách xử lý |
|---|---|---|
| New module | Products, Lessons | Tạo đủ 4 project |
| New feature in module | Product approval | Thêm vào module hiện có |
| Cross-module feature | Product file attach | Tích hợp Files module |
| System feature | Cleanup job | Tích hợp BackgroundJobs |

## 5.3. Impact checklist

Kiểm tra có ảnh hưởng tới:

- Auth / Permission
- ErrorCodes
- DbContext / Migration
- CacheKeys
- AuditChangeTrackingInterceptor
- ActivityLog
- Files module
- Notifications module
- BackgroundJobs
- Monitoring / Health checks
- Docs
- Tests
- Docker / production config

## 5.4. Output của bước này

Tạo bảng impact:

```markdown
| Area | Impact | Action |
|---|---|---|
| Permission | Yes | Add Product.* permissions |
| ErrorCodes | Yes | Add ProductErrors |
| DB | Yes | Add ProductsDbContext + migration |
| Cache | Yes | Add ProductDetail/ProductList keys |
| Files | Optional | Product file reference |
| Notification | No | Backlog |
| BackgroundJobs | No | Backlog |
| Docs | Yes | Add docs/PRODUCTS.md |
| Tests | Yes | Add Products tests |
```

---

# 6. Step 03 — Module Design

## 6.1. Mục tiêu

Thiết kế module theo đúng structure chuẩn.

## 6.2. Nếu là module mới

Bắt buộc tạo:

```text
src/Modules/{ModuleName}/{ModuleName}.Domain
src/Modules/{ModuleName}/{ModuleName}.Application
src/Modules/{ModuleName}/{ModuleName}.Infrastructure
src/Modules/{ModuleName}/{ModuleName}.Api
```

## 6.3. Dependency rule

```text
Domain
  -> BuildingBlocks.Domain

Application
  -> Domain
  -> BuildingBlocks.Application

Infrastructure
  -> Application
  -> Domain
  -> BuildingBlocks.Infrastructure

Api
  -> Application
  -> BuildingBlocks.Web
```

Không được tạo dependency ngược.

## 6.4. Thiết kế entity

Mỗi entity chính phải có:

- Id
- Business fields
- Status nếu cần
- IsActive nếu cần
- Audit fields
- Soft delete fields
- Domain methods

Ví dụ Product:

```text
Product
- Id
- Code
- Name
- Description
- Price
- Currency
- Status
- IsActive
- CreatedAt / CreatedBy
- UpdatedAt / UpdatedBy
- IsDeleted / DeletedAt / DeletedBy
```

## 6.5. Domain behavior

Không chỉ tạo property. Phải có behavior:

```text
Create
Update
Activate
Deactivate
Archive
SoftDelete
```

## 6.6. Output của bước này

Tạo thiết kế ngắn:

```markdown
## Domain Design

Entity: Product

Fields:
...

Statuses:
- Draft
- Active
- Inactive
- Archived

Domain Methods:
- Create
- Update
- Activate
- Deactivate
- Archive
- SoftDelete

Business Rules:
- Code immutable
- Price >= 0
- Deleted product cannot be updated
```

---

# 7. Step 04 — API & Permission Design

## 7.1. Mục tiêu

Thiết kế API và permission trước khi code.

## 7.2. API route convention

Bắt buộc dùng:

```text
/api/v1/{resource}
```

Ví dụ:

```text
GET    /api/v1/products
GET    /api/v1/products/{id}
POST   /api/v1/products
PUT    /api/v1/products/{id}
DELETE /api/v1/products/{id}
PATCH  /api/v1/products/{id}/activate
PATCH  /api/v1/products/{id}/deactivate
PATCH  /api/v1/products/{id}/archive
```

## 7.3. Permission design

Permission format:

```text
{Module}.View
{Module}.Create
{Module}.Update
{Module}.Delete
{Module}.Activate
{Module}.Deactivate
{Module}.Archive
```

Nếu có ownership/cross-user:

```text
{Module}.ViewAll
{Module}.Manage
```

Nếu có file:

```text
{Module}.AttachFile
{Module}.RemoveFile
```

## 7.4. API design table

Nên tạo bảng trước khi code:

```markdown
| Method | Route | Permission | Description |
|---|---|---|---|
| GET | /api/v1/products | Product.View | List products |
| GET | /api/v1/products/{id} | Product.View | Get product detail |
| POST | /api/v1/products | Product.Create | Create product |
| PUT | /api/v1/products/{id} | Product.Update | Update product |
| DELETE | /api/v1/products/{id} | Product.Delete | Soft delete product |
| PATCH | /api/v1/products/{id}/activate | Product.Activate | Activate product |
| PATCH | /api/v1/products/{id}/deactivate | Product.Deactivate | Deactivate product |
| PATCH | /api/v1/products/{id}/archive | Product.Archive | Archive product |
```

## 7.5. Response convention

Mọi API phải trả:

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

Không trả raw entity.

Không trả freestyle response.

---

# 8. Step 05 — Data Model & Migration Design

## 8.1. Mục tiêu

Thiết kế DB trước khi tạo migration.

## 8.2. DbContext

Mỗi module mới phải có:

```csharp
{ModuleName}DbContext
```

Ví dụ:

```csharp
ProductsDbContext
```

## 8.3. Schema/table

Ví dụ:

```text
Schema: products
Table: products.products
```

## 8.4. EF configuration bắt buộc

Mỗi entity phải có configuration:

- Table/schema
- Required fields
- Max length
- Decimal precision
- Indexes
- Unique filtered index nếu có code unique
- Soft delete filter/index nếu cần

Ví dụ:

```text
Code: max 100, required, unique where is_deleted = false
Name: max 255, required
Price: decimal(18,2)
Currency: max 10
Status: max 50
```

## 8.5. Migration

Tên migration:

```text
Add{ModuleName}Module
```

Ví dụ:

```text
AddProductsModule
```

## 8.6. Output của bước này

Tạo bảng DB design:

```markdown
| Column | Type | Required | Rule |
|---|---|---|---|
| id | uuid | yes | PK |
| code | varchar(100) | yes | unique where is_deleted=false |
| name | varchar(255) | yes |  |
| price | decimal(18,2) | yes | >= 0 |
| status | varchar(50) | yes | Draft/Active/Inactive/Archived |
```

---

# 9. Step 06 — Implementation

## 9.1. Thứ tự implement khuyến nghị

Không code lung tung. Nên làm theo thứ tự:

```text
1. Domain
2. Application DTOs
3. ErrorCodes
4. Validators
5. Infrastructure DbContext / EF Config
6. UnitOfWork / DI
7. Application Service / Handlers
8. Api Controller
9. Permission seeding
10. Cache integration
11. ActivityLog / AuditLog
12. Files/Notifications/Jobs if needed
13. Tests
14. Docs
```

## 9.2. Domain implementation

Bắt đầu từ entity và status constants.

Checklist:

- [ ] Entity kế thừa base phù hợp
- [ ] Có constructor/factory nếu convention cần
- [ ] Có domain methods
- [ ] Không hardcode status string rải rác
- [ ] Business rule nằm đúng layer

## 9.3. Application DTOs

Tạo request/response DTOs:

```text
CreateProductRequest
UpdateProductRequest
GetProductsRequest
ProductListItemResponse
ProductDetailResponse
CreateProductResponse
UpdateProductResponse
```

## 9.4. ErrorCodes

Thêm ProductErrors vào centralized ErrorCodes.

Ví dụ:

```text
Product.NotFound
Product.CodeAlreadyExists
Product.InvalidPrice
Product.AlreadyActive
Product.AlreadyInactive
Product.AlreadyDeleted
```

## 9.5. Validators

Dùng FluentValidation.

Bắt buộc validate:

- Required
- Max length
- Number range
- Date range
- PageIndex/PageSize
- Status values
- Currency nếu có

## 9.6. Infrastructure

Tạo:

- DbContext
- Entity configuration
- UnitOfWork
- Repository nếu cần
- DI extension
- Migration

## 9.7. Application service/handlers

Service/handler phải:

- Inject typed UnitOfWork
- Không inject plain IUnitOfWork
- Không gọi SaveChanges nếu TransactionBehavior quản lý
- Return Result/Result<T>
- Map entity sang DTO
- Dùng QueryReadOnly/AsNoTracking cho read
- Enqueue activity/cache post-commit cho write

## 9.8. Controller

Controller phải:

- Inherit BaseApiController
- Có [Authorize]
- Có [HasPermission]
- Dùng FromResult/FromPagedResult/CreatedFromResult
- Không expose entity
- Route đúng `/api/v1/...`

---

# 10. Step 07 — Cross-cutting Integration

## 10.1. Permission

Cần:

- Define constants
- Add vào PermissionCodes.All
- Seed idempotently
- Assign Admin/SuperAdmin theo convention hiện tại
- Add [HasPermission] vào controller

## 10.2. AuditLog

Cần:

- DbContext tích hợp audit interceptor
- Module mapping nếu cần
- Entity create/update/delete/status change được audit
- Không audit recursion

## 10.3. ActivityLog

Các business action phải enqueue post-commit:

```text
Created
Updated
Deleted
Activated
Deactivated
Archived
```

Không ghi success activity trước commit.

## 10.4. Cache

Nếu module có cache:

- Thêm CacheKeys
- Cache read detail/list
- Invalidate post-commit khi write
- Không cache failed response
- Không cache secret/binary

## 10.5. Files

Nếu có file:

- Dùng Files module
- Không tự lưu binary
- Reference bằng ModuleName/ReferenceType/ReferenceId
- Không expose StoragePath/StoredFileName
- Document ownership nếu chưa enforce

## 10.6. Notifications

Nếu có notification/email:

- Không gửi email trực tiếp trong transaction
- Persist message/event
- Gửi sau commit hoặc bằng background job
- Không expose SMTP raw error

## 10.7. BackgroundJobs

Nếu có job:

- Dùng Hangfire/BackgroundJobs foundation
- Có execution history nếu là job quan trọng
- Có permission cho manual trigger
- Không overlap nếu job nhạy cảm

---

# 11. Step 08 — Testing

## 11.1. Test strategy

Mỗi module/tính năng mới phải có test.

Test chia theo:

- Domain tests
- Application/service tests
- Validator tests
- Controller/helper tests nếu cần
- Integration tests nếu feasible

## 11.2. Required tests cho business module

Tối thiểu:

```text
Create success
Create validation fail
Duplicate code conflict
Update success
Immutable code behavior
Not found
Soft delete
Activate/deactivate/archive
Pagination
Cache hit
Cache invalidation
ActivityLog enqueue
Permission/ownership if applicable
```

## 11.3. Nếu có file

Thêm test:

```text
Attach file success
Missing file
Invalid file reference
Permission check
No binary caching
```

## 11.4. Nếu có notification/email

Thêm test:

```text
Notification created
Email message created
Email failure safe
No raw provider error
```

## 11.5. Nếu có background job

Thêm test:

```text
Job picks correct records
Job success updates status
Job failure records failure
Job respects max retry
No overlapping if required
```

## 11.6. Default test rule

Default `dotnet test` không được phụ thuộc:

- Real SMTP
- Real Redis
- Real PostgreSQL
- Real external API

Nếu cần PostgreSQL thì dùng:

- Skipped test rõ lý do
- Testcontainers nếu đã setup
- Docker profile riêng

---

# 12. Step 09 — Documentation

## 12.1. Module doc

Mỗi module phải có:

```text
docs/{MODULE}.md
```

Ví dụ:

```text
docs/PRODUCTS.md
```

## 12.2. Nội dung bắt buộc

```markdown
# Products Module

## Purpose

## Entity

## Status Model

## API Routes

## Permissions

## Error Codes

## Cache Behavior

## Audit / Activity Behavior

## File Integration

## Notification / Background Job

## Examples

## Known Limitations
```

## 12.3. Update shared docs

Nếu có thay đổi, update:

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

Future item phải ghi rõ:

```text
Future / Backlog
```

---

# 13. Step 10 — Review & Definition of Done

## 13.1. Review checklist

Trước khi xem module hoàn tất, check:

### Structure

- [ ] Có đủ Domain/Application/Infrastructure/Api
- [ ] Dependency đúng chiều
- [ ] Không circular dependency

### Domain

- [ ] Entity có behavior
- [ ] Business rule đúng layer
- [ ] Status constants rõ ràng

### Database

- [ ] DbContext riêng
- [ ] EF configuration đầy đủ
- [ ] Indexes đủ
- [ ] Migration build được

### UnitOfWork

- [ ] Typed UnitOfWork
- [ ] Registered as itself
- [ ] Registered as IUnitOfWork
- [ ] Service không inject plain IUnitOfWork

### API

- [ ] Route `/api/v1/...`
- [ ] BaseApiController
- [ ] DTO only
- [ ] Permission enforced
- [ ] No entity exposed

### Cross-cutting

- [ ] ErrorCodes
- [ ] Validation
- [ ] AuditLog
- [ ] ActivityLog post-commit
- [ ] Cache invalidation post-commit
- [ ] File integration nếu cần
- [ ] Notification/job nếu cần

### Security

- [ ] No IDOR
- [ ] No secret exposed
- [ ] No unsafe logging
- [ ] Permission check backend-side
- [ ] Ownership check nếu có

### Tests

- [ ] Unit tests
- [ ] Validator tests
- [ ] Service tests
- [ ] Integration tests nếu feasible
- [ ] `dotnet test` pass

### Docs

- [ ] Module doc
- [ ] Shared docs updated
- [ ] Known limitations documented

## 13.2. Definition of Done

Module/tính năng chỉ được xem là Done khi:

```text
dotnet build passes
dotnet test passes
Migration builds
Permissions seeded
APIs appear in Swagger Dev/Docker
Validation works
ErrorCodes work
AuditLog works
ActivityLog post-commit works
Cache works if implemented
Files/Notifications/Jobs integrated if required
Docs updated
No real secrets
No production hardening regression
```

Nếu item nào chưa làm, phải ghi rõ:

```text
Skipped:
Reason:
Backlog:
```

---

# 14. Cursor Execution Template

Khi giao việc cho Cursor, nên dùng format sau:

```text
Read and follow:
- docs/BUSINESS_MODULE_RULES.md
- docs/BUSINESS_MODULE_WORKFLOW_GUIDE.md
- docs/MODULE_DEVELOPMENT_GUIDE.md
- docs/API_CONVENTIONS.md
- docs/AUTHORIZATION.md
- docs/ERROR_CODES.md
- docs/POST_COMMIT_HOOKS.md

Task:
Implement {ModuleName} module.

Business purpose:
...

Scope:
...

Out of scope:
...

Required APIs:
...

Required permissions:
...

Required integrations:
- AuditLog: yes/no
- ActivityLog: yes/no
- Cache: yes/no
- Files: yes/no
- Notifications: yes/no
- BackgroundJobs: yes/no

Rules:
- Do not inject plain IUnitOfWork into module services.
- Use typed UnitOfWork.
- Do not expose EF entities.
- Use Result<T>, ApiResponse, PagedResponse.
- Use FluentValidation.
- Use centralized ErrorCodes.
- Use post-commit hooks for activity/cache.
- Add tests.
- Update docs.

Expected output:
- Build passes.
- Tests pass.
- Docs updated.
```

---

# 15. Business Module Request Template

Khi có module/tính năng mới, BA/PM/Tech Lead nên fill template này:

```markdown
# New Module / Feature Request

## 1. Name

## 2. Business Purpose

## 3. Main Users / Actors

## 4. Main Entity

## 5. Core Use Cases

## 6. API List

## 7. Permissions

## 8. Data Fields

## 9. Status / Workflow

## 10. Validation Rules

## 11. File Requirements

## 12. Notification Requirements

## 13. Background Job Requirements

## 14. Cache Requirements

## 15. Ownership / Tenant Rules

## 16. Reporting / Export Requirements

## 17. Out of Scope

## 18. Acceptance Criteria
```

---

# 16. Recommended Review Prompt

Sau khi Cursor implement xong module/tính năng, dùng prompt review:

```text
Review the implemented {ModuleName} module against:
- BUSINESS_MODULE_RULES.md
- BUSINESS_MODULE_WORKFLOW_GUIDE.md
- MODULE_DEVELOPMENT_GUIDE.md
- API_CONVENTIONS.md
- AUTHORIZATION.md
- ERROR_CODES.md
- POST_COMMIT_HOOKS.md

Check:
1. Module structure
2. Domain behavior
3. DbContext / EF configuration / migration
4. Typed UnitOfWork registration
5. DTOs / validators
6. ErrorCodes
7. Permissions
8. Controllers and response convention
9. AuditLog
10. ActivityLog post-commit
11. Cache post-commit invalidation
12. Files integration if any
13. Notifications/background jobs if any
14. Security / IDOR
15. Tests
16. Documentation
17. Build/test result

Return:
- What was implemented correctly
- What is missing
- What is risky
- What must be fixed before considering Done
- Whether the module is a good reference implementation
```

---

# 17. Common Mistakes to Avoid

## 17.1. Inject plain IUnitOfWork

Sai:

```csharp
public ProductService(IUnitOfWork unitOfWork)
```

Đúng:

```csharp
public ProductService(ProductsUnitOfWork unitOfWork)
```

## 17.2. SaveChanges thủ công

Sai:

```csharp
await _unitOfWork.SaveChangesAsync();
```

Đúng:

```csharp
return Result.Success(response);
```

`TransactionBehavior` sẽ commit.

## 17.3. Ghi ActivityLog trước commit

Sai:

```csharp
await _activityLogService.LogAsync("Product created");
```

Đúng:

```csharp
await _activityLogService.EnqueuePostCommitAsync("Product created");
```

## 17.4. Xóa cache trước commit

Sai:

```csharp
await _cache.RemoveAsync(key);
```

Đúng:

```csharp
_cacheOperationBuffer.EnqueueRemove(key);
```

## 17.5. Expose EF entity

Sai:

```csharp
return Ok(product);
```

Đúng:

```csharp
return FromResult(resultWithProductDto);
```

## 17.6. Hardcode error string

Sai:

```csharp
return Result.Failure("Product not found");
```

Đúng:

```csharp
return Result.Failure(ProductErrors.NotFound);
```

## 17.7. API không permission

Sai:

```csharp
[HttpPost]
public async Task<IActionResult> Create(...)
```

Đúng:

```csharp
[Authorize]
[HasPermission(ProductPermissions.Create)]
[HttpPost]
public async Task<IActionResult> Create(...)
```

## 17.8. List API không pagination

Sai:

```text
GET /api/v1/products/all
```

Đúng:

```text
GET /api/v1/products?pageIndex=1&pageSize=20
```

---

# 18. Suggested File Name

Lưu file này tại:

```text
docs/BUSINESS_MODULE_WORKFLOW_GUIDE.md
```

Và link từ:

```text
docs/README.md
docs/MODULE_DEVELOPMENT_GUIDE.md
docs/BUSINESS_MODULE_RULES.md
```
