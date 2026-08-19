# LzDetailingStore Backend — Project Context

## What Is This Project?

Backend Web API for **LZ Detailing Store**, an online store specializing in automotive detailing products (ceramic coatings, waxes, polishes, tools, etc.). The store also has an academy section with courses and multiple physical branches.

## Business Domain

- **Industry**: Automotive detailing / car care products
- **Target Users**: Detailing professionals, car enthusiasts, retail customers
- **Business Model**: E-commerce with physical branches + online academy
- **Payment Provider**: Mercado Pago (Mexico)
- **Currencies**: MXN (primary) + USD (via Banxico exchange rate API)

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 10 / ASP.NET Core |
| Language | C# 14 |
| Database | PostgreSQL 17 + pgvector extension |
| ORM | Entity Framework Core 10 |
| API Docs | Scalar (replaces Swagger) + Microsoft.AspNetCore.OpenApi |
| Authentication | JWT Bearer (24h) + Refresh Tokens (7 days) |
| OAuth Providers | Google, Apple, Microsoft (Outlook) |
| Validation | FluentValidation |
| Mediator | MediatR (CQRS pattern) |
| Structured Logging | Serilog (Console + Seq sinks) |
| Tracing | OpenTelemetry (traces, metrics, logs) |
| Health Checks | ASP.NET Core Health Checks |
| File Storage | AWS S3 |
| Email | External service (TBD) |
| Exchange Rate | Banxico SIE API (serie SF43718) |
| AI/Embeddings | pgvector + ML.NET + ONNX (all-MiniLM-L6-v2, 384d) — local model |
| Containers | Docker + Docker Compose |
| Tests | xUnit + FluentAssertions + Testcontainers |

## Key Integrations

1. **Mercado Pago** — Checkout Pro (payment preferences, webhooks/IPN)
2. **Banxico SIE API** — Official USD/MXN exchange rate (serie SF43718)
3. **OAuth Providers** — Google Identity, Apple Sign In, Microsoft Identity
4. **AWS S3** — Product images, brand logos, course images
5. **Email Service** — Order confirmations, password recovery (provider TBD)
6. **COPOMEX/SEPOMEX** — Mexican postal code lookup (or local DB)

## Deployment

- Docker containers deployed to a dedicated server
- docker-compose orchestrates: API, PostgreSQL, Seq (logs), Jaeger (traces)
- Environment-based configuration (Development, Staging, Production)

## Team Context

- Frontend: Angular app consuming this API (already in development)
- Backend: This project (.NET 10 Web API)
- The frontend team provided endpoint specifications that guide API design
