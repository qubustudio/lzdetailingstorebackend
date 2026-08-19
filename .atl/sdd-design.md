# SDD Design — LZ Detailing Store Backend

## Date: 2025-01-15
## Phase: Design
## Status: Draft

---

## 1. Solution Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         LzDetailingStore.Api                             │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────┐  ┌───────────────┐  │
│  │  Endpoints  │  │  Middleware   │  │  Filters   │  │  DI Wiring    │  │
│  │ (MapGroup)  │  │ (Pipeline)   │  │            │  │               │  │
│  └──────┬──────┘  └──────┬───────┘  └─────┬──────┘  └───────┬───────┘  │
└─────────┼────────────────┼─────────────────┼─────────────────┼──────────┘
          │                │                 │                 │
          ▼                ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    LzDetailingStore.Application                          │
│  ┌──────────────┐  ┌───────────────┐  ┌─────────────┐  ┌───────────┐  │
│  │  Commands    │  │   Queries     │  │  Behaviors  │  │   DTOs    │  │
│  │  + Handlers  │  │   + Handlers  │  │  (Pipeline) │  │ + Mapping │  │
│  └──────┬───────┘  └───────┬───────┘  └──────┬──────┘  └─────┬─────┘  │
│         │                  │                  │                │         │
│  ┌──────┴──────────────────┴──────────────────┴────────────────┘        │
│  │  Interfaces (Ports): IRepository<T>, IUnitOfWork, IS3Service,        │
│  │  IEmailService, IBanxicoService, IMercadoPagoService, IEmbedding...  │
│  └──────┬───────────────────────────────────────────────────────────    │
└─────────┼───────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      LzDetailingStore.Domain                             │
│  ┌───────────┐  ┌────────────────┐  ┌──────────────┐  ┌────────────┐  │
│  │ Entities  │  │ Value Objects  │  │ Domain Events│  │   Enums    │  │
│  └───────────┘  └────────────────┘  └──────────────┘  └────────────┘  │
│                        ZERO external dependencies                        │
└─────────────────────────────────────────────────────────────────────────┘
          ▲
          │ implements interfaces from Application
┌─────────┴───────────────────────────────────────────────────────────────┐
│                  LzDetailingStore.Infrastructure                         │
│  ┌────────────┐  ┌─────────────┐  ┌────────────┐  ┌─────────────────┐ │
│  │ AppDbCtx   │  │ Repositories│  │ EF Configs │  │ External Svcs   │ │
│  │ (EF Core)  │  │ (Generic)   │  │ (Fluent)   │  │ S3,MP,Banxico   │ │
│  └────────────┘  └─────────────┘  └────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
          ▲
          │ referenced by all layers
┌─────────┴───────────────────────────────────────────────────────────────┐
│                   LzDetailingStore.SharedKernel                          │
│  ┌────────────┐  ┌───────────┐  ┌──────────────┐  ┌─────────────────┐ │
│  │ BaseEntity │  │ Result<T> │  │ Interfaces   │  │ PagedResult<T>  │ │
│  │ Auditable  │  │ Error     │  │ IRepo, IUoW  │  │ Common Enums    │ │
│  └────────────┘  └───────────┘  └──────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

### Project References (csproj dependencies)

```
Api            → Application, Infrastructure
Application    → Domain, SharedKernel
Domain         → SharedKernel
Infrastructure → Application, Domain, SharedKernel
SharedKernel   → (none — leaf project)
```

---

## 2. SharedKernel Design

### BaseEntity

```csharp
namespace LzDetailingStore.SharedKernel;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### AuditableEntity

```csharp
namespace LzDetailingStore.SharedKernel;

public abstract class AuditableEntity : BaseEntity
{
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

### Result Pattern

```csharp
namespace LzDetailingStore.SharedKernel;

public sealed record Error(string Code, string Message);

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }
    public int? StatusCode { get; }

    protected Result(bool isSuccess, Error? error, int? statusCode = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error, int statusCode = 400) => new(false, error, statusCode);
    public static Result NotFound(string message) => Failure(new Error("NOT_FOUND", message), 404);
    public static Result Conflict(string message) => Failure(new Error("CONFLICT", message), 409);
    public static Result Forbidden() => Failure(new Error("FORBIDDEN", "Access denied"), 403);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) => Value = value;
    private Result(Error error, int statusCode) : base(false, error, statusCode) { }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error, int statusCode = 400) => new(error, statusCode);
    public static new Result<T> NotFound(string message) => new(new Error("NOT_FOUND", message), 404);
    public static new Result<T> Conflict(string message) => new(new Error("CONFLICT", message), 409);

    public static implicit operator Result<T>(T value) => Success(value);
}
```

### PagedResult

```csharp
namespace LzDetailingStore.SharedKernel;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Data,
    int Total,
    int Page,
    int TotalPages);
```

### Interfaces

```csharp
namespace LzDetailingStore.SharedKernel;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IDomainEvent : MediatR.INotification
{
    DateTime OccurredOn { get; }
}
```

### Common Enums

```csharp
namespace LzDetailingStore.SharedKernel;

public enum OrderStatus
{
    Pendiente,
    Pagada,
    Enviada,
    Entregada,
    Cancelada
}

public enum CourseLevel { Basico, Intermedio, Avanzado }
public enum CourseStatus { Draft, Active }
public enum BrandCategory { Ceramicos, Pulido, Lavado, Herramientas }
public enum ImportStatus { Pending, Processing, Completed, Failed }
```

---

## 3. Infrastructure Layer Design

### AppDbContext

```csharp
namespace LzDetailingStore.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    // DbSets registered via IEntityTypeConfiguration discovery

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(ct);
    }
}
```

**Configuration**: `UseNpgsql()` + `UseSnakeCaseNamingConvention()` + `UseVector()`.

### Generic Repository

```csharp
namespace LzDetailingStore.Infrastructure.Persistence.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    public void Update(T entity) => DbSet.Update(entity);
    public void Remove(T entity) => DbSet.Remove(entity);
}
```

### Entity Configuration Pattern

One `IEntityTypeConfiguration<T>` per entity in `Infrastructure/Persistence/Configurations/`:

```csharp
namespace LzDetailingStore.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.Embedding).HasColumnType("vector(384)");
        builder.HasMany(p => p.Variants).WithOne(v => v.Product).HasForeignKey(v => v.ProductId);
        builder.HasMany(p => p.Images).WithOne().HasForeignKey(i => i.ProductId);
        builder.HasMany(p => p.Specs).WithOne().HasForeignKey(s => s.ProductId);
        builder.HasMany(p => p.ApplicationSteps).WithOne().HasForeignKey(a => a.ProductId);
    }
}
```

### Migration Strategy

- Migrations stored in `Infrastructure/Persistence/Migrations/`
- Generated via `dotnet ef migrations add <Name> --project src/LzDetailingStore.Infrastructure --startup-project src/LzDetailingStore.Api`
- Applied on startup in Development; manual in Production
- Seed data applied via `IEntityTypeConfiguration.HasData()` for static data (Roles, Modules, Permissions)

### External Service Adapters

All adapters implement interfaces defined in `Application/Interfaces/`:

| Interface | Implementation | Responsibility |
|-----------|---------------|----------------|
| `IS3Service` | `S3Service` | Presigned URL generation, upload, delete |
| `IEmailService` | `NoOpEmailService` (MVP) | Sends password reset emails (no-op for now) |
| `IBanxicoService` | `BanxicoService` | Fetches USD/MXN rate from SIE API |
| `IMercadoPagoService` | `MercadoPagoService` | Creates preferences, validates webhooks |
| `IEmbeddingService` | `OnnxEmbeddingService` | Generates 384-dim embeddings from text |
| `IJwtService` | `JwtService` | Token generation and validation |
| `IPasswordHasher` | `BcryptPasswordHasher` | BCrypt hash and verify |
| `IAuditService` | `AuditService` | Writes audit log entries |
| `IBulkImportService` | `ExcelBulkImportService` | Parses and processes Excel imports |
| `IBulkExportService` | `ExcelBulkExportService` | Generates Excel exports |

---

## 4. Application Layer Design

### MediatR Pipeline Behaviors (execution order)

```
Request → LoggingBehavior → ValidationBehavior → TransactionBehavior → Handler
```

#### LoggingBehavior

```csharp
namespace LzDetailingStore.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} {@Request}", requestName, request);

        var response = await next();

        logger.LogInformation("Handled {RequestName}", requestName);
        return response;
    }
}
```

#### ValidationBehavior

```csharp
namespace LzDetailingStore.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count != 0)
            return (TResponse)(object)Result.Failure(
                new Error("VALIDATION_ERROR", string.Join("; ", failures.Select(f => f.ErrorMessage))), 400);

        return await next();
    }
}
```

#### TransactionBehavior

Wraps commands (not queries) in a database transaction. Applied only to `ICommand<T>` marker interface.

### Command/Query Naming Convention

| Pattern | Example |
|---------|---------|
| `{Verb}{Entity}Command` | `CreateProductCommand`, `UpdateOrderStatusCommand` |
| `Get{Entities}Query` | `GetProductsQuery`, `GetOrdersQuery` |
| `Get{Entity}By{Key}Query` | `GetProductBySlugQuery`, `GetOrderByIdQuery` |

### Handler Pattern

```csharp
namespace LzDetailingStore.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    string ImageUrl,
    string? ImageAlt) : IRequest<Result<ProductDto>>;

public sealed class CreateProductCommandHandler(
    IRepository<Product> productRepository,
    IRepository<Brand> brandRepository,
    IRepository<Category> categoryRepository,
    IUnitOfWork unitOfWork,
    ISlugGenerator slugGenerator) : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        // Validate references exist
        var brand = await brandRepository.GetByIdAsync(request.BrandId, ct);
        if (brand is null) return Result<ProductDto>.NotFound("Brand not found");

        var category = await categoryRepository.GetByIdAsync(request.CategoryId, ct);
        if (category is null) return Result<ProductDto>.NotFound("Category not found");

        // Generate unique slug
        var slug = await slugGenerator.GenerateAsync<Product>(request.Name, ct);

        var product = new Product { /* map fields */ };
        product.AddDomainEvent(new ProductCreatedEvent(product.Id));

        await productRepository.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return product.Adapt<ProductDto>();
    }
}
```

### Validation Pattern

One `AbstractValidator<TCommand>` per command:

```csharp
namespace LzDetailingStore.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description is not null);
    }
}
```

### DTO Pattern

| Type | Naming | Usage |
|------|--------|-------|
| Full detail | `{Entity}Dto` | Single-entity responses (GET by ID) |
| List item | `{Entity}ListDto` | Paginated list items (lighter) |
| Create request | `Create{Entity}Request` | Endpoint request body |
| Update request | `Update{Entity}Request` | Endpoint request body |

### Mapster Registration

One `IRegister` per feature module, registered at startup via assembly scanning:

```csharp
namespace LzDetailingStore.Application.Features.Products;

public sealed class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Product, ProductDto>()
            .Map(dest => dest.Variants, src => src.Variants.Where(v => v.IsActive));

        config.NewConfig<Product, ProductListDto>()
            .Map(dest => dest.BrandName, src => src.Brand.Nombre)
            .Map(dest => dest.CategoryName, src => src.Category.Nombre);
    }
}
```

---

## 5. API Layer Design

### Endpoint Group Interface

```csharp
namespace LzDetailingStore.Api.Endpoints;

public interface IEndpointGroup
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
```

### Auto-Discovery and Registration

```csharp
namespace LzDetailingStore.Api.Extensions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointGroups = typeof(IEndpointGroup).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IEndpointGroup)))
            .Select(Activator.CreateInstance)
            .Cast<IEndpointGroup>();

        foreach (var group in endpointGroups)
            group.MapEndpoints(app);

        return app;
    }
}
```

### Endpoint Group Example

```csharp
namespace LzDetailingStore.Api.Endpoints.Products;

public sealed class ProductEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/productos").WithTags("Products");

        group.MapGet("/", GetProducts);
        group.MapGet("/{slug}", GetProductBySlug);
        group.MapGet("/{id:guid}/recomendaciones", GetRecommendations);

        var admin = app.MapGroup("/api/v1/admin/productos")
            .WithTags("Admin - Products")
            .RequireAuthorization("productos.ver");

        admin.MapPost("/", CreateProduct).RequireAuthorization("productos.crear");
        admin.MapPut("/{id:guid}", UpdateProduct).RequireAuthorization("productos.editar");
        admin.MapDelete("/{id:guid}", DeleteProduct).RequireAuthorization("productos.eliminar");
    }

    private static async Task<IResult> GetProducts(
        [AsParameters] GetProductsQueryParams queryParams,
        ISender sender, CancellationToken ct)
    {
        var query = queryParams.Adapt<GetProductsQuery>();
        var result = await sender.Send(query, ct);
        return result.ToApiResponse();
    }
    // ...
}
```

### Middleware Pipeline Order (Program.cs)

```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapApiEndpoints();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready");
```

### Result-to-HTTP Extension

```csharp
namespace LzDetailingStore.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToApiResponse(this Result result) => result switch
    {
        { IsSuccess: true } => Results.Ok(),
        { StatusCode: 404 } => Results.NotFound(ToErrorBody(result)),
        { StatusCode: 409 } => Results.Conflict(ToErrorBody(result)),
        { StatusCode: 422 } => Results.UnprocessableEntity(ToErrorBody(result)),
        { StatusCode: 401 } => Results.Unauthorized(),
        { StatusCode: 403 } => Results.Json(ToErrorBody(result), statusCode: 403),
        _ => Results.BadRequest(ToErrorBody(result))
    };

    public static IResult ToApiResponse<T>(this Result<T> result) => result switch
    {
        { IsSuccess: true } => Results.Ok(result.Value),
        _ => ((Result)result).ToApiResponse()
    };

    private static object ToErrorBody(Result result) => new
    {
        error = result.Error!.Code,
        message = result.Error.Message,
        statusCode = result.StatusCode ?? 400
    };
}
```

---

## 6. Feature Module Designs

### 6.1 Identity Module

**Entities** (`Domain/Entities/Identity/`):
- `User` : AuditableEntity — Nombre, Email, PasswordHash?, Telefono, IsActive, EmailVerified
- `Role` : BaseEntity — Nombre, Descripcion, IsSystem
- `Permission` : BaseEntity — ModuleId, Nombre, Slug, Descripcion
- `Module` : BaseEntity — Nombre, Slug, Descripcion, Orden
- `OAuthAccount` : BaseEntity — UserId, Provider, ProviderUserId, Email
- `RefreshToken` : BaseEntity — UserId, Token, ExpiresAt, RevokedAt, ReplacedByToken
- `UserRole` — UserId, RoleId, AssignedAt, AssignedBy
- `RolePermission` — RoleId, PermissionId

**Commands**:

| Command | Returns | Auth |
|---------|---------|------|
| `RegisterUserCommand` | `Result<AuthResponseDto>` | Anonymous |
| `LoginCommand` | `Result<AuthResponseDto>` | Anonymous |
| `RefreshTokenCommand` | `Result<AuthResponseDto>` | Anonymous |
| `LoginOAuthCommand` | `Result<AuthResponseDto>` | Anonymous |
| `RecoverPasswordCommand` | `Result` | Anonymous |
| `ResetPasswordCommand` | `Result` | Anonymous |
| `UpdateProfileCommand` | `Result<ProfileDto>` | Authenticated |

**Queries**:

| Query | Returns | Auth |
|-------|---------|------|
| `GetProfileQuery` | `Result<ProfileDto>` | Authenticated |
| `GetUsersQuery` | `Result<PagedResult<UserListDto>>` | `usuarios.ver` |
| `GetRolesQuery` | `Result<List<RoleDto>>` | `roles.ver` |

**Services** (`Application/Interfaces/`):
- `IJwtService.GenerateTokenAsync(User user, IList<string> roles)` → `(string accessToken, int expiresIn)`
- `IPasswordHasher.Hash(string password)` → `string`
- `IPasswordHasher.Verify(string password, string hash)` → `bool`
- `IOAuthValidator.ValidateTokenAsync(string provider, string token)` → `Result<OAuthUserInfo>`

**Authorization Handler**:

```csharp
namespace LzDetailingStore.Infrastructure.Auth;

public sealed class PermissionAuthorizationHandler(IServiceScopeFactory scopeFactory)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hasPermission = await dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == Guid.Parse(userId))
            .SelectMany(ur => ur.Role.RolePermissions)
            .AnyAsync(rp => rp.Permission.Slug == requirement.Permission);

        if (hasPermission) context.Succeed(requirement);
    }
}
```

**JWT Claims**: `{ sub: userId, email, roles: ["Admin", "SuperAdmin"], exp, iat, jti }`

---

### 6.2 Catalog Module

**Entities** (`Domain/Entities/Catalog/`):
- `Product` : AuditableEntity — BrandId, CategoryId, Sku, Name, Slug, Description, PrecioBase?, PrecioOriginal?, ImageUrl, ImageAlt, Rating, ReviewCount, Badge, IsActive, Embedding
- `ProductVariant` : BaseEntity — ProductId, Sku, Nombre, PrecioVenta, PrecioCosto, SizeValue, SizeUnit, Stock, Barcode, IsActive, SortOrder
- `Brand` : AuditableEntity — Nombre, Slug, LogoUrl, Descripcion, Categoria, Badge, Origen, Especialidad, Destacada, HistoriaCompleta, ImagenSpotlight, IsActive
- `Category` : AuditableEntity — Nombre, Slug, Descripcion, IsActive, Orden
- `ProductImage` : BaseEntity — ProductId, ImageUrl, ImageAlt, Orden
- `ProductSpec` : BaseEntity — ProductId, Label, Value, Orden
- `ProductApplicationStep` : BaseEntity — ProductId, StepNumber, Title, Description

**Commands**:

| Command | Key Logic |
|---------|-----------|
| `CreateProductCommand` | Generates slug, sets PrecioBase = null (no variants yet) |
| `UpdateProductCommand` | Updates fields, regenerates slug if name changed |
| `DeleteProductCommand` | Soft delete (IsActive = false) |
| `CreateVariantCommand` | Creates variant, triggers `Product.RecalculatePrecioBase()` |
| `UpdateVariantCommand` | Updates variant, triggers PrecioBase recalculation |
| `DeleteVariantCommand` | Soft delete variant, triggers PrecioBase recalculation |
| `CreateBrandCommand` | Generates slug, validates uniqueness |
| `UpdateBrandCommand` | Updates brand, regenerates slug if name changed |
| `DeleteBrandCommand` | Soft delete (has products check) |
| `CreateCategoryCommand` | Generates slug |
| `UpdateCategoryCommand` | Updates category |
| `UploadProductImageCommand` | Calls IS3Service, stores URL in ProductImages |
| `DeleteProductImageCommand` | Removes from DB + S3 |

**Queries**:

| Query | Pagination | Filters |
|-------|-----------|---------|
| `GetProductsQuery` | Yes | brandId, categoryId, minPrice, maxPrice, search, sortBy |
| `GetProductBySlugQuery` | No | Includes variants, images, specs, steps |
| `GetBrandsQuery` | Yes | destacada, categoria, search |
| `GetCategoriesQuery` | No | Sorted by Orden |

**Domain Logic — PrecioBase Recalculation**:

```csharp
namespace LzDetailingStore.Domain.Entities.Catalog;

public partial class Product
{
    public void RecalculatePrecioBase()
    {
        var activeVariants = Variants.Where(v => v.IsActive).ToList();
        PrecioBase = activeVariants.Count != 0
            ? activeVariants.Min(v => v.PrecioVenta)
            : null;
    }
}
```

**SlugGenerator Service**:

```csharp
namespace LzDetailingStore.Application.Services;

public interface ISlugGenerator
{
    Task<string> GenerateAsync<T>(string input, CancellationToken ct = default) where T : BaseEntity;
}
```

Logic: lowercase → normalize diacritics → replace spaces with hyphens → remove non-alphanumeric → check uniqueness → append `-2`, `-3` suffix on collision.

**Filtering via IQueryable Extensions**:

```csharp
namespace LzDetailingStore.Infrastructure.Persistence.Extensions;

public static class ProductQueryExtensions
{
    public static IQueryable<Product> ApplyFilters(this IQueryable<Product> query, GetProductsQuery filters)
    {
        if (filters.BrandId.HasValue)
            query = query.Where(p => p.BrandId == filters.BrandId);
        if (filters.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filters.CategoryId);
        if (filters.MinPrice.HasValue)
            query = query.Where(p => p.PrecioBase >= filters.MinPrice);
        if (filters.MaxPrice.HasValue)
            query = query.Where(p => p.PrecioBase <= filters.MaxPrice);
        if (!string.IsNullOrWhiteSpace(filters.Search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{filters.Search}%")
                                  || EF.Functions.ILike(p.Description ?? "", $"%{filters.Search}%"));
        return query;
    }
}
```

---

### 6.3 Commerce Module

**Entities** (`Domain/Entities/Commerce/`):
- `Cart` : BaseEntity — UserId (unique)
- `CartItem` : BaseEntity — CartId, ProductId, VariantId, Quantity, UnitPrice
- `Order` : AuditableEntity — UserId, Subtotal, Impuestos, Envio, Total, Estado, MetodoPago, Tracking, Currency, ExchangeRate, MercadoPagoPreferenceId, MercadoPagoPaymentId
- `OrderItem` : BaseEntity — OrderId, ProductId, VariantId?, ProductName, ProductImage, VariantName, Sku, UnitPrice, Quantity, Subtotal
- `ShippingAddress` : BaseEntity — UserId, Alias, Calle, NumExterior, NumInterior, Colonia, Ciudad, Estado, CodigoPostal, Referencia, EsPrincipal

**Commands**:

| Command | Key Logic |
|---------|-----------|
| `AddToCartCommand` | Upsert: if (CartId, VariantId) exists → update qty; else add. Stock validation. |
| `UpdateCartItemCommand` | Validates stock, updates quantity |
| `RemoveCartItemCommand` | Removes item by ID |
| `ClearCartCommand` | Removes all items from user's cart |
| `MergeCartCommand` | Accepts array of {variantId, quantity}, merges with server cart |
| `CreateCheckoutCommand` | Creates Order (PENDIENTE) + OrderItems (snapshots) + calls MercadoPago |
| `ProcessWebhookCommand` | Validates signature, updates order to PAGADA, clears cart |
| `UpdateOrderStatusCommand` | Validates transition via OrderStatusMachine, updates |
| `AssignTrackingCommand` | Sets tracking number on order |

**OrderStatusMachine**:

```csharp
namespace LzDetailingStore.Domain.Services;

public static class OrderStatusMachine
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ValidTransitions = new()
    {
        [OrderStatus.Pendiente] = [OrderStatus.Pagada, OrderStatus.Cancelada],
        [OrderStatus.Pagada] = [OrderStatus.Enviada, OrderStatus.Cancelada],
        [OrderStatus.Enviada] = [OrderStatus.Entregada],
        [OrderStatus.Entregada] = [],
        [OrderStatus.Cancelada] = []
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
```

**Cart Duplicate Add Logic** (in handler):
```csharp
var existingItem = await dbContext.Set<CartItem>()
    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.VariantId == request.VariantId, ct);

if (existingItem is not null)
{
    existingItem.Quantity += request.Quantity;
    // Re-validate stock
}
else
{
    cart.Items.Add(new CartItem { ... });
}
```

**MercadoPago Integration**:

```csharp
namespace LzDetailingStore.Application.Interfaces;

public interface IMercadoPagoService
{
    Task<Result<string>> CreatePreferenceAsync(Order order, CancellationToken ct = default);
    bool ValidateWebhookSignature(string xSignature, string xRequestId, string dataId);
    Task<Result<PaymentInfo>> GetPaymentAsync(string paymentId, CancellationToken ct = default);
}
```

- `CreatePreferenceAsync` → builds preference with items, back_urls, notification_url → returns `init_point` URL.
- Webhook: validates `x-signature` header via HMAC-SHA256 with secret, looks up order by `MercadoPagoPaymentId`, transitions to PAGADA.

---

### 6.4 Content Module

**Entities** (`Domain/Entities/Content/`):
- `Course` : AuditableEntity — Title, Description, Level, Duration, ImageUrl, ImageAlt, Status, IsActive
- `CourseModule` : BaseEntity — CourseId, Number, Title, Description, Orden
- `CourseTopic` : BaseEntity — CourseId, Nombre, Orden
- `Branch` : AuditableEntity — Nombre, Descripcion, Direccion, Telefono, Email, GoogleMapsUrl, ImagenUrl, ImagenAlt, IsActive

**Commands**: Standard CRUD for Course (with nested Modules/Topics) and Branch.

**Queries**:
- `GetCoursesQuery` → public: filters `Status == Active && IsActive == true`, returns list with modules/topics
- `GetCourseByIdQuery` → full detail
- `GetBranchesQuery` → public: filters `IsActive == true`

---

### 6.5 Exchange Rate Module

**Background Service** (`Infrastructure/Jobs/`):

```csharp
namespace LzDetailingStore.Infrastructure.Jobs;

public sealed class ExchangeRateJob(
    IBanxicoService banxicoService,
    AppDbContext dbContext,
    IMemoryCache cache,
    ILogger<ExchangeRateJob> logger)
{
    private const string CacheKey = "exchange-rate-usd-mxn";

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        var result = await banxicoService.GetCurrentRateAsync();

        if (result.IsSuccess)
        {
            var entry = new ExchangeRate
            {
                FromCurrency = "USD",
                ToCurrency = "MXN",
                Rate = result.Value,
                Source = "banxico",
                FetchedAt = DateTime.UtcNow
            };

            await dbContext.Set<ExchangeRate>().AddAsync(entry);
            await dbContext.SaveChangesAsync();
            cache.Set(CacheKey, result.Value, TimeSpan.FromHours(1));
        }
        else
        {
            logger.LogWarning("Banxico API failed: {Error}", result.Error?.Message);
        }
    }
}
```

**Registration**: `RecurringJob.AddOrUpdate<ExchangeRateJob>("exchange-rate", j => j.ExecuteAsync(), "0 */4 * * *");`

**Endpoint Handler**:
1. Check `IMemoryCache` for `"exchange-rate-usd-mxn"`
2. If miss → query `ExchangeRates` table ORDER BY `FetchedAt DESC LIMIT 1`
3. Return rate + fetchedAt timestamp

---

### 6.6 Intelligence Module

**Service Interface**:

```csharp
namespace LzDetailingStore.Application.Interfaces;

public interface IEmbeddingService
{
    bool IsAvailable { get; }
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}
```

**ONNX Implementation**:

```csharp
namespace LzDetailingStore.Infrastructure.AI;

public sealed class OnnxEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly InferenceSession? _session;
    public bool IsAvailable => _session is not null;

    public OnnxEmbeddingService(IOptions<EmbeddingOptions> options, ILogger<OnnxEmbeddingService> logger)
    {
        try
        {
            _session = new InferenceSession(options.Value.ModelPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load ONNX model at {Path}", options.Value.ModelPath);
        }
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (!IsAvailable) return null;
        // Tokenize input, run inference, mean-pool, return float[384]
        return await Task.Run(() => RunInference(text), ct);
    }
}
```

**Registered as Singleton** — model loaded once at startup (~90MB RAM).

**Embedding Input Construction**:
```
{Product.Name} {Brand.Nombre} {Category.Nombre} {Product.Description} {Spec1.Label}: {Spec1.Value} ...
```

**Background Job** (triggered by `ProductCreatedEvent` / `ProductUpdatedEvent`):

```csharp
public sealed class RegenerateEmbeddingJob(
    AppDbContext dbContext,
    IEmbeddingService embeddingService)
{
    public async Task ExecuteAsync(Guid productId)
    {
        var product = await dbContext.Set<Product>()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specs)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null) return;

        var text = BuildEmbeddingText(product);
        var embedding = await embeddingService.GenerateEmbeddingAsync(text);

        if (embedding is not null)
        {
            product.Embedding = new Pgvector.Vector(embedding);
            await dbContext.SaveChangesAsync();
        }
    }
}
```

**Recommendation Query** (raw SQL via EF Core):

```sql
SELECT p.id, p.name, p.slug, p.image_url, p.precio_base,
       1 - (p.embedding <=> @targetEmbedding) AS similarity
FROM products p
WHERE p.id != @productId
  AND p.is_active = true
  AND p.embedding IS NOT NULL
ORDER BY p.embedding <=> @targetEmbedding
LIMIT 10;
```

Then: `Random.Shared.Shuffle(top10)` → take 3–6 items.

**Graceful Degradation**: If `IsAvailable == false` or product embedding is null → return empty `List<ProductListDto>` with HTTP 200.

---

### 6.7 Admin Operations Module (Bulk Import/Export)

**Import Flow**:

```
Upload(.xlsx) → Validate file format → Create BulkImportLog(pending)
                                            │
                    ┌───────────────────────┼────────────────────────┐
                    │ ≤100 rows             │ >100 rows              │
                    │ Process sync          │ Enqueue Hangfire job   │
                    │ Return 200            │ Return 202 + importId  │
                    └───────────┬───────────┴────────────┬───────────┘
                                │                        │
                                ▼                        ▼
                    Process rows → validate each → create/update entities
                                │
                                ▼
                    Update BulkImportLog (completed/failed)
```

**Row Processor Pattern**:

```csharp
namespace LzDetailingStore.Application.Interfaces;

public interface IRowProcessor<TRow>
{
    Task<RowResult> ProcessAsync(TRow row, int rowNumber, CancellationToken ct);
}

public sealed record RowResult(bool Success, string? Error = null);
```

Each row: validate required fields → check FK references (auto-create brands/categories if missing) → create or update entity → record errors in `List<ImportError>` → persist to `ErrorsDetail` jsonb.

**Export**:

```csharp
namespace LzDetailingStore.Application.Interfaces;

public interface IBulkExportService
{
    Task<byte[]> ExportProductsAsync(ExportFilters? filters, CancellationToken ct = default);
    Task<byte[]> ExportBrandsAsync(CancellationToken ct = default);
    Task<byte[]> ExportCategoriesAsync(CancellationToken ct = default);
}
```

Returns `byte[]` → endpoint wraps in `Results.File(bytes, contentType, fileName)`.

**Template**: Pre-built ClosedXML workbook with headers + 1 example row per entity type. Stored as embedded resource or generated on-the-fly.

---

### 6.8 Observability Module

**CorrelationIdMiddleware**:

```csharp
namespace LzDetailingStore.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[Header].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[Header] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
```

**GlobalExceptionMiddleware**:

```csharp
namespace LzDetailingStore.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString();
            logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "INTERNAL_ERROR",
                message = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? ex.Message : "An unexpected error occurred",
                statusCode = 500
            });
        }
    }
}
```

**Serilog Configuration** (Program.cs):
- Enrichers: `CorrelationId`, `MachineName`, `ThreadId`
- Sinks: Console (always), Seq (Development/Staging)
- UserId enrichment via custom middleware after auth

**OpenTelemetry Setup**:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

**Audit Service** (called from `SaveChangesAsync` override or domain event handler):

```csharp
namespace LzDetailingStore.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);
}

public sealed record AuditEntry(
    Guid? UserId,
    string Action,
    string EntityType,
    string? EntityId,
    object? OldValues,
    object? NewValues,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId);
```

**Rate Limiting** (Program.cs):

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("authenticated", opt =>
    {
        opt.PermitLimit = 200;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("admin", opt =>
    {
        opt.PermitLimit = 500;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "RATE_LIMITED",
            message = "Too many requests",
            statusCode = 429
        }, ct);
    };
});
```

**Health Checks**:
- Liveness: `app.MapHealthChecks("/health/live", new() { Predicate = _ => false })` — always healthy if process runs
- Readiness: `app.MapHealthChecks("/health/ready")` with `AddNpgSql(connectionString)`

---

## 7. Domain Events Pattern

**Event Dispatch Strategy**: Events are dispatched AFTER `SaveChangesAsync` succeeds (not inside transaction) to avoid side effects on rollback.

**Implementation in AppDbContext**:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    // Timestamps logic...
    var domainEvents = ChangeTracker.Entries<BaseEntity>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

    var result = await base.SaveChangesAsync(ct);

    // Dispatch after successful save
    foreach (var domainEvent in domainEvents)
        await _publisher.Publish(domainEvent, ct);

    // Clear events from entities
    ChangeTracker.Entries<BaseEntity>()
        .ToList()
        .ForEach(e => e.Entity.ClearDomainEvents());

    return result;
}
```

**Key Domain Events**:

| Event | Triggers |
|-------|----------|
| `ProductCreatedEvent(Guid ProductId)` | Embedding generation job |
| `ProductUpdatedEvent(Guid ProductId)` | Embedding regeneration job |
| `OrderPaidEvent(Guid OrderId)` | Cart clearing, audit log |
| `OrderStatusChangedEvent(Guid OrderId, OrderStatus From, OrderStatus To)` | Audit log |

**Event Handlers** (MediatR `INotificationHandler<T>`):

```csharp
namespace LzDetailingStore.Application.Features.Products.Events;

public sealed class ProductCreatedEventHandler(IBackgroundJobClient jobClient)
    : INotificationHandler<ProductCreatedEvent>
{
    public Task Handle(ProductCreatedEvent notification, CancellationToken ct)
    {
        jobClient.Enqueue<RegenerateEmbeddingJob>(j => j.ExecuteAsync(notification.ProductId));
        return Task.CompletedTask;
    }
}
```

---

## 8. Error Handling Strategy

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                              │
│  GlobalExceptionMiddleware catches unhandled → 500           │
│  ResultExtensions.ToApiResponse() maps Result → HTTP status  │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                    Application Layer                          │
│  ValidationBehavior returns Result with VALIDATION_ERROR     │
│  Handlers return Result<T> with domain-specific errors       │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                      Domain Layer                             │
│  Entities return Result from business methods                │
│  OrderStatusMachine.CanTransition returns bool               │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                  Infrastructure Layer                         │
│  Adapters catch external exceptions → wrap in Result         │
│  Repository catches DbUpdateException → Result.Conflict      │
└─────────────────────────────────────────────────────────────┘
```

**Error Code → HTTP Status Mapping**:

| Error Scenario | Error Code | HTTP Status |
|----------------|-----------|-------------|
| Entity not found | `NOT_FOUND` | 404 |
| Validation failure | `VALIDATION_ERROR` | 400 |
| Business rule violation | `DOMAIN_ERROR` / specific | 422 |
| Duplicate/conflict | `CONFLICT`, `SKU_ALREADY_EXISTS`, `EMAIL_ALREADY_EXISTS` | 409 |
| Unauthorized | `UNAUTHORIZED` | 401 |
| Forbidden | `FORBIDDEN` | 403 |
| Rate limited | `RATE_LIMITED` | 429 |
| Unhandled exception | `INTERNAL_ERROR` | 500 |

**Response Shape** (always consistent):

```json
{
  "error": "VALIDATION_ERROR",
  "message": "One or more validation errors occurred",
  "statusCode": 400,
  "errors": {
    "name": ["Name must not exceed 200 characters"],
    "brandId": ["BrandId is required"]
  }
}
```

---

## 9. Security Design

### JWT Structure

```json
{
  "sub": "a1b2c3d4-...",
  "email": "admin@lzdetailing.com",
  "roles": ["Admin"],
  "exp": 1705420800,
  "iat": 1705334400,
  "jti": "unique-token-id"
}
```

- Signing: HMAC-SHA256 with secret from configuration
- Expiration: 24 hours
- Issued from `IJwtService` in Infrastructure

### Password Security

- Algorithm: BCrypt
- Cost factor: 12 (via `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)`)
- Verification: timing-safe comparison built into BCrypt library

### Refresh Token

- Generation: `RandomNumberGenerator.GetBytes(32)` → Base64 (256-bit random)
- Storage: SHA256 hash of token stored in DB (raw token returned to client only once)
- Rotation: on each use, old token revoked, new token issued
- Family invalidation: if a revoked token is reused → revoke ALL tokens for that user

### OAuth Validation

Backend validates token directly with provider endpoint:
- Google: `https://oauth2.googleapis.com/tokeninfo?id_token={token}`
- Apple: Decode JWT + validate signature with Apple's public keys
- Microsoft: `https://graph.microsoft.com/v1.0/me` with bearer token

Never trusts client-side claims (email, name) without server-side validation.

### Permission Check Flow

```
Request → JWT Middleware (validates token, extracts claims)
        → Authorization Middleware
            → PermissionAuthorizationHandler
                → Query DB: UserRoles → RolePermissions → Permissions
                → Check if user has required permission slug
                → No caching (real-time role changes reflected immediately)
```

### Rate Limiting Strategy

| Tier | Identification | Limit |
|------|---------------|-------|
| Public (anonymous) | IP address | 100 req/min |
| Authenticated | UserId from JWT | 200 req/min |
| Admin | UserId from JWT | 500 req/min |
| Webhook | IP whitelist | Unlimited |

---

## 10. File/Folder Structure

### src/LzDetailingStore.SharedKernel/

```
SharedKernel/
├── BaseEntity.cs
├── AuditableEntity.cs
├── Result.cs
├── Error.cs
├── PagedResult.cs
├── IDomainEvent.cs
├── Interfaces/
│   ├── IRepository.cs
│   └── IUnitOfWork.cs
└── Enums/
    ├── OrderStatus.cs
    ├── CourseLevel.cs
    ├── CourseStatus.cs
    ├── BrandCategory.cs
    └── ImportStatus.cs
```

### src/LzDetailingStore.Domain/

```
Domain/
├── Entities/
│   ├── Identity/
│   │   ├── User.cs
│   │   ├── Role.cs
│   │   ├── Permission.cs
│   │   ├── Module.cs
│   │   ├── OAuthAccount.cs
│   │   ├── RefreshToken.cs
│   │   ├── UserRole.cs
│   │   └── RolePermission.cs
│   ├── Catalog/
│   │   ├── Product.cs
│   │   ├── ProductVariant.cs
│   │   ├── Brand.cs
│   │   ├── Category.cs
│   │   ├── ProductImage.cs
│   │   ├── ProductSpec.cs
│   │   └── ProductApplicationStep.cs
│   ├── Commerce/
│   │   ├── Cart.cs
│   │   ├── CartItem.cs
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   └── ShippingAddress.cs
│   ├── Content/
│   │   ├── Course.cs
│   │   ├── CourseModule.cs
│   │   ├── CourseTopic.cs
│   │   └── Branch.cs
│   └── Business/
│       └── ExchangeRate.cs
├── Events/
│   ├── ProductCreatedEvent.cs
│   ├── ProductUpdatedEvent.cs
│   ├── OrderPaidEvent.cs
│   └── OrderStatusChangedEvent.cs
└── Services/
    └── OrderStatusMachine.cs
```

### src/LzDetailingStore.Application/

```
Application/
├── DependencyInjection.cs
├── Interfaces/
│   ├── IJwtService.cs
│   ├── IPasswordHasher.cs
│   ├── IOAuthValidator.cs
│   ├── IS3Service.cs
│   ├── IEmailService.cs
│   ├── IBanxicoService.cs
│   ├── IMercadoPagoService.cs
│   ├── IEmbeddingService.cs
│   ├── IAuditService.cs
│   ├── ISlugGenerator.cs
│   ├── IBulkImportService.cs
│   └── IBulkExportService.cs
├── Behaviors/
│   ├── LoggingBehavior.cs
│   ├── ValidationBehavior.cs
│   └── TransactionBehavior.cs
├── Features/
│   ├── Identity/
│   │   ├── Commands/
│   │   │   ├── RegisterUser/
│   │   │   │   ├── RegisterUserCommand.cs
│   │   │   │   ├── RegisterUserCommandHandler.cs
│   │   │   │   └── RegisterUserCommandValidator.cs
│   │   │   ├── Login/
│   │   │   │   ├── LoginCommand.cs
│   │   │   │   ├── LoginCommandHandler.cs
│   │   │   │   └── LoginCommandValidator.cs
│   │   │   ├── RefreshToken/
│   │   │   ├── LoginOAuth/
│   │   │   ├── RecoverPassword/
│   │   │   ├── ResetPassword/
│   │   │   └── UpdateProfile/
│   │   ├── Queries/
│   │   │   ├── GetProfile/
│   │   │   ├── GetUsers/
│   │   │   └── GetRoles/
│   │   ├── DTOs/
│   │   │   ├── AuthResponseDto.cs
│   │   │   ├── ProfileDto.cs
│   │   │   ├── UserListDto.cs
│   │   │   └── RoleDto.cs
│   │   └── IdentityMappingConfig.cs
│   ├── Products/
│   │   ├── Commands/
│   │   │   ├── CreateProduct/
│   │   │   ├── UpdateProduct/
│   │   │   ├── DeleteProduct/
│   │   │   ├── CreateVariant/
│   │   │   ├── UpdateVariant/
│   │   │   ├── DeleteVariant/
│   │   │   ├── UploadProductImage/
│   │   │   └── DeleteProductImage/
│   │   ├── Queries/
│   │   │   ├── GetProducts/
│   │   │   └── GetProductBySlug/
│   │   ├── Events/
│   │   │   ├── ProductCreatedEventHandler.cs
│   │   │   └── ProductUpdatedEventHandler.cs
│   │   ├── DTOs/
│   │   │   ├── ProductDto.cs
│   │   │   ├── ProductListDto.cs
│   │   │   ├── ProductVariantDto.cs
│   │   │   └── CreateProductRequest.cs
│   │   └── ProductMappingConfig.cs
│   ├── Brands/
│   │   ├── Commands/
│   │   │   ├── CreateBrand/
│   │   │   ├── UpdateBrand/
│   │   │   └── DeleteBrand/
│   │   ├── Queries/
│   │   │   └── GetBrands/
│   │   ├── DTOs/
│   │   └── BrandMappingConfig.cs
│   ├── Categories/
│   │   ├── Commands/
│   │   │   ├── CreateCategory/
│   │   │   ├── UpdateCategory/
│   │   │   └── DeleteCategory/
│   │   ├── Queries/
│   │   │   └── GetCategories/
│   │   └── DTOs/
│   ├── Commerce/
│   │   ├── Commands/
│   │   │   ├── AddToCart/
│   │   │   ├── UpdateCartItem/
│   │   │   ├── RemoveCartItem/
│   │   │   ├── ClearCart/
│   │   │   ├── MergeCart/
│   │   │   ├── CreateCheckout/
│   │   │   ├── ProcessWebhook/
│   │   │   ├── UpdateOrderStatus/
│   │   │   └── AssignTracking/
│   │   ├── Queries/
│   │   │   ├── GetCart/
│   │   │   ├── GetOrders/
│   │   │   ├── GetOrderById/
│   │   │   └── GetAllOrders/
│   │   ├── DTOs/
│   │   │   ├── CartDto.cs
│   │   │   ├── OrderDto.cs
│   │   │   ├── OrderListDto.cs
│   │   │   └── ShippingAddressDto.cs
│   │   └── CommerceMappingConfig.cs
│   ├── Courses/
│   │   ├── Commands/
│   │   │   ├── CreateCourse/
│   │   │   ├── UpdateCourse/
│   │   │   └── DeleteCourse/
│   │   ├── Queries/
│   │   │   ├── GetCourses/
│   │   │   └── GetCourseById/
│   │   └── DTOs/
│   ├── Branches/
│   │   ├── Commands/
│   │   │   ├── CreateBranch/
│   │   │   ├── UpdateBranch/
│   │   │   └── DeleteBranch/
│   │   ├── Queries/
│   │   │   └── GetBranches/
│   │   └── DTOs/
│   ├── ExchangeRate/
│   │   ├── Queries/
│   │   │   └── GetExchangeRate/
│   │   └── DTOs/
│   ├── Intelligence/
│   │   ├── Queries/
│   │   │   └── GetRecommendations/
│   │   └── DTOs/
│   └── Admin/
│       ├── BulkImport/
│       │   ├── Commands/
│       │   │   └── ProcessBulkImport/
│       │   ├── Queries/
│       │   │   └── GetImportHistory/
│       │   └── DTOs/
│       └── BulkExport/
│           ├── Queries/
│           │   └── ExportProducts/
│           └── DTOs/
```

### src/LzDetailingStore.Api/

```
Api/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── DependencyInjection.cs
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs
│   ├── CorrelationIdMiddleware.cs
│   └── SecurityHeadersMiddleware.cs
├── Endpoints/
│   ├── IEndpointGroup.cs
│   ├── Identity/
│   │   └── AuthEndpoints.cs
│   ├── Products/
│   │   ├── ProductEndpoints.cs
│   │   └── AdminProductEndpoints.cs
│   ├── Brands/
│   │   ├── BrandEndpoints.cs
│   │   └── AdminBrandEndpoints.cs
│   ├── Categories/
│   │   ├── CategoryEndpoints.cs
│   │   └── AdminCategoryEndpoints.cs
│   ├── Commerce/
│   │   ├── CartEndpoints.cs
│   │   ├── CheckoutEndpoints.cs
│   │   ├── OrderEndpoints.cs
│   │   ├── AdminOrderEndpoints.cs
│   │   ├── ShippingAddressEndpoints.cs
│   │   └── WebhookEndpoints.cs
│   ├── Content/
│   │   ├── CourseEndpoints.cs
│   │   ├── AdminCourseEndpoints.cs
│   │   ├── BranchEndpoints.cs
│   │   └── AdminBranchEndpoints.cs
│   ├── ExchangeRate/
│   │   └── ExchangeRateEndpoints.cs
│   └── Admin/
│       ├── ImportEndpoints.cs
│       ├── ExportEndpoints.cs
│       ├── UserEndpoints.cs
│       └── RoleEndpoints.cs
├── Extensions/
│   ├── EndpointExtensions.cs
│   ├── ResultExtensions.cs
│   └── ServiceCollectionExtensions.cs
└── Auth/
    ├── PermissionRequirement.cs
    └── PermissionPolicyProvider.cs
```

### src/LzDetailingStore.Infrastructure/

```
Infrastructure/
├── DependencyInjection.cs
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Repositories/
│   │   └── Repository.cs
│   ├── Configurations/
│   │   ├── Identity/
│   │   │   ├── UserConfiguration.cs
│   │   │   ├── RoleConfiguration.cs
│   │   │   ├── PermissionConfiguration.cs
│   │   │   ├── ModuleConfiguration.cs
│   │   │   ├── OAuthAccountConfiguration.cs
│   │   │   └── RefreshTokenConfiguration.cs
│   │   ├── Catalog/
│   │   │   ├── ProductConfiguration.cs
│   │   │   ├── ProductVariantConfiguration.cs
│   │   │   ├── BrandConfiguration.cs
│   │   │   └── CategoryConfiguration.cs
│   │   ├── Commerce/
│   │   │   ├── CartConfiguration.cs
│   │   │   ├── CartItemConfiguration.cs
│   │   │   ├── OrderConfiguration.cs
│   │   │   ├── OrderItemConfiguration.cs
│   │   │   └── ShippingAddressConfiguration.cs
│   │   └── Content/
│   │       ├── CourseConfiguration.cs
│   │       ├── BranchConfiguration.cs
│   │       └── ExchangeRateConfiguration.cs
│   ├── Extensions/
│   │   └── ProductQueryExtensions.cs
│   ├── Migrations/
│   └── Seed/
│       └── SeedData.cs
├── Auth/
│   ├── JwtService.cs
│   ├── BcryptPasswordHasher.cs
│   ├── PermissionAuthorizationHandler.cs
│   └── OAuthValidator.cs
├── Services/
│   ├── S3Service.cs
│   ├── NoOpEmailService.cs
│   ├── BanxicoService.cs
│   ├── MercadoPagoService.cs
│   ├── AuditService.cs
│   ├── SlugGenerator.cs
│   ├── ExcelBulkImportService.cs
│   └── ExcelBulkExportService.cs
├── AI/
│   ├── OnnxEmbeddingService.cs
│   └── EmbeddingOptions.cs
└── Jobs/
    ├── ExchangeRateJob.cs
    ├── RegenerateEmbeddingJob.cs
    └── ProcessBulkImportJob.cs
```

---

## Summary

This design provides a complete implementation blueprint for the LZ Detailing Store backend covering all 58 requirements across 8 phases. Key architectural decisions:

- **Vertical slices** keep related code together (command + handler + validator + DTO per feature)
- **Result pattern** ensures consistent error handling without exceptions for business logic
- **Domain events** decouple side effects (embeddings, audit) from core operations
- **Pipeline behaviors** centralize cross-cutting concerns (logging, validation, transactions)
- **Permission-based auth** with DB-driven roles provides dynamic RBAC without restart
- **Graceful degradation** throughout (embedding service, Banxico, MercadoPago fallbacks)
