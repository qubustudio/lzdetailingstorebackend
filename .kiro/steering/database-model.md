# Database Model — PostgreSQL 17

## Extensions Required

- `pgvector` — Vector similarity search for product recommendations
- `uuid-ossp` — UUID generation (or use application-level UUIDs)

## Schema Overview

### Identity & Access Module

#### Users
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| Nombre | varchar(100) | NOT NULL |
| Email | varchar(254) | NOT NULL, UNIQUE |
| PasswordHash | varchar(512) | NULL (OAuth users may not have password) |
| Telefono | varchar(20) | NULL |
| IsActive | boolean | DEFAULT true |
| EmailVerified | boolean | DEFAULT false |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

#### Roles
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| Nombre | varchar(50) | NOT NULL, UNIQUE |
| Descripcion | varchar(200) | NULL |
| IsSystem | boolean | DEFAULT false (system roles cannot be deleted) |
| CreatedAt | timestamptz | NOT NULL |

**Seed Roles:** Cliente, Admin, SuperAdmin

#### Modules
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| Nombre | varchar(50) | NOT NULL |
| Slug | varchar(50) | NOT NULL, UNIQUE |
| Descripcion | varchar(200) | NULL |
| Orden | int | DEFAULT 0 |

**Seed Modules:** Productos, Categorias, Marcas, Cursos, Sucursales, Ordenes, Usuarios, Roles, Configuracion, Dashboard

#### Permissions
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| ModuleId | uuid | FK → Modules |
| Nombre | varchar(50) | NOT NULL |
| Slug | varchar(100) | NOT NULL, UNIQUE |
| Descripcion | varchar(200) | NULL |

**Seed Permissions per Module:** Ver, Crear, Editar, Eliminar (e.g., `productos.ver`, `productos.crear`)

#### RolePermissions
| Column | Type | Constraints |
|--------|------|-------------|
| RoleId | uuid | FK → Roles, PK |
| PermissionId | uuid | FK → Permissions, PK |

#### UserRoles
| Column | Type | Constraints |
|--------|------|-------------|
| UserId | uuid | FK → Users, PK |
| RoleId | uuid | FK → Roles, PK |
| AssignedAt | timestamptz | NOT NULL |
| AssignedBy | uuid | FK → Users, NULL |

#### OAuthAccounts
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | FK → Users |
| Provider | varchar(20) | NOT NULL (google, apple, microsoft) |
| ProviderUserId | varchar(200) | NOT NULL |
| Email | varchar(254) | NULL |
| CreatedAt | timestamptz | NOT NULL |

**Unique:** (Provider, ProviderUserId)

#### RefreshTokens
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | FK → Users |
| Token | varchar(512) | NOT NULL, UNIQUE |
| ExpiresAt | timestamptz | NOT NULL |
| CreatedAt | timestamptz | NOT NULL |
| RevokedAt | timestamptz | NULL |
| ReplacedByToken | varchar(512) | NULL |

### Catalog Module

#### Categories
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| Nombre | varchar(100) | NOT NULL |
| Slug | varchar(100) | NOT NULL, UNIQUE |
| Descripcion | varchar(500) | NULL |
| IsActive | boolean | DEFAULT true |
| Orden | int | DEFAULT 0 |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

#### Brands
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| Nombre | varchar(100) | NOT NULL |
| Slug | varchar(100) | NOT NULL, UNIQUE |
| LogoUrl | varchar(500) | NULL |
| Descripcion | varchar(1000) | NULL |
| Categoria | varchar(50) | NULL (CERAMICOS, PULIDO, LAVADO, HERRAMIENTAS) |
| Badge | varchar(50) | NULL |
| Origen | varchar(100) | NULL |
| Especialidad | varchar(200) | NULL |
| Destacada | boolean | DEFAULT false |
| HistoriaCompleta | text | NULL |
| ImagenSpotlight | varchar(500) | NULL |
| IsActive | boolean | DEFAULT true |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

#### Products
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| BrandId | uuid | FK → Brands |
| CategoryId | uuid | FK → Categories |
| Sku | varchar(50) | NOT NULL, UNIQUE |
| Name | varchar(200) | NOT NULL |
| Slug | varchar(200) | NOT NULL, UNIQUE |
| Description | text | NULL |
| PrecioBase | decimal(10,2) | NULL (minimum variant price for catalog display) |
| PrecioOriginal | decimal(10,2) | NULL (discount "before" price) |
| ImageUrl | varchar(500) | NOT NULL |
| ImageAlt | varchar(200) | NULL |
| Rating | decimal(2,1) | DEFAULT 0 |
| ReviewCount | int | DEFAULT 0 |
| Badge | varchar(50) | NULL |
| IsActive | boolean | DEFAULT true |
| Embedding | vector(384) | NULL (pgvector, all-MiniLM-L6-v2) |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

**Indexes:** BrandId, CategoryId, Slug, Sku, IsActive, Price, embedding (ivfflat/hnsw)

#### ProductImages
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| ProductId | uuid | FK → Products |
| ImageUrl | varchar(500) | NOT NULL |
| ImageAlt | varchar(200) | NULL |
| Orden | int | DEFAULT 0 |

#### ProductSpecs
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| ProductId | uuid | FK → Products |
| Label | varchar(100) | NOT NULL |
| Value | varchar(200) | NOT NULL |
| Orden | int | DEFAULT 0 |

#### ProductApplicationSteps
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| ProductId | uuid | FK → Products |
| StepNumber | int | NOT NULL |
| Title | varchar(100) | NOT NULL |
| Description | varchar(500) | NOT NULL |

#### ProductVariants
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

### Academy Module

#### Courses
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| Title | varchar(200) | NOT NULL |
| Description | text | NULL |
| Level | varchar(20) | NOT NULL (BASICO, INTERMEDIO, AVANZADO) |
| Duration | decimal(4,1) | NOT NULL (hours) |
| ImageUrl | varchar(500) | NULL |
| ImageAlt | varchar(200) | NULL |
| Status | varchar(20) | DEFAULT 'DRAFT' (ACTIVE, DRAFT) |
| IsActive | boolean | DEFAULT true |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

#### CourseTopics
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| CourseId | uuid | FK → Courses |
| Nombre | varchar(100) | NOT NULL |
| Orden | int | DEFAULT 0 |

#### CourseModules
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| CourseId | uuid | FK → Courses |
| Number | int | NOT NULL |
| Title | varchar(200) | NOT NULL |
| Description | varchar(500) | NULL |
| Orden | int | DEFAULT 0 |

### Commerce Module

#### Carts
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | FK → Users, UNIQUE |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

#### CartItems
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| CartId | uuid | FK → Carts |
| ProductId | uuid | FK → Products |
| VariantId | uuid | FK → ProductVariants, NOT NULL |
| Quantity | int | NOT NULL, CHECK > 0 |
| UnitPrice | decimal(10,2) | NOT NULL |
| CreatedAt | timestamptz | NOT NULL |

**Unique:** (CartId, VariantId)

#### Orders
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | FK → Users |
| Subtotal | decimal(12,2) | NOT NULL |
| Impuestos | decimal(12,2) | NOT NULL |
| Envio | decimal(10,2) | DEFAULT 0 |
| Total | decimal(12,2) | NOT NULL |
| Estado | varchar(20) | NOT NULL (PENDIENTE, PAGADA, ENVIADA, ENTREGADA, CANCELADA) |
| MetodoPago | varchar(50) | NULL |
| Tracking | varchar(100) | NULL |
| Currency | varchar(3) | DEFAULT 'MXN' |
| ExchangeRate | decimal(10,4) | NULL (rate at time of purchase) |
| MercadoPagoPreferenceId | varchar(100) | NULL |
| MercadoPagoPaymentId | varchar(100) | NULL |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

#### OrderItems
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| OrderId | uuid | FK → Orders |
| ProductId | uuid | FK → Products |
| VariantId | uuid | FK → ProductVariants, NULL |
| ProductName | varchar(200) | NOT NULL (snapshot) |
| ProductImage | varchar(500) | NULL (snapshot) |
| VariantName | varchar(100) | NULL (snapshot, e.g., "1 Galón") |
| Sku | varchar(50) | NULL (snapshot) |
| UnitPrice | decimal(10,2) | NOT NULL |
| Quantity | int | NOT NULL |
| Subtotal | decimal(12,2) | NOT NULL |

#### ShippingAddresses
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | FK → Users |
| Alias | varchar(50) | NULL |
| Calle | varchar(200) | NOT NULL |
| NumExterior | varchar(20) | NOT NULL |
| NumInterior | varchar(20) | NULL |
| Colonia | varchar(100) | NOT NULL |
| Ciudad | varchar(100) | NOT NULL |
| Estado | varchar(100) | NOT NULL |
| CodigoPostal | varchar(5) | NOT NULL |
| Referencia | varchar(200) | NULL |
| EsPrincipal | boolean | DEFAULT false |
| CreatedAt | timestamptz | NOT NULL |

### Business Module

#### Branches
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| Nombre | varchar(100) | NOT NULL |
| Descripcion | varchar(500) | NULL |
| Direccion | varchar(300) | NOT NULL |
| Telefono | varchar(20) | NULL |
| Email | varchar(254) | NULL |
| GoogleMapsUrl | varchar(1000) | NULL |
| ImagenUrl | varchar(500) | NULL |
| ImagenAlt | varchar(200) | NULL |
| IsActive | boolean | DEFAULT true |
| CreatedAt | timestamptz | NOT NULL |
| UpdatedAt | timestamptz | NOT NULL |

#### ExchangeRates
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| FromCurrency | varchar(3) | NOT NULL (USD) |
| ToCurrency | varchar(3) | NOT NULL (MXN) |
| Rate | decimal(10,4) | NOT NULL |
| Source | varchar(50) | NOT NULL (banxico, manual) |
| FetchedAt | timestamptz | NOT NULL |
| CreatedAt | timestamptz | NOT NULL |

**Index:** (FromCurrency, ToCurrency, FetchedAt DESC) — latest rate query

### Observability Module

#### BulkImportLog
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

**Indexes**: (UserId), (Status), (CreatedAt DESC)

#### AuditLogs
| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | FK → Users, NULL (system actions) |
| Action | varchar(50) | NOT NULL (Create, Update, Delete, Login, etc.) |
| EntityType | varchar(100) | NOT NULL (Product, Order, User, etc.) |
| EntityId | varchar(50) | NULL |
| OldValues | jsonb | NULL |
| NewValues | jsonb | NULL |
| IpAddress | varchar(45) | NULL |
| UserAgent | varchar(500) | NULL |
| CorrelationId | varchar(50) | NULL |
| CreatedAt | timestamptz | NOT NULL |

**Index:** (EntityType, EntityId), (UserId), (CreatedAt DESC)

## Relationships Summary

```
Users 1──N UserRoles N──1 Roles
Roles 1──N RolePermissions N──1 Permissions
Permissions N──1 Modules
Users 1──N OAuthAccounts
Users 1──N RefreshTokens
Users 1──1 Carts
Users 1──N Orders
Users 1──N ShippingAddresses
Users 1──N BulkImportLog
Brands 1──N Products
Categories 1──N Products
Products 1──N ProductImages
Products 1──N ProductSpecs
Products 1──N ProductApplicationSteps
Products 1──N ProductVariants
Carts 1──N CartItems
CartItems N──1 Products
CartItems N──1 ProductVariants
Orders 1──N OrderItems
OrderItems N──1 Products
OrderItems N──1 ProductVariants (nullable)
Courses 1──N CourseTopics
Courses 1──N CourseModules
```

## Pricing Model

- **PrecioVenta** (in ProductVariants): Retail price shown to customers
- **PrecioCosto** (in ProductVariants): Wholesale cost paid to supplier (admin-only visibility)
- **PrecioBase** (in Products): Reference/minimum variant price for catalog listing display
- **PrecioOriginal** (in Products): Discount "before" price (for promotional display)
- No wholesale/distributor pricing tiers for customers in MVP
