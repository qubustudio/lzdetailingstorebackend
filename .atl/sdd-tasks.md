# SDD Tasks — LZ Detailing Store Backend

## Date: 2026-08-12
## Phase: Task Breakdown
## Total Tasks: 78

---

## Phase 1: Foundation (15 tasks)

#### Task 1.01: Create Solution Structure
- **Files**: `LzDetailingStore.sln`, `src/LzDetailingStore.Api/`, `src/LzDetailingStore.Application/`, `src/LzDetailingStore.Domain/`, `src/LzDetailingStore.Infrastructure/`, `src/LzDetailingStore.SharedKernel/`, `tests/LzDetailingStore.UnitTests/`, `tests/LzDetailingStore.IntegrationTests/`, `tests/LzDetailingStore.ArchTests/`
- **REQ**: REQ-001
- **Do**: Create .NET 10 solution with 5 source projects and 3 test projects. Set up project references enforcing dependency rule: Api→Application+Infrastructure, Application→Domain+SharedKernel, Infrastructure→Application+Domain+SharedKernel, Domain→SharedKernel. Add `Directory.Build.props` with shared settings (LangVersion 14, Nullable enable, ImplicitUsings). Add `.editorconfig`.
- [ ] Complete

#### Task 1.02: Docker Compose Setup
- **Files**: `docker/docker-compose.yml`, `docker/docker-compose.override.yml`, `docker/.env.example`, `docker/Dockerfile`
- **REQ**: REQ-002
- **Do**: Create Docker Compose with services: `db` (pgvector/pgvector:pg16 on 5432), `seq` (datalust/seq on 5341), `api` (build from Dockerfile on 5000). Multi-stage Dockerfile: sdk:10.0 build → aspnet:10.0 runtime. Environment variables for connection strings.
- [ ] Complete

#### Task 1.03: SharedKernel — BaseEntity and AuditableEntity
- **Files**: `src/LzDetailingStore.SharedKernel/BaseEntity.cs`, `src/LzDetailingStore.SharedKernel/AuditableEntity.cs`, `src/LzDetailingStore.SharedKernel/IDomainEvent.cs`
- **REQ**: REQ-004
- **Do**: Create `BaseEntity` (Guid Id, DateTime CreatedAt/UpdatedAt, DomainEvents list with Add/Clear methods). Create `AuditableEntity : BaseEntity` (Guid? CreatedBy/UpdatedBy). Create `IDomainEvent : INotification` (DateTime OccurredOn).
- [ ] Complete

#### Task 1.04: SharedKernel — Result Pattern
- **Files**: `src/LzDetailingStore.SharedKernel/Result.cs`, `src/LzDetailingStore.SharedKernel/Error.cs`, `src/LzDetailingStore.SharedKernel/PagedResult.cs`
- **REQ**: REQ-004
- **Do**: Create `Error` record (Code, Message). Create `Result` class (IsSuccess, IsFailure, Error?, StatusCode?) with static factories: Success(), Failure(), NotFound(), Conflict(), Forbidden(). Create `Result<T> : Result` with Value. Create `PagedResult<T>` record (Data, Total, Page, TotalPages).
- [ ] Complete

#### Task 1.05: SharedKernel — Interfaces and Enums
- **Files**: `src/LzDetailingStore.SharedKernel/Interfaces/IRepository.cs`, `src/LzDetailingStore.SharedKernel/Interfaces/IUnitOfWork.cs`, `src/LzDetailingStore.SharedKernel/Enums/OrderStatus.cs`, `src/LzDetailingStore.SharedKernel/Enums/CourseLevel.cs`, `src/LzDetailingStore.SharedKernel/Enums/ImportStatus.cs`, `src/LzDetailingStore.SharedKernel/Enums/BrandCategory.cs`
- **REQ**: REQ-004
- **Do**: Create `IRepository<T>` (GetByIdAsync, GetAllAsync, AddAsync, Update, Remove). Create `IUnitOfWork` (SaveChangesAsync). Create enums: OrderStatus, CourseLevel, CourseStatus, BrandCategory, ImportStatus.
- [ ] Complete

#### Task 1.06: EF Core Configuration — AppDbContext
- **Files**: `src/LzDetailingStore.Infrastructure/Persistence/AppDbContext.cs`, `src/LzDetailingStore.Infrastructure/LzDetailingStore.Infrastructure.csproj` (NuGet packages)
- **REQ**: REQ-003
- **Do**: Install Npgsql.EntityFrameworkCore.PostgreSQL, EFCore.NamingConventions, Npgsql.EntityFrameworkCore.PostgreSQL.pgvector. Create `AppDbContext : DbContext, IUnitOfWork`. Override `OnModelCreating` (HasPostgresExtension("vector"), ApplyConfigurationsFromAssembly). Override `SaveChangesAsync` to auto-set CreatedAt/UpdatedAt timestamps.
- [ ] Complete

#### Task 1.07: Generic Repository
- **Files**: `src/LzDetailingStore.Infrastructure/Persistence/Repositories/Repository.cs`
- **REQ**: REQ-003
- **Do**: Create `Repository<T> : IRepository<T>` using primary constructor with AppDbContext. Implement all interface methods using DbSet<T>.
- [ ] Complete

#### Task 1.08: Global Exception Handler Middleware
- **Files**: `src/LzDetailingStore.Api/Middleware/GlobalExceptionMiddleware.cs`
- **REQ**: REQ-005
- **Do**: Create middleware that catches all exceptions, logs via Serilog with CorrelationId, returns JSON `{ error: "INTERNAL_ERROR", message, statusCode: 500 }`. In Development, include exception message; in Production, generic message only.
- [ ] Complete

#### Task 1.09: Correlation ID Middleware
- **Files**: `src/LzDetailingStore.Api/Middleware/CorrelationIdMiddleware.cs`
- **REQ**: REQ-010
- **Do**: Read X-Correlation-ID header or generate new GUID. Store in HttpContext.Items. Set response header. Push to Serilog LogContext.
- [ ] Complete

#### Task 1.10: Serilog Configuration
- **Files**: `src/LzDetailingStore.Api/Program.cs` (partial), `src/LzDetailingStore.Api/appsettings.json`, `src/LzDetailingStore.Api/appsettings.Development.json`
- **REQ**: REQ-008
- **Do**: Install Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.Seq. Configure in Program.cs with enrichers (CorrelationId, MachineName). Console sink always; Seq sink in Development via appsettings.
- [ ] Complete

#### Task 1.11: Health Checks
- **Files**: `src/LzDetailingStore.Api/Program.cs` (partial)
- **REQ**: REQ-007
- **Do**: Install AspNetCore.HealthChecks.NpgSql. Add `AddHealthChecks().AddNpgSql()`. Map `/health/live` (Predicate = _ => false) and `/health/ready` (includes DB check).
- [ ] Complete

#### Task 1.12: API Versioning and Scalar
- **Files**: `src/LzDetailingStore.Api/Program.cs` (partial), `src/LzDetailingStore.Api/LzDetailingStore.Api.csproj`
- **REQ**: REQ-006, REQ-009
- **Do**: Install Scalar.AspNetCore, Microsoft.AspNetCore.OpenApi. Configure OpenAPI document generation. Add Scalar UI endpoint. All routes prefixed with `/api/v1/`.
- [ ] Complete

#### Task 1.13: Endpoint Group Infrastructure
- **Files**: `src/LzDetailingStore.Api/Endpoints/IEndpointGroup.cs`, `src/LzDetailingStore.Api/Extensions/EndpointExtensions.cs`, `src/LzDetailingStore.Api/Extensions/ResultExtensions.cs`
- **REQ**: REQ-006
- **Do**: Create `IEndpointGroup` interface (MapEndpoints). Create `EndpointExtensions.MapApiEndpoints()` auto-discovery via reflection. Create `ResultExtensions.ToApiResponse()` mapping Result → IResult HTTP responses.
- [ ] Complete

#### Task 1.14: MediatR Pipeline Behaviors
- **Files**: `src/LzDetailingStore.Application/Behaviors/LoggingBehavior.cs`, `src/LzDetailingStore.Application/Behaviors/ValidationBehavior.cs`, `src/LzDetailingStore.Application/LzDetailingStore.Application.csproj`
- **REQ**: REQ-055
- **Do**: Install MediatR, FluentValidation, FluentValidation.DependencyInjectionExtensions. Create `LoggingBehavior<TRequest, TResponse>`. Create `ValidationBehavior<TRequest, TResponse>` that collects errors and returns Result.Failure.
- [ ] Complete

#### Task 1.15: DI Wiring and Compilation Verification
- **Files**: `src/LzDetailingStore.Api/Program.cs`, `src/LzDetailingStore.Application/DependencyInjection.cs`, `src/LzDetailingStore.Infrastructure/DependencyInjection.cs`
- **REQ**: REQ-001
- **Do**: Create `AddApplicationServices()` (MediatR, FluentValidation, Mapster scan). Create `AddInfrastructureServices(configuration)` (DbContext, Repository, UnitOfWork). Wire in Program.cs with middleware pipeline order. Verify `dotnet build` succeeds with zero errors.
- [ ] Complete

---

## Phase 2: Identity & Access (12 tasks)

#### Task 2.01: Identity Entities
- **Files**: `src/LzDetailingStore.Domain/Entities/Identity/User.cs`, `Role.cs`, `Permission.cs`, `Module.cs`, `OAuthAccount.cs`, `RefreshToken.cs`, `UserRole.cs`, `RolePermission.cs`
- **REQ**: REQ-016
- **Do**: Create all identity entities per design. User extends AuditableEntity. Include navigation properties.
- [ ] Complete

#### Task 2.02: Identity EF Configurations
- **Files**: `src/LzDetailingStore.Infrastructure/Persistence/Configurations/Identity/UserConfiguration.cs`, `RoleConfiguration.cs`, `PermissionConfiguration.cs`, `ModuleConfiguration.cs`, `OAuthAccountConfiguration.cs`, `RefreshTokenConfiguration.cs`
- **REQ**: REQ-003, REQ-016
- **Do**: Fluent API configs with max lengths, unique indexes, FK relationships, composite PKs for UserRole/RolePermission.
- [ ] Complete

#### Task 2.03: Seed Data
- **Files**: `src/LzDetailingStore.Infrastructure/Persistence/Seed/SeedData.cs`
- **REQ**: REQ-018
- **Do**: Seed 3 roles (SuperAdmin, Admin, Cliente), 10 modules, 40 permissions (4 per module: ver, crear, editar, eliminar), role-permission assignments. SuperAdmin gets all, Admin gets all except usuarios/roles management, Cliente gets none.
- [ ] Complete

#### Task 2.04: JWT Service and Password Hasher
- **Files**: `src/LzDetailingStore.Infrastructure/Auth/JwtService.cs`, `src/LzDetailingStore.Infrastructure/Auth/BcryptPasswordHasher.cs`, `src/LzDetailingStore.Application/Interfaces/IJwtService.cs`, `src/LzDetailingStore.Application/Interfaces/IPasswordHasher.cs`
- **REQ**: REQ-012
- **Do**: IJwtService: GenerateTokenAsync(User, roles) → (accessToken, expiresIn). IPasswordHasher: Hash(password), Verify(password, hash). Implementations: HMAC-SHA256 JWT with 24h expiry, BCrypt cost 12.
- [ ] Complete

#### Task 2.05: Registration Command
- **Files**: `src/LzDetailingStore.Application/Features/Identity/Commands/RegisterUser/RegisterUserCommand.cs`, `RegisterUserCommandHandler.cs`, `RegisterUserCommandValidator.cs`, `src/LzDetailingStore.Application/Features/Identity/DTOs/AuthResponseDto.cs`
- **REQ**: REQ-011
- **Do**: Command: Email, Password, Nombre, Telefono?. Validator: email format, password ≥6 chars, name required. Handler: check email unique → hash password → create user → assign Cliente role → generate JWT → return AuthResponseDto.
- [ ] Complete

#### Task 2.06: Login Command
- **Files**: `src/LzDetailingStore.Application/Features/Identity/Commands/Login/LoginCommand.cs`, `LoginCommandHandler.cs`, `LoginCommandValidator.cs`
- **REQ**: REQ-012
- **Do**: Command: Email, Password. Handler: find user by email → verify password → generate JWT + refresh token → save refresh token (hashed) → return AuthResponseDto. Generic error message on failure (no email enumeration).
- [ ] Complete

#### Task 2.07: Refresh Token Command
- **Files**: `src/LzDetailingStore.Application/Features/Identity/Commands/RefreshToken/RefreshTokenCommand.cs`, `RefreshTokenCommandHandler.cs`
- **REQ**: REQ-013
- **Do**: Command: RefreshToken string. Handler: hash token → find in DB → check not revoked/expired → revoke old → generate new pair → if revoked token reused → revoke ALL user tokens (family invalidation).
- [ ] Complete

#### Task 2.08: OAuth Login Command
- **Files**: `src/LzDetailingStore.Application/Features/Identity/Commands/LoginOAuth/LoginOAuthCommand.cs`, `LoginOAuthCommandHandler.cs`, `src/LzDetailingStore.Application/Interfaces/IOAuthValidator.cs`, `src/LzDetailingStore.Infrastructure/Auth/OAuthValidator.cs`
- **REQ**: REQ-014
- **Do**: Command: Provider, Token. IOAuthValidator validates with provider endpoint. Handler: validate → find/create user + OAuthAccount → generate JWT. Support Google, Apple, Microsoft.
- [ ] Complete

#### Task 2.09: Password Recovery and Reset
- **Files**: `src/LzDetailingStore.Application/Features/Identity/Commands/RecoverPassword/RecoverPasswordCommand.cs`, `RecoverPasswordCommandHandler.cs`, `src/LzDetailingStore.Application/Features/Identity/Commands/ResetPassword/ResetPasswordCommand.cs`, `ResetPasswordCommandHandler.cs`, `src/LzDetailingStore.Application/Interfaces/IEmailService.cs`, `src/LzDetailingStore.Infrastructure/Services/NoOpEmailService.cs`
- **REQ**: REQ-015
- **Do**: RecoverPassword: generates 1h token, calls IEmailService (no-op). ResetPassword: validates token + expiry, updates password hash, revokes all refresh tokens. IEmailService interface + NoOpEmailService that logs.
- [ ] Complete

#### Task 2.10: Permission Authorization Handler
- **Files**: `src/LzDetailingStore.Infrastructure/Auth/PermissionAuthorizationHandler.cs`, `src/LzDetailingStore.Api/Auth/PermissionRequirement.cs`, `src/LzDetailingStore.Api/Auth/PermissionPolicyProvider.cs`
- **REQ**: REQ-017
- **Do**: PermissionRequirement(string Permission). Handler queries DB for user's permissions via roles. PolicyProvider dynamically creates policies from permission strings (e.g., "productos.crear").
- [ ] Complete

#### Task 2.11: Profile and User Management
- **Files**: `src/LzDetailingStore.Application/Features/Identity/Queries/GetProfile/`, `src/LzDetailingStore.Application/Features/Identity/Commands/UpdateProfile/`, `src/LzDetailingStore.Application/Features/Identity/Queries/GetUsers/`, DTOs
- **REQ**: REQ-019
- **Do**: GetProfile query returns ProfileDto (name, email, phone, avatar, roles). UpdateProfile command (name, phone). Admin GetUsers query with pagination.
- [ ] Complete

#### Task 2.12: Identity Endpoints and DI
- **Files**: `src/LzDetailingStore.Api/Endpoints/Identity/AuthEndpoints.cs`, `src/LzDetailingStore.Api/Endpoints/Admin/UserEndpoints.cs`, `src/LzDetailingStore.Api/Endpoints/Admin/RoleEndpoints.cs`
- **REQ**: REQ-011-019
- **Do**: Map all auth endpoints (/api/v1/auth/*). Map admin user/role endpoints. Wire identity services in DI. Verify compilation.
- [ ] Complete

---

## Phase 3: Catalog (14 tasks)

#### Task 3.01: Catalog Entities
- **Files**: `src/LzDetailingStore.Domain/Entities/Catalog/Product.cs`, `ProductVariant.cs`, `Brand.cs`, `Category.cs`, `ProductImage.cs`, `ProductSpec.cs`, `ProductApplicationStep.cs`
- **REQ**: REQ-020, REQ-021
- **Do**: Create entities per design. Product includes `RecalculatePrecioBase()` method. Include navigation properties.
- [ ] Complete

#### Task 3.02: Catalog EF Configurations
- **Files**: `src/LzDetailingStore.Infrastructure/Persistence/Configurations/Catalog/ProductConfiguration.cs`, `ProductVariantConfiguration.cs`, `BrandConfiguration.cs`, `CategoryConfiguration.cs`
- **REQ**: REQ-003
- **Do**: Max lengths, unique indexes (slug, sku), FK relationships, vector(384) column type for embedding.
- [ ] Complete

#### Task 3.03: Slug Generator Service
- **Files**: `src/LzDetailingStore.Application/Interfaces/ISlugGenerator.cs`, `src/LzDetailingStore.Infrastructure/Services/SlugGenerator.cs`
- **REQ**: REQ-027
- **Do**: Lowercase → normalize diacritics → replace spaces with hyphens → remove non-alphanumeric → check DB uniqueness → append -2, -3 suffix on collision.
- [ ] Complete

#### Task 3.04: Brands CRUD
- **Files**: `src/LzDetailingStore.Application/Features/Brands/Commands/CreateBrand/`, `UpdateBrand/`, `DeleteBrand/`, `Queries/GetBrands/`, `DTOs/`, `BrandMappingConfig.cs`
- **REQ**: REQ-023
- **Do**: CreateBrand (name, description, category, origin). UpdateBrand. DeleteBrand (soft delete). GetBrands (paginated, filterable by destacada/categoria). Mapster config.
- [ ] Complete

#### Task 3.05: Categories CRUD
- **Files**: `src/LzDetailingStore.Application/Features/Categories/Commands/CreateCategory/`, `UpdateCategory/`, `DeleteCategory/`, `Queries/GetCategories/`, `DTOs/`
- **REQ**: REQ-024
- **Do**: CRUD commands. GetCategories sorted by Orden. Soft delete.
- [ ] Complete

#### Task 3.06: Products CRUD (base)
- **Files**: `src/LzDetailingStore.Application/Features/Products/Commands/CreateProduct/`, `UpdateProduct/`, `DeleteProduct/`, `DTOs/ProductDto.cs`, `ProductListDto.cs`, `CreateProductRequest.cs`
- **REQ**: REQ-020
- **Do**: CreateProduct (name, description, brandId, categoryId, imageUrl). Auto-slug. Soft delete. DTOs for detail and list.
- [ ] Complete

#### Task 3.07: Product Variants CRUD
- **Files**: `src/LzDetailingStore.Application/Features/Products/Commands/CreateVariant/`, `UpdateVariant/`, `DeleteVariant/`, `DTOs/ProductVariantDto.cs`
- **REQ**: REQ-021, REQ-022
- **Do**: CreateVariant (sku manual, nombre, precioVenta, precioCosto, sizeValue, sizeUnit, stock). After create/update/delete → call Product.RecalculatePrecioBase(). PrecioCosto excluded from public DTOs.
- [ ] Complete

#### Task 3.08: Product Images
- **Files**: `src/LzDetailingStore.Application/Features/Products/Commands/UploadProductImage/`, `DeleteProductImage/`, `src/LzDetailingStore.Application/Interfaces/IS3Service.cs`, `src/LzDetailingStore.Infrastructure/Services/S3Service.cs`
- **REQ**: REQ-025
- **Do**: IS3Service: UploadAsync(stream, key) → url, DeleteAsync(key). UploadProductImage stores in S3 + creates ProductImage record. DeleteProductImage removes from both.
- [ ] Complete

#### Task 3.09: Product Specs and Application Steps
- **Files**: `src/LzDetailingStore.Application/Features/Products/Commands/ManageProductSpecs/`, `ManageProductSteps/`
- **REQ**: REQ-026
- **Do**: Bulk update specs (replace all for product). Bulk update steps (replace all for product). Ordered by Orden/StepNumber.
- [ ] Complete

#### Task 3.10: Product Filtering Extensions
- **Files**: `src/LzDetailingStore.Infrastructure/Persistence/Extensions/ProductQueryExtensions.cs`
- **REQ**: REQ-028
- **Do**: IQueryable<Product>.ApplyFilters(query) — filters by brandId, categoryId, minPrice, maxPrice, search (ILike on name+description). ApplySorting for price asc/desc, name, newest.
- [ ] Complete

#### Task 3.11: Product Queries
- **Files**: `src/LzDetailingStore.Application/Features/Products/Queries/GetProducts/GetProductsQuery.cs`, `GetProductsQueryHandler.cs`, `src/LzDetailingStore.Application/Features/Products/Queries/GetProductBySlug/`
- **REQ**: REQ-028, REQ-020
- **Do**: GetProducts: paginated + filtered + sorted. GetProductBySlug: includes variants (active only), images, specs, steps. PrecioCosto excluded for non-admin.
- [ ] Complete

#### Task 3.12: Catalog Mapster Configs
- **Files**: `src/LzDetailingStore.Application/Features/Products/ProductMappingConfig.cs`, `src/LzDetailingStore.Application/Features/Brands/BrandMappingConfig.cs`
- **REQ**: REQ-020
- **Do**: Product→ProductDto (include variants, brandName, categoryName). Product→ProductListDto (lighter). Brand→BrandDto. ProductVariant→ProductVariantDto (exclude PrecioCosto for public).
- [ ] Complete

#### Task 3.13: Public Catalog Endpoints
- **Files**: `src/LzDetailingStore.Api/Endpoints/Products/ProductEndpoints.cs`, `src/LzDetailingStore.Api/Endpoints/Brands/BrandEndpoints.cs`, `src/LzDetailingStore.Api/Endpoints/Categories/CategoryEndpoints.cs`
- **REQ**: REQ-020, REQ-023, REQ-024, REQ-028
- **Do**: GET /api/v1/productos (list), GET /api/v1/productos/{slug} (detail), GET /api/v1/marcas, GET /api/v1/categorias.
- [ ] Complete

#### Task 3.14: Admin Catalog Endpoints and DI
- **Files**: `src/LzDetailingStore.Api/Endpoints/Products/AdminProductEndpoints.cs`, `src/LzDetailingStore.Api/Endpoints/Brands/AdminBrandEndpoints.cs`, `src/LzDetailingStore.Api/Endpoints/Categories/AdminCategoryEndpoints.cs`
- **REQ**: REQ-020-028
- **Do**: CRUD admin endpoints for products (+ variants, images), brands, categories. RequireAuthorization with permission policies. Verify compilation.
- [ ] Complete

---

## Phase 4: Commerce (12 tasks)

#### Task 4.01: Commerce Entities
- **Files**: `src/LzDetailingStore.Domain/Entities/Commerce/Cart.cs`, `CartItem.cs`, `Order.cs`, `OrderItem.cs`, `ShippingAddress.cs`
- **REQ**: REQ-029, REQ-034
- **Do**: Create entities per design. OrderStatusMachine in `Domain/Services/`. Include navigation properties.
- [ ] Complete

#### Task 4.02: Commerce EF Configurations
- **Files**: `src/LzDetailingStore.Infrastructure/Persistence/Configurations/Commerce/CartConfiguration.cs`, `CartItemConfiguration.cs`, `OrderConfiguration.cs`, `OrderItemConfiguration.cs`, `ShippingAddressConfiguration.cs`
- **REQ**: REQ-003
- **Do**: Unique (UserId) on Cart. Unique (CartId, VariantId) on CartItem. FK relationships. Snapshot fields on OrderItem.
- [ ] Complete

#### Task 4.03: Shipping Address CRUD
- **Files**: `src/LzDetailingStore.Application/Features/Commerce/Commands/CreateAddress/`, `UpdateAddress/`, `DeleteAddress/`, `Queries/GetAddresses/`, DTOs
- **REQ**: REQ-036
- **Do**: CRUD scoped to authenticated user. EsPrincipal logic (only one default at a time). Endpoint: /api/v1/direcciones.
- [ ] Complete

#### Task 4.04: Add to Cart Command
- **Files**: `src/LzDetailingStore.Application/Features/Commerce/Commands/AddToCart/AddToCartCommand.cs`, `AddToCartCommandHandler.cs`, `AddToCartCommandValidator.cs`
- **REQ**: REQ-029, REQ-030
- **Do**: Input: VariantId, Quantity. Validates variant exists + is active + stock sufficient. Upsert logic: if (CartId, VariantId) exists → update qty, else add new item. Auto-creates cart if none exists.
- [ ] Complete

#### Task 4.05: Cart Operations (Update, Remove, Clear)
- **Files**: `src/LzDetailingStore.Application/Features/Commerce/Commands/UpdateCartItem/`, `RemoveCartItem/`, `ClearCart/`
- **REQ**: REQ-029
- **Do**: UpdateCartItem: validates stock. RemoveCartItem: by item ID. ClearCart: removes all items.
- [ ] Complete

#### Task 4.06: Cart Merge Command
- **Files**: `src/LzDetailingStore.Application/Features/Commerce/Commands/MergeCart/MergeCartCommand.cs`, `MergeCartCommandHandler.cs`
- **REQ**: REQ-031
- **Do**: Input: array of {VariantId, Quantity}. For each: if variant exists in server cart → keep server qty; if new → add. Skip invalid/inactive variants silently.
- [ ] Complete

#### Task 4.07: Cart Query
- **Files**: `src/LzDetailingStore.Application/Features/Commerce/Queries/GetCart/GetCartQuery.cs`, `GetCartQueryHandler.cs`, `DTOs/CartDto.cs`
- **REQ**: REQ-029
- **Do**: Returns cart with items including product name, image, variant name, unit price, quantity, subtotal. Calculates totals.
- [ ] Complete

#### Task 4.08: Mercado Pago Service
- **Files**: `src/LzDetailingStore.Application/Interfaces/IMercadoPagoService.cs`, `src/LzDetailingStore.Infrastructure/Services/MercadoPagoService.cs`
- **REQ**: REQ-032
- **Do**: CreatePreferenceAsync(order, items, backUrls) → preference init_point URL. ValidateWebhookSignature(headers, body). GetPaymentAsync(paymentId) → payment info.
- [ ] Complete

#### Task 4.09: Checkout Command
- **Files**: `src/LzDetailingStore.Application/Features/Commerce/Commands/CreateCheckout/CreateCheckoutCommand.cs`, `CreateCheckoutCommandHandler.cs`
- **REQ**: REQ-032, REQ-035
- **Do**: Validates cart not empty + has default address. Creates Order (PENDIENTE) + OrderItems (snapshots from current product/variant data). Calls MercadoPago CreatePreference. Returns preference URL.
- [ ] Complete

#### Task 4.10: Webhook Command
- **Files**: `src/LzDetailingStore.Application/Features/Commerce/Commands/ProcessWebhook/ProcessWebhookCommand.cs`, `ProcessWebhookCommandHandler.cs`
- **REQ**: REQ-033
- **Do**: Validates signature. Gets payment info. If approved → Order status to PAGADA + clear user's cart. Idempotent: if already PAGADA, return success.
- [ ] Complete

#### Task 4.11: Order Queries and Status Update
- **Files**: `src/LzDetailingStore.Application/Features/Commerce/Queries/GetOrders/`, `GetOrderById/`, `GetAllOrders/`, `Commands/UpdateOrderStatus/`, `AssignTracking/`
- **REQ**: REQ-034, REQ-037
- **Do**: User: GetOrders (own, paginated), GetOrderById (own). Admin: GetAllOrders (filtered). UpdateOrderStatus uses OrderStatusMachine.CanTransition. AssignTracking sets tracking number.
- [ ] Complete

#### Task 4.12: Commerce Endpoints and DI
- **Files**: `src/LzDetailingStore.Api/Endpoints/Commerce/CartEndpoints.cs`, `CheckoutEndpoints.cs`, `OrderEndpoints.cs`, `AdminOrderEndpoints.cs`, `ShippingAddressEndpoints.cs`, `WebhookEndpoints.cs`
- **REQ**: REQ-029-037
- **Do**: Map all commerce endpoints. Webhook endpoint: no JWT, signature validation. Verify compilation.
- [ ] Complete

---

## Phase 5: Content & Business (6 tasks)

#### Task 5.01: Content Entities and EF Configs
- **Files**: `src/LzDetailingStore.Domain/Entities/Content/Course.cs`, `CourseModule.cs`, `CourseTopic.cs`, `Branch.cs`, `src/LzDetailingStore.Infrastructure/Persistence/Configurations/Content/CourseConfiguration.cs`, `BranchConfiguration.cs`
- **REQ**: REQ-038, REQ-039
- **Do**: Create entities + Fluent API configs. Course has nested Modules and Topics.
- [ ] Complete

#### Task 5.02: Courses CRUD
- **Files**: `src/LzDetailingStore.Application/Features/Courses/Commands/CreateCourse/`, `UpdateCourse/`, `DeleteCourse/`, `Queries/GetCourses/`, `GetCourseById/`, DTOs
- **REQ**: REQ-038
- **Do**: CRUD with nested modules/topics. Public query filters Status=Active && IsActive=true. Soft delete.
- [ ] Complete

#### Task 5.03: Branches CRUD
- **Files**: `src/LzDetailingStore.Application/Features/Branches/Commands/CreateBranch/`, `UpdateBranch/`, `DeleteBranch/`, `Queries/GetBranches/`, DTOs
- **REQ**: REQ-039
- **Do**: CRUD. Public query filters IsActive=true. Includes GoogleMapsUrl.
- [ ] Complete

#### Task 5.04: Banxico Service Adapter
- **Files**: `src/LzDetailingStore.Application/Interfaces/IBanxicoService.cs`, `src/LzDetailingStore.Infrastructure/Services/BanxicoService.cs`, `src/LzDetailingStore.Domain/Entities/Business/ExchangeRate.cs`, `src/LzDetailingStore.Infrastructure/Persistence/Configurations/ExchangeRateConfiguration.cs`
- **REQ**: REQ-040
- **Do**: IBanxicoService: GetCurrentRateAsync() → Result<decimal>. Implementation calls Banxico API serie SF43718 with Bmx-Token header. Parses response. ExchangeRate entity + config.
- [ ] Complete

#### Task 5.05: Exchange Rate Job and Cache
- **Files**: `src/LzDetailingStore.Infrastructure/Jobs/ExchangeRateJob.cs`, `src/LzDetailingStore.Application/Features/ExchangeRate/Queries/GetExchangeRate/GetExchangeRateQuery.cs`, `GetExchangeRateQueryHandler.cs`
- **REQ**: REQ-040
- **Do**: Hangfire recurring job every 4h. Saves to DB + IMemoryCache (1h TTL). Query handler: check cache → miss → read DB. Fallback chain.
- [ ] Complete

#### Task 5.06: Content Endpoints and DI
- **Files**: `src/LzDetailingStore.Api/Endpoints/Content/CourseEndpoints.cs`, `AdminCourseEndpoints.cs`, `BranchEndpoints.cs`, `AdminBranchEndpoints.cs`, `ExchangeRate/ExchangeRateEndpoints.cs`
- **REQ**: REQ-038, REQ-039, REQ-040
- **Do**: Map all content endpoints. Register Hangfire + recurring job. Verify compilation.
- [ ] Complete

---

## Phase 6: Intelligence (5 tasks)

#### Task 6.01: ONNX Embedding Service
- **Files**: `src/LzDetailingStore.Application/Interfaces/IEmbeddingService.cs`, `src/LzDetailingStore.Infrastructure/AI/OnnxEmbeddingService.cs`, `src/LzDetailingStore.Infrastructure/AI/EmbeddingOptions.cs`
- **REQ**: REQ-041
- **Do**: IEmbeddingService: IsAvailable, GenerateEmbeddingAsync(text) → float[]?. OnnxEmbeddingService loads model at startup (singleton). EmbeddingOptions: ModelPath. Graceful failure if model missing.
- [ ] Complete

#### Task 6.02: Embedding Generation Logic
- **Files**: `src/LzDetailingStore.Infrastructure/Jobs/RegenerateEmbeddingJob.cs`
- **REQ**: REQ-042
- **Do**: Job receives productId. Loads product with brand, category, specs. Concatenates text. Calls IEmbeddingService. Stores vector in Product.Embedding column.
- [ ] Complete

#### Task 6.03: Domain Events for Embedding
- **Files**: `src/LzDetailingStore.Domain/Events/ProductCreatedEvent.cs`, `ProductUpdatedEvent.cs`, `src/LzDetailingStore.Application/Features/Products/Events/ProductCreatedEventHandler.cs`, `ProductUpdatedEventHandler.cs`
- **REQ**: REQ-042
- **Do**: Events fire from Product entity on create/update. Handlers enqueue Hangfire job for RegenerateEmbeddingJob.
- [ ] Complete

#### Task 6.04: Recommendation Query
- **Files**: `src/LzDetailingStore.Application/Features/Intelligence/Queries/GetRecommendations/GetRecommendationsQuery.cs`, `GetRecommendationsQueryHandler.cs`
- **REQ**: REQ-043, REQ-044
- **Do**: Input: productId. Gets product embedding. If null → return empty. Raw SQL via EF Core: pgvector cosine distance, top 10, WHERE is_active AND embedding IS NOT NULL. Random.Shared.Shuffle → take 3-6. Graceful degradation.
- [ ] Complete

#### Task 6.05: Intelligence Endpoints and DI
- **Files**: `src/LzDetailingStore.Api/Endpoints/Products/ProductEndpoints.cs` (add recommendation route), DI wiring for IEmbeddingService as singleton
- **REQ**: REQ-043
- **Do**: Add GET /api/v1/productos/{id}/recomendaciones endpoint. Register OnnxEmbeddingService as singleton. Register Hangfire job. Verify compilation.
- [ ] Complete

---

## Phase 7: Admin Operations (6 tasks)

#### Task 7.01: BulkImportLog Entity
- **Files**: `src/LzDetailingStore.Domain/Entities/Business/BulkImportLog.cs`, `src/LzDetailingStore.Infrastructure/Persistence/Configurations/BulkImportLogConfiguration.cs`
- **REQ**: REQ-045
- **Do**: Entity per database model design. EF config with indexes on UserId, Status, CreatedAt.
- [ ] Complete

#### Task 7.02: Excel Import Service
- **Files**: `src/LzDetailingStore.Application/Interfaces/IBulkImportService.cs`, `src/LzDetailingStore.Infrastructure/Services/ExcelBulkImportService.cs`
- **REQ**: REQ-045, REQ-046
- **Do**: Install ClosedXML. IBulkImportService: ProcessFileAsync(Stream, importType, importLogId). Implementation: reads rows, validates each (required fields, SKU uniqueness, FK refs), creates/updates entities. Records errors in List. Returns success/error counts.
- [ ] Complete

#### Task 7.03: Async Import Job
- **Files**: `src/LzDetailingStore.Infrastructure/Jobs/ProcessBulkImportJob.cs`
- **REQ**: REQ-047
- **Do**: Hangfire job receives importLogId. Downloads file from S3. Calls IBulkImportService.ProcessFileAsync. Updates BulkImportLog with counts and status.
- [ ] Complete

#### Task 7.04: Import Endpoints
- **Files**: `src/LzDetailingStore.Application/Features/Admin/BulkImport/Commands/ProcessBulkImport/ProcessBulkImportCommand.cs`, handler, `Queries/GetImportHistory/`, `Queries/GetImportErrors/`
- **REQ**: REQ-045, REQ-046, REQ-047
- **Do**: POST upload: validate file format, create log, if ≤100 rows → sync (200), if >100 → async Hangfire (202). GET history (paginated). GET errors by importId.
- [ ] Complete

#### Task 7.05: Excel Export and Template Services
- **Files**: `src/LzDetailingStore.Application/Interfaces/IBulkExportService.cs`, `src/LzDetailingStore.Infrastructure/Services/ExcelBulkExportService.cs`, `src/LzDetailingStore.Application/Features/Admin/BulkExport/Queries/ExportProducts/`
- **REQ**: REQ-048, REQ-049
- **Do**: IBulkExportService: ExportProductsAsync(filters), ExportBrandsAsync(), ExportCategoriesAsync(), GetTemplateAsync(type). Implementation with ClosedXML. Template includes headers + 1 example row.
- [ ] Complete

#### Task 7.06: Admin Import/Export Endpoints and DI
- **Files**: `src/LzDetailingStore.Api/Endpoints/Admin/ImportEndpoints.cs`, `src/LzDetailingStore.Api/Endpoints/Admin/ExportEndpoints.cs`
- **REQ**: REQ-045-049
- **Do**: POST /admin/importar/{tipo}, GET /admin/importar/plantilla/{tipo}, GET /admin/importar/historial, GET /admin/importar/{id}/errores, GET /admin/exportar/{tipo}. Wire DI. Verify compilation.
- [ ] Complete

---

## Phase 8: Observability & Hardening (8 tasks)

#### Task 8.01: OpenTelemetry Configuration
- **Files**: `src/LzDetailingStore.Api/Program.cs` (extend), NuGet packages
- **REQ**: REQ-050
- **Do**: Install OpenTelemetry packages. AddOpenTelemetry() with AspNetCore + EFCore + HttpClient instrumentation. OTLP exporter configurable. Correlation ID as span attribute.
- [ ] Complete

#### Task 8.02: Audit Service
- **Files**: `src/LzDetailingStore.Application/Interfaces/IAuditService.cs`, `src/LzDetailingStore.Infrastructure/Services/AuditService.cs`, `src/LzDetailingStore.Domain/Entities/Business/AuditLog.cs`, EF config
- **REQ**: REQ-051
- **Do**: IAuditService.LogAsync(AuditEntry). Implementation writes to AuditLogs table. AuditEntry: UserId, Action, EntityType, EntityId, OldValues, NewValues, IpAddress, UserAgent, CorrelationId. Integrate with SaveChanges or domain event handlers.
- [ ] Complete

#### Task 8.03: Rate Limiting
- **Files**: `src/LzDetailingStore.Api/Program.cs` (extend)
- **REQ**: REQ-052
- **Do**: AddRateLimiter with 3 fixed window policies: "public" (100/min by IP), "authenticated" (200/min by userId), "admin" (500/min by userId). OnRejected returns 429 JSON. Apply policies to endpoint groups.
- [ ] Complete

#### Task 8.04: Security Headers Middleware
- **Files**: `src/LzDetailingStore.Api/Middleware/SecurityHeadersMiddleware.cs`
- **REQ**: REQ-053
- **Do**: Adds X-Content-Type-Options: nosniff, X-Frame-Options: DENY, Strict-Transport-Security, X-XSS-Protection: 0, Referrer-Policy: strict-origin-when-cross-origin.
- [ ] Complete

#### Task 8.05: CORS Configuration
- **Files**: `src/LzDetailingStore.Api/Program.cs` (extend)
- **REQ**: REQ-054
- **Do**: AddCors with policy: allowed origins from config, methods (GET, POST, PUT, DELETE), headers (Authorization, Content-Type, X-Correlation-ID). Expose X-Correlation-ID + X-RateLimit-Remaining.
- [ ] Complete

#### Task 8.06: Domain Event Dispatch in SaveChanges
- **Files**: `src/LzDetailingStore.Infrastructure/Persistence/AppDbContext.cs` (extend)
- **REQ**: REQ-051
- **Do**: After base.SaveChangesAsync, collect domain events from tracked entities, dispatch via MediatR IPublisher, then clear events. Ensures events fire AFTER successful save.
- [ ] Complete

#### Task 8.07: Hangfire Dashboard
- **Files**: `src/LzDetailingStore.Api/Program.cs` (extend)
- **REQ**: REQ-047
- **Do**: Install Hangfire + Hangfire.PostgreSql. Configure dashboard at /hangfire (admin-only via authorization filter). Register all recurring jobs (exchange rate).
- [ ] Complete

#### Task 8.08: Final Program.cs Assembly
- **Files**: `src/LzDetailingStore.Api/Program.cs`
- **REQ**: All
- **Do**: Final review of middleware pipeline order: SecurityHeaders → Exception → Correlation → Auth → Authorization → RateLimit → CORS → Endpoints + HealthChecks + Scalar + Hangfire. Verify ALL services registered. `dotnet build` + `dotnet run` smoke test.
- [ ] Complete

---

## Review Workload Forecast

- **Total tasks**: 78
- **Estimated changed lines**: ~8,000-10,000
- **Estimated files**: ~150
- **Chained PRs recommended**: Yes (8 phases = natural PR boundaries)
- **400-line budget risk**: High per phase
- **Decision needed before apply**: Delivery strategy (chained PRs per phase)

---

## Dependencies

```
Phase 1 ──→ ALL (foundation required first)
Phase 2 ──→ Phase 4, Phase 7 (auth required for commerce and admin)
Phase 3 ──→ Phase 4, Phase 6, Phase 7 (catalog required for commerce, intelligence, admin ops)
Phase 4 ──→ Independent after Phase 2+3
Phase 5 ──→ Independent after Phase 1
Phase 6 ──→ Depends on Phase 3
Phase 7 ──→ Depends on Phase 2+3
Phase 8 ──→ Applies incrementally across all phases
```
