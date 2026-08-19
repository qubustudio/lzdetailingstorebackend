# Architecture Decisions

## Architecture Style: Clean Architecture + Vertical Slices + Lightweight CQRS

### Why This Architecture?

- **Clean Architecture** ensures domain logic is independent of infrastructure (testable, portable)
- **Vertical Slices** organize code by feature (Products, Orders, Cart) instead of technical layers — easier to navigate and maintain
- **Lightweight CQRS** separates reads (Queries) from writes (Commands) using MediatR, without the complexity of event sourcing or separate databases

### Solution Structure

```
src/
├── LzDetailingStore.Api/              # Presentation layer (endpoints, middleware)
├── LzDetailingStore.Application/      # Use cases (commands, queries, handlers)
├── LzDetailingStore.Domain/           # Entities, value objects, domain events
├── LzDetailingStore.Infrastructure/   # EF Core, repos, external services
├── LzDetailingStore.SharedKernel/     # Cross-cutting: Result pattern, base entities
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

- Domain has ZERO external dependencies
- Application depends on Domain only (defines interfaces/ports)
- Infrastructure implements interfaces from Application (adapters)
- Api references Application and Infrastructure for DI wiring

## Key Architecture Decisions

### ADR-001: RBAC Dynamic (Role-Based Access Control)

- Roles are stored in database, not hardcoded
- Permissions are assigned to roles dynamically
- Modules group permissions logically (Products, Orders, Users, etc.)
- Users can have multiple roles
- Authorization checks use policy-based auth in ASP.NET Core

### ADR-002: Multi-Currency via Banxico API

- Prices stored in MXN only (single source of truth)
- Exchange rate fetched from Banxico SIE API (serie SF43718) via Background Service
- Rate cached in database table `ExchangeRates` + in-memory cache (1h TTL)
- Frontend requests specify desired currency; backend converts on the fly
- Mercado Pago handles payment in the user's chosen currency

### ADR-003: Product Recommendations via pgvector

- Products have an `embedding` column (vector(384)) in PostgreSQL
- Local model: ML.NET + ONNX runtime with all-MiniLM-L6-v2 (384 dimensions)
- No external API dependency (no OpenAI/Google) — zero cost, full data privacy
- Embeddings generated from: name + brand + category + description + specs
- Cosine similarity search finds semantically related products
- Results: top 10 nearest → random 3-6 selection (controlled randomness)
- Embedding regenerated via background job when product is created/updated

### ADR-004: Observability Stack

- **Structured Logging**: Serilog with enrichers (CorrelationId, UserId, TraceId)
- **Distributed Tracing**: OpenTelemetry with Jaeger/Tempo exporter
- **Metrics**: OpenTelemetry with Prometheus exporter
- **Health Checks**: /health/live (liveness) + /health/ready (readiness)
- **Correlation ID**: Middleware propagates X-Correlation-ID header across all logs/traces
- **Audit Log**: Database table recording all write operations (who, what, when, from where)

### ADR-005: API Versioning

- URL-based versioning: `/api/v1/...`
- Breaking changes require new version
- Multiple versions can coexist during migration periods

### ADR-006: Authentication Flow

- JWT tokens (24h expiration) for API access
- Refresh tokens (7 days) stored in database, rotated on each use
- OAuth flow: frontend gets provider token → backend validates → issues JWT
- Password recovery via email with time-limited tokens

### ADR-007: Cart Merge Strategy

- Anonymous cart lives in frontend localStorage (48h TTL)
- On login, frontend calls POST /carrito/merge with localStorage items
- Server merges: existing item → sum quantities; new item → add to cart
- Server cart is the source of truth post-login

### ADR-008: File Storage (AWS S3)

- All images uploaded to S3 with CDN distribution
- Presigned URLs for uploads from admin panel
- Public read access via CloudFront/CDN URL
- Organized by entity: `products/{id}/`, `brands/{id}/`, `courses/{id}/`

### ADR-009: Rate Limiting

- Public endpoints: 100 requests/minute per IP
- Authenticated endpoints: 200 requests/minute per user
- Webhook endpoints: No rate limiting (Mercado Pago IPs whitelisted)
- Implementation: ASP.NET Core Rate Limiting middleware

### ADR-010: Scalar for API Documentation

- Scalar replaces Swagger UI (Swashbuckle removed)
- Uses native Microsoft.AspNetCore.OpenApi for document generation
- Scalar provides interactive UI for testing endpoints
- Available only in Development/Staging environments
