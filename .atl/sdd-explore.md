# SDD Explore — LZ Detailing Store Backend

## Fecha: 2026-08-11
## Fase: Exploration — Price List Analysis & Data Model Validation

---

## 1. Price List Analysis (7 PDFs)

### Brands Analyzed:
1. **Kers** (Mexico) — 19 products, sizes in ml and gr
2. **MajorWheel** (Mexico) — ~10 premium products, long descriptions with usage instructions
3. **3D Car Care** (USA) — ~27 products, sizes in oz and gallon
4. **3D GLW Series** (USA) — ~7 premium products, same oz/gallon sizes
5. **Gyeon EVO** (Korea) — ~35 products, sizes in ml and liters (up to 4L)
6. **Jescar** (USA) — ~6 polish/sealant products, oz and gallon
7. **Nanolex** (Germany) — ~15 products, sizes in ml

### Key Data Patterns:
- **Size variants**: One product has 2-4 different size presentations
- **Units of measurement**: ml, liters, oz, gallon, gr (5 distinct units)
- **Two price levels**: Public and Distributor (~15% average discount)
- **Price range**: $299 MXN (glass cleaner) to $4,999 MXN (graphene ceramic)
- **No explicit SKU**: Will be generated with pattern `{BRAND}-{PRODUCT}-{VARIANT}`
- **No images in price lists**: Will be uploaded separately via S3
- **Implicit categories**: Shampoo, APC, Iron Remover, Tire, Wax, Polish, Compound, Ceramic Coating, Leather, Glass, Rim

---

## 2. Critical Gap: ProductVariants Table Missing

The current database model has `Products` with a single `Price` and `OriginalPrice`, but the price lists show that **each product has multiple size presentations** with different prices.

### Required Changes to Database Model:

#### Add: ProductVariants table
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| ProductId | uuid | FK -> Products |
| Sku | varchar(50) | NOT NULL, UNIQUE |
| Nombre | varchar(100) | NOT NULL (e.g., "500 ml", "1 galon") |
| PrecioPublico | decimal(10,2) | NOT NULL |
| PrecioDistribuidor | decimal(10,2) | NOT NULL |
| SizeValue | decimal(8,2) | NOT NULL (numeric value: 500, 1, 32) |
| SizeUnit | varchar(20) | NOT NULL (ml, litros, oz, galon, gr) |
| Stock | int | DEFAULT 0 |
| Barcode | varchar(50) | NULL |
| IsActive | boolean | DEFAULT true |
| SortOrder | int | DEFAULT 0 |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

#### Modify: Products table
- Remove: `Price` (single price doesn't make sense with variants)
- Add: `PrecioBase` decimal(10,2) NULL (reference price, lowest variant)
- Add: `PrecioDistribuidor` decimal(10,2) NULL (reference distributor price)
- Keep: `OriginalPrice` → rename to `PrecioOriginal` (for "before" price in discounts)

#### Add: BulkImportLog table
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | FK -> Users |
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

---

## 3. API Endpoints Needed for Bulk Import/Export

### New Admin Endpoints:
- `POST /admin/importar/productos` — Import products from Excel/CSV (multipart)
- `POST /admin/importar/marcas` — Import brands
- `POST /admin/importar/categorias` — Import categories
- `GET /admin/importar/plantilla/:tipo` — Download Excel template
- `GET /admin/importar/historial` — Import history (paginated)
- `GET /admin/importar/:id/errores` — Import error details
- `GET /admin/exportar/productos` — Export products to Excel
- `GET /admin/exportar/marcas` — Export brands to Excel

---

## 4. Detected Categories (for seed data)

Based on 7 price lists:
1. Shampoos / Soaps
2. Cleaners (APC, Interior, Glass, Fabric)
3. Decontaminants (Iron Remover, Bug Remover, Tar Remover)
4. Protection (Waxes, Sealants, Spray Wax)
5. Ceramics (Coatings, Spray Sealants, Boosters)
6. Polishes (Compound, Polish, One-Step)
7. Tires & Rims (Tire Gel, Rim Cleaner)
8. Leather & Vinyl (Leather Cleaner, Conditioner)
9. Quick Detailers
10. Accessories & Tools (Clay Lube, Prep)

---

## 5. NuGet Packages Investigated

### Core
| Package | Purpose |
|---------|---------|
| MediatR 12.x | Lightweight CQRS |
| FluentValidation 11.x | Request validation |
| Mapster 7.x | DTO mapping |

### Database
| Package | Purpose |
|---------|---------|
| Npgsql.EntityFrameworkCore.PostgreSQL 9.x | EF Core + PostgreSQL |
| Npgsql pgvector plugin | Vector embeddings |
| EFCore.NamingConventions 8.x | snake_case |

### Auth
| Package | Purpose |
|---------|---------|
| Microsoft.AspNetCore.Authentication.JwtBearer | JWT |
| BCrypt.Net-Next 4.x | Password hashing |

### Observability
| Package | Purpose |
|---------|---------|
| Serilog.AspNetCore 8.x | Structured logging |
| OpenTelemetry | Tracing/Metrics |

### Files & Background
| Package | Purpose |
|---------|---------|
| AWSSDK.S3 | File uploads |
| ClosedXML 0.104.x | Excel import/export |
| Hangfire 1.8.x | Background jobs |

### Payments
| Package | Purpose |
|---------|---------|
| MercadoPago SDK .NET 2.x | Mercado Pago integration |

### Testing
| Package | Purpose |
|---------|---------|
| xUnit 2.x | Test framework |
| FluentAssertions 7.x | Readable assertions |
| NSubstitute 5.x | Mocking |
| Testcontainers 4.x | Integration tests |
| Bogus 35.x | Fake data |

---

## 6. Banxico API — Exchange Rate

- Endpoint: `GET https://www.banxico.org.mx/SieAPIRest/service/v1/series/SF43718/datos/oportuno`
- Header: `Bmx-Token: {API_TOKEN}`
- Serie: SF43718 (FIX exchange rate)
- Free token registration
- Implementation: Hangfire recurring job every 4 hours + IMemoryCache fallback

---

## 7. Pending Decisions (for SDD Propose)

1. **Embeddings**: Local model (ML.NET + ONNX) vs external API (OpenAI)?
2. **Cache**: IMemoryCache (simple) vs Redis (distributed)?
3. **Email service**: Which provider?
4. **SKU generation**: Automatic with pattern or manual by admin?
5. **Log sink in production**: Seq + CloudWatch/ELK?
6. **Distributor pricing**: Visible only via role or separate price endpoint?

---

## 8. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Banxico API downtime | No exchange rate | Fallback to last DB rate + alert |
| pgvector hosting support | Recommendations broken | Verify hosting supports extension |
| .NET 10 maturity | Possible breaking changes | Use GA release |
| Bulk import large files | Timeout | Async via Hangfire, progress tracking |
