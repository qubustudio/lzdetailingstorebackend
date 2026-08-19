# SDD Proposal — LZ Detailing Store Backend

## Date: 2025-01-15
## Phase: Proposal
## Status: Draft

---

## 1. Intent

Build a production-ready .NET 10 Web API for LZ Detailing Store, an automotive detailing e-commerce platform serving the Mexican market. The system provides a product catalog with multi-size variants, secure checkout via Mercado Pago, AI-powered product recommendations using local embeddings (pgvector), and a dynamic RBAC admin panel. The backend supports a single retail storefront with physical branch locations, a course academy listing, and multi-currency display via Banxico exchange rates. This is a single-tenant, single-server deployment optimized for a small-to-medium e-commerce operation (~100 concurrent users).

---

## 2. Scope

### In Scope (MVP)

| Module | Features |
|--------|----------|
| **Authentication** | JWT (24h) + Refresh Tokens (7d, rotation) + OAuth (Google, Apple, Microsoft) + Password Recovery |
| **Product Catalog** | Products with multiple size variants (500ml, 1L, 32oz, 1gal, etc.), each with independent SKU, price, and stock |
| **Brands & Categories** | CRUD, slugs, spotlight branding, category tree |
| **Cart** | Server-side cart, anonymous merge on login, variant-aware items |
| **Checkout & Payments** | Mercado Pago Checkout Pro, order creation, payment webhooks |
| **Orders** | Full lifecycle (PENDIENTE → PAGADA → ENVIADA → ENTREGADA → CANCELADA), tracking |
| **Academy** | Course listing with modules and topics (display only) |
| **Branches** | Physical store locations with maps and contact info |
| **AI Recommendations** | Local embeddings (ML.NET + ONNX, all-MiniLM-L6-v2, 384d), pgvector cosine similarity, top-10 → random 3-6 |
| **Exchange Rate** | Banxico API (serie SF43718), background refresh every 4h, IMemoryCache fallback |
| **Bulk Import/Export** | Excel via ClosedXML, async processing via Hangfire, import history and error tracking |
| **Admin Panel API** | Dynamic RBAC (roles + permissions in DB), module-based permission grouping |
| **Observability** | Serilog (console + Seq in dev), OpenTelemetry (tracing + metrics), health checks, audit log |

### Out of Scope

| Feature | Reason |
|---------|--------|
| Wholesale/distributor pricing tiers | Single retail pricing model (PrecioVenta only for customers) |
| Course enrollment/payment | Academy is listing-only for MVP; enrollment is a future phase |
| Email notifications | IEmailService interface defined, no provider implemented |
| Mobile push notifications | No mobile app in initial release |
| Multi-tenant / multi-store | Single store deployment; architecture does not preclude future multi-tenancy |

---

## 3. Architecture Overview

### Pattern: Clean Architecture + Vertical Slices + Lightweight CQRS

```
┌─────────────────────────────────────────────────────┐
│                    API Layer                          │
│         (Controllers, Middleware, Filters)            │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│                Application Layer                     │
│   (Commands, Queries, Handlers, Validators, DTOs)    │
│              Organized by Feature Slice              │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│                  Domain Layer                         │
│     (Entities, Value Objects, Domain Events)         │
│              ZERO external dependencies              │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│              Infrastructure Layer                     │
│  (EF Core, Repos, S3, MercadoPago, Banxico, ONNX)   │
└─────────────────────────────────────────────────────┘
```

### Solution Structure

```
src/
├── LzDetailingStore.Api/              # Presentation (endpoints, middleware, filters)
├── LzDetailingStore.Application/      # Use cases (commands, queries, handlers, validators)
├── LzDetailingStore.Domain/           # Entities, value objects, domain events, enums
├── LzDetailingStore.Infrastructure/   # EF Core, repositories, external service adapters
├── LzDetailingStore.SharedKernel/     # Cross-cutting: Result<T>, base entities, interfaces
tests/
├── LzDetailingStore.UnitTests/
├── LzDetailingStore.IntegrationTests/
├── LzDetailingStore.ArchTests/
docker/
├── docker-compose.yml
├── docker-compose.override.yml
├── Dockerfile
├── .env.example
```

### Dependency Rule

```
Api → Application → Domain ← Infrastructure
                        ↑
                  SharedKernel
```

- **Domain**: Zero external dependencies. Pure business logic.
- **Application**: Depends on Domain only. Defines ports (interfaces).
- **Infrastructure**: Implements Application interfaces (adapters).
- **Api**: Wires DI, references Application and Infrastructure.
- **SharedKernel**: Referenced by all layers for cross-cutting concerns.

### Key Patterns

- **MediatR** for command/query dispatch (no direct service injection in controllers)
- **FluentValidation** for request validation (pipeline behavior)
- **Result<T>** pattern for error handling (no exceptions for business logic)
- **Mapster** for DTO mapping
- **Repository pattern** with EF Core (unit of work via DbContext)
- **Domain events** for side effects (embedding regeneration, audit logging)

---

## 4. Database Model Changes Required

Based on price list analysis (7 PDFs, ~120 products across 7 brands), the current database model requires these modifications:

### ADD: ProductVariants Table

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| ProductId | uuid | FK → Products, NOT NULL |
| Sku | varchar(50) | NOT NULL, UNIQUE |
| Nombre | varchar(100) | NOT NULL (e.g., "500 ml", "1 Galón") |
| PrecioVenta | decimal(10,2) | NOT NULL (retail price shown to customer) |
| PrecioCosto | decimal(10,2) | NOT NULL (wholesale cost, admin only) |
| SizeValue | decimal(8,2) | NOT NULL (numeric: 500, 1, 32) |
| SizeUnit | varchar(20) | NOT NULL (ml, litros, oz, galon, gr) |
| Stock | int | DEFAULT 0 |
| Barcode | varchar(50) | NULL |
| IsActive | boolean | DEFAULT true |
| SortOrder | int | DEFAULT 0 |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

**Indexes**: (ProductId, IsActive), Sku (unique), (ProductId, SortOrder)

### ADD: BulkImportLog Table

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | FK → Users |
| ImportType | varchar(50) | NOT NULL (products, brands, categories) |
| FileName | varchar(255) | NOT NULL |
| FileUrl | varchar(500) | NULL (S3 path) |
| TotalRows | int | DEFAULT 0 |
| SuccessCount | int | DEFAULT 0 |
| ErrorCount | int | DEFAULT 0 |
| ErrorsDetail | jsonb | NULL |
| Status | varchar(30) | NOT NULL (pending, processing, completed, failed) |
| StartedAt | timestamptz | NULL |
| CompletedAt | timestamptz | NULL |
| CreatedAt | timestamptz | NOT NULL |

### MODIFY: Products Table

| Change | Details |
|--------|---------|
| REMOVE | `Price` column (replaced by variant prices) |
| REMOVE | `Sku` column (moved to variants) |
| REMOVE | `StockQuantity` column (moved to variants) |
| REMOVE | `InStock` column (computed from variants) |
| ADD | `PrecioBase` decimal(10,2) NULL — reference/minimum variant price for catalog display |
| RENAME | `OriginalPrice` → `PrecioOriginal` decimal(10,2) NULL — discount "before" price |
| CHANGE | `Embedding` vector(1536) → `Embedding` vector(384) — matches all-MiniLM-L6-v2 output |

### MODIFY: CartItems Table

| Change | Details |
|--------|---------|
| ADD | `VariantId` uuid FK → ProductVariants, NOT NULL |
| CHANGE | Unique constraint: (CartId, ProductId) → (CartId, VariantId) |

### MODIFY: OrderItems Table

| Change | Details |
|--------|---------|
| ADD | `VariantId` uuid FK → ProductVariants, NULL (nullable for historical orders) |
| ADD | `VariantName` varchar(100) NULL — snapshot of variant name at purchase time |
| ADD | `Sku` varchar(50) NULL — snapshot of SKU at purchase time |

### Updated Relationships

```
Products 1──N ProductVariants
CartItems N──1 ProductVariants
OrderItems N──1 ProductVariants (nullable)
```

---

## 5. Implementation Phases

### Phase 1: Foundation
- Solution scaffold (Clean Architecture structure)
- Docker Compose (PostgreSQL 17 + pgvector, Seq)
- EF Core setup with snake_case naming conventions
- Base entities, Result<T> pattern, SharedKernel
- Global exception handling middleware
- API versioning (URL-based: `/api/v1/`)
- Health checks (liveness + readiness)
- Serilog configuration (console + Seq sink)
- Scalar API documentation

### Phase 2: Identity & Access
- User registration and login (JWT + Refresh Tokens)
- OAuth integration (Google, Apple, Microsoft)
- Password recovery flow (IEmailService interface)
- Dynamic RBAC (Roles, Permissions, Modules)
- Policy-based authorization middleware
- Seed data (SuperAdmin, Admin, Cliente roles)

### Phase 3: Catalog
- Products CRUD (with PrecioBase computed from variants)
- ProductVariants CRUD (multiple sizes/prices per product)
- Brands CRUD (with spotlight and categorization)
- Categories CRUD (with ordering)
- Product images (S3 upload via presigned URLs)
- Product specs and application steps
- Slug generation and validation

### Phase 4: Commerce
- Cart management (server-side, variant-aware)
- Anonymous cart merge on login
- Checkout flow → Mercado Pago Checkout Pro
- Payment webhooks (IPN notifications)
- Order creation and lifecycle management
- Shipping addresses CRUD
- Order history (user + admin views)

### Phase 5: Content & Business
- Course listing (CRUD, modules, topics)
- Branch management (locations, maps, contact)
- Exchange rate service (Banxico API + Hangfire job + IMemoryCache)

### Phase 6: Intelligence
- ML.NET + ONNX runtime setup (all-MiniLM-L6-v2)
- Embedding generation pipeline (product text → vector)
- pgvector similarity search (cosine, top-10 → random 3-6)
- Background job: regenerate embeddings on product create/update
- Recommendation endpoint

### Phase 7: Admin Operations
- Bulk import (Excel → Products/Brands/Categories via ClosedXML)
- Bulk export (Products/Brands → Excel download)
- Import history and error reporting (BulkImportLog)
- Excel template download endpoints
- Async processing via Hangfire

### Phase 8: Observability & Hardening
- OpenTelemetry (distributed tracing + metrics)
- Correlation ID middleware
- Audit log (all write operations)
- Rate limiting (100/min public, 200/min authenticated)
- Input validation hardening
- Security headers middleware
- CORS configuration

---

## 6. Tech Stack Summary

### Core Framework

| Package | Version | Purpose |
|---------|---------|---------|
| .NET 10 | 10.x | Runtime and SDK |
| ASP.NET Core 10 | 10.x | Web API framework |
| MediatR | 12.x | Lightweight CQRS (command/query dispatch) |
| FluentValidation | 11.x | Request validation pipeline |
| Mapster | 7.x | Object-to-DTO mapping |

### Database & ORM

| Package | Version | Purpose |
|---------|---------|---------|
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.x | EF Core provider for PostgreSQL |
| Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime | 9.x | NodaTime type mapping (optional) |
| pgvector (Npgsql plugin) | latest | Vector similarity search |
| EFCore.NamingConventions | 8.x | snake_case column naming |

### Authentication & Security

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.x | JWT token validation |
| BCrypt.Net-Next | 4.x | Password hashing |
| System.IdentityModel.Tokens.Jwt | 8.x | JWT token generation |

### Observability

| Package | Version | Purpose |
|---------|---------|---------|
| Serilog.AspNetCore | 8.x | Structured logging |
| Serilog.Sinks.Seq | latest | Seq log sink (dev) |
| Serilog.Sinks.Console | latest | Console sink |
| OpenTelemetry.Extensions.Hosting | latest | OTel host integration |
| OpenTelemetry.Instrumentation.AspNetCore | latest | HTTP tracing |
| OpenTelemetry.Instrumentation.EntityFrameworkCore | latest | DB tracing |
| AspNetCore.HealthChecks.NpgSql | latest | PostgreSQL health check |

### File Storage & Background Jobs

| Package | Version | Purpose |
|---------|---------|---------|
| AWSSDK.S3 | 3.x | S3 file uploads |
| ClosedXML | 0.104.x | Excel import/export |
| Hangfire | 1.8.x | Background job processing |
| Hangfire.PostgreSql | latest | Hangfire storage in PostgreSQL |

### Payments

| Package | Version | Purpose |
|---------|---------|---------|
| MercadoPago SDK | 2.x | Mercado Pago Checkout Pro integration |

### AI / Embeddings

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.ML | latest | ML.NET runtime |
| Microsoft.ML.OnnxRuntime | latest | ONNX model inference |
| all-MiniLM-L6-v2 (ONNX) | — | Sentence embedding model (384 dimensions) |

### API Documentation

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.OpenApi | 10.x | OpenAPI document generation |
| Scalar.AspNetCore | latest | Interactive API documentation UI |

### Caching

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Extensions.Caching.Memory | 10.x | IMemoryCache (built-in, single server) |

### Testing

| Package | Version | Purpose |
|---------|---------|---------|
| xUnit | 2.x | Test framework |
| FluentAssertions | 7.x | Readable test assertions |
| NSubstitute | 5.x | Mocking library |
| Testcontainers | 4.x | Docker-based integration tests |
| Bogus | 35.x | Fake data generation |

---

## 7. Risks and Mitigations

| # | Risk | Impact | Likelihood | Mitigation |
|---|------|--------|------------|------------|
| 1 | **Banxico API downtime** | No exchange rate updates | Medium | Fallback to last DB rate + IMemoryCache (1h TTL) + alert on consecutive failures |
| 2 | **pgvector hosting support** | Recommendations unavailable | Low | Verify extension support before deployment; graceful degradation returns random popular products |
| 3 | **.NET 10 maturity** | Potential breaking changes in preview | Low | Use GA release only; pin package versions |
| 4 | **Bulk import large files** | Request timeout, memory pressure | Medium | Async processing via Hangfire; 10MB file size limit; chunked processing (100 rows/batch) |
| 5 | **ONNX model size in memory** | High RAM usage on local server | Low | Model loaded once at startup (~90MB); singleton lifetime; lazy initialization |
| 6 | **Mercado Pago webhook reliability** | Missed payment confirmations | Medium | Idempotent webhook handling; retry mechanism; manual payment verification endpoint for admin |
| 7 | **S3 region latency** | Slow image uploads from Mexico | Low | Use us-east-1 or closest region; presigned URLs shift upload to client → CDN |
| 8 | **Single server SPOF** | Complete downtime on failure | Medium | Acceptable for MVP (~99% target); future: load balancer + multiple instances |

---

## 8. Non-Functional Requirements

### Performance

| Metric | Target |
|--------|--------|
| Catalog query response (p95) | < 200ms |
| Recommendation query (p95) | < 500ms |
| Checkout creation (p95) | < 1s |
| Bulk import (1000 rows) | < 30s |

### Scalability

| Metric | Target |
|--------|--------|
| Concurrent users | ~100 simultaneous |
| Product catalog size | Up to 5,000 products with variants |
| Daily orders | Up to 200 |

### Availability

| Metric | Target |
|--------|--------|
| Uptime | 99% (acceptable for small e-commerce) |
| Planned maintenance window | Weekday 2-4 AM CST |
| Recovery time (RTO) | < 1 hour |

### Security

| Requirement | Implementation |
|-------------|----------------|
| OWASP Top 10 compliance | Input validation, parameterized queries, CSRF protection, security headers |
| Password storage | BCrypt with cost factor 12 |
| Token security | JWT (24h) + Refresh Token (7d) with rotation |
| API rate limiting | 100 req/min (public), 200 req/min (authenticated) |
| Data at rest | PostgreSQL encryption (TDE if available) |
| Data in transit | HTTPS/TLS 1.3 enforced |
| Admin access | Dynamic RBAC with permission-based policies |
| Audit trail | All write operations logged with user, action, timestamp, IP |

### Observability

| Requirement | Implementation |
|-------------|----------------|
| Structured logging | Serilog with correlation ID enrichment |
| Distributed tracing | OpenTelemetry with spans for HTTP, DB, external calls |
| Health monitoring | /health/live (liveness) + /health/ready (readiness) |
| Error alerting | Seq dashboards in dev; future: external alerting |

---

## 9. Locked Decisions Summary

| # | Decision | Choice | Rationale |
|---|----------|--------|-----------|
| 1 | Embeddings | Local (ML.NET + ONNX, all-MiniLM-L6-v2, 384d) | No external API dependency, no per-request cost, full data privacy |
| 2 | Cache | IMemoryCache | Single server deployment, no Redis infrastructure needed |
| 3 | Email | Abstract IEmailService, no provider | Decoupled interface; provider chosen when email features are needed |
| 4 | SKU | Manual entry by admin | Business prefers control over SKU format |
| 5 | Hosting/Logs | Local server, Serilog → Console + Seq (dev) | Simple deployment, Seq for structured log exploration |
| 6 | Pricing | PrecioVenta + PrecioCosto per variant | Retail shown to customers; cost visible to admin only |
| 7 | Architecture | Clean Architecture + Vertical Slices + CQRS | Testable, maintainable, feature-organized |
| 8 | Database | PostgreSQL 17 + pgvector | Mature RDBMS + native vector search in one database |
| 9 | Payments | Mercado Pago Checkout Pro | Dominant payment platform in Mexico, handles card processing |
| 10 | File Storage | AWS S3 | Reliable, CDN-ready, presigned uploads |

---

## Appendix: API Endpoint Overview (High-Level)

### Public (no auth)
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/oauth/{provider}`
- `GET /api/v1/productos` (paginated, filterable)
- `GET /api/v1/productos/{slug}`
- `GET /api/v1/productos/{id}/recomendaciones`
- `GET /api/v1/marcas`
- `GET /api/v1/categorias`
- `GET /api/v1/cursos`
- `GET /api/v1/sucursales`
- `GET /api/v1/tipo-cambio`

### Authenticated (Cliente+)
- `GET/POST/PUT/DELETE /api/v1/carrito`
- `POST /api/v1/carrito/merge`
- `POST /api/v1/checkout`
- `GET /api/v1/ordenes`
- `GET /api/v1/ordenes/{id}`
- `GET/POST/PUT/DELETE /api/v1/direcciones`

### Admin (permission-based)
- `CRUD /api/v1/admin/productos`
- `CRUD /api/v1/admin/variantes`
- `CRUD /api/v1/admin/marcas`
- `CRUD /api/v1/admin/categorias`
- `CRUD /api/v1/admin/cursos`
- `CRUD /api/v1/admin/sucursales`
- `CRUD /api/v1/admin/usuarios`
- `CRUD /api/v1/admin/roles`
- `POST /api/v1/admin/importar/{tipo}`
- `GET /api/v1/admin/importar/plantilla/{tipo}`
- `GET /api/v1/admin/importar/historial`
- `GET /api/v1/admin/exportar/{tipo}`
- `GET /api/v1/admin/ordenes` (all orders, filterable)
- `PUT /api/v1/admin/ordenes/{id}/estado`

### Webhooks
- `POST /api/v1/webhooks/mercadopago`
