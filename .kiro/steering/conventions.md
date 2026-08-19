# Code Conventions

## Language & Framework

- C# 14 with .NET 10
- Use file-scoped namespaces
- Use primary constructors where appropriate
- Use `required` keyword for mandatory properties
- Prefer records for DTOs and value objects
- Use `init` properties for immutable data

## Naming Conventions

| Element | Style | Example |
|---------|-------|---------|
| Namespaces | PascalCase | `LzDetailingStore.Application.Features.Products` |
| Classes | PascalCase | `CreateProductCommand` |
| Interfaces | I + PascalCase | `IProductRepository` |
| Methods | PascalCase | `GetByIdAsync` |
| Properties | PascalCase | `ProductName` |
| Private fields | _camelCase | `_productRepository` |
| Local variables | camelCase | `productList` |
| Constants | PascalCase | `MaxPageSize` |
| Enums | PascalCase (singular) | `OrderStatus.Pending` |
| Generic params | T + PascalCase | `TEntity`, `TResponse` |

## File Organization

- One class per file (exceptions: nested classes, small related records)
- File name matches class name exactly
- Features organized by domain concept, not technical layer

```
Features/
├── Products/
│   ├── Commands/
│   │   ├── CreateProduct/
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductCommandHandler.cs
│   │   │   └── CreateProductCommandValidator.cs
│   │   └── UpdateProduct/
│   └── Queries/
│       ├── GetProducts/
│       │   ├── GetProductsQuery.cs
│       │   ├── GetProductsQueryHandler.cs
│       │   └── ProductDto.cs
│       └── GetProductById/
```

## CQRS Naming

- Commands: `{Verb}{Entity}Command` → `CreateProductCommand`
- Queries: `Get{Entity/Entities}Query` → `GetProductsQuery`
- Handlers: `{Command/Query}Handler` → `CreateProductCommandHandler`
- DTOs: `{Entity}Dto` or `{Entity}{Context}Dto` → `ProductDto`, `ProductListDto`
- Validators: `{Command/Query}Validator` → `CreateProductCommandValidator`

## API Conventions

- Use Minimal APIs grouped by feature (`.MapGroup()`)
- Route prefix: `/api/v1/`
- Resource names in Spanish (matching frontend contract): `/productos`, `/marcas`, `/cursos`
- HTTP verbs follow REST semantics
- Always return consistent error shape: `{ error, message, statusCode }`
- Pagination: `?page=1&limit=20` → `{ data, total, page, totalPages }`
- Dates in ISO 8601 format (UTC)

## Entity Conventions

- All entities inherit from `BaseEntity` (Id, CreatedAt, UpdatedAt)
- Soft delete via `IsActive` flag (no hard deletes for business entities)
- Audit fields: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`
- Use strongly-typed IDs where appropriate (ProductId, OrderId)

## Database Conventions

- Table names: PascalCase plural (`Products`, `OrderItems`)
- Column names: PascalCase (EF Core convention)
- Foreign keys: `{Entity}Id` → `BrandId`, `CategoryId`
- Indexes on: foreign keys, frequently queried columns, unique constraints
- Use Fluent API for all configurations (no data annotations on entities)
- Each entity has its own `IEntityTypeConfiguration<T>` file

## Testing Conventions

- Unit tests: `{ClassName}Tests.cs` → `CreateProductCommandHandlerTests.cs`
- Integration tests: `{Endpoint}EndpointTests.cs` → `ProductsEndpointTests.cs`
- Use Arrange/Act/Assert pattern
- Use FluentAssertions for readable assertions
- Use Testcontainers for integration tests with real PostgreSQL

## Dependency Injection

- Register services using extension methods per layer:
  - `AddApplicationServices()`
  - `AddInfrastructureServices(configuration)`
- Use `Scrutor` for automatic assembly scanning if needed
- Prefer constructor injection over service locator

## Error Handling

- Use Result pattern (not exceptions) for business logic failures
- Reserve exceptions for truly exceptional situations
- Global exception handler middleware for unhandled exceptions
- Validation errors return 400 with detailed field errors
- Domain errors return 422 with business rule explanation

## Git Conventions

- Branch naming: `feature/{short-description}`, `fix/{short-description}`
- Commit messages: Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`)
- One logical change per commit
- PR must pass all tests before merge
