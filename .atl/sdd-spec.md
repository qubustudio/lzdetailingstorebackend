# SDD Spec — LZ Detailing Store Backend

## Date: 2025-01-15
## Phase: Specification
## Status: Draft

---

## Overview

This specification defines the functional requirements and acceptance criteria for the LZ Detailing Store backend — a .NET 10 Web API e-commerce platform for the Mexican automotive detailing market. Requirements are organized by implementation phase and written in Given/When/Then format for testability.

---

## Phase 1: Foundation

Delivers the solution scaffold, infrastructure containers, database configuration, cross-cutting concerns (logging, error handling, health checks), and API documentation. This phase produces a deployable skeleton with zero business logic.

---

#### REQ-001: Solution Scaffold

**Description**: The solution must follow Clean Architecture with 5 source projects (Api, Application, Domain, Infrastructure, SharedKernel) and 3 test projects (UnitTests, IntegrationTests, ArchTests). Project references must enforce the dependency rule: Domain has zero external dependencies, Application depends only on Domain, Infrastructure implements Application interfaces, and Api wires everything together.

**Acceptance Criteria**:
- **Given** a developer clones the repository, **When** they open the solution file, **Then** it contains exactly 8 projects organized under `src/` and `tests/` directories with correct project references.
- **Given** the dependency rule is defined, **When** an architecture test runs, **Then** Domain has no references to Application, Infrastructure, or Api; and Application has no reference to Infrastructure.
- **Given** the solution is scaffolded, **When** `dotnet build` is executed, **Then** the solution compiles with zero errors and zero warnings.

---

#### REQ-002: Docker Compose Infrastructure

**Description**: A Docker Compose configuration must provision PostgreSQL 17 with the pgvector extension enabled, and a Seq instance for structured log ingestion. The configuration must support both development and CI environments with environment variable overrides.

**Acceptance Criteria**:
- **Given** Docker is installed, **When** `docker compose up` is executed, **Then** PostgreSQL 17 starts on port 5432 with pgvector extension available and Seq starts on port 5341.
- **Given** the containers are running, **When** a SQL query `SELECT * FROM pg_extension WHERE extname = 'vector'` is executed, **Then** it returns one row confirming pgvector is installed.
- **Given** a `.env.example` file exists, **When** a developer copies it to `.env` and runs compose, **Then** all services start with the configured credentials.

---

#### REQ-003: EF Core Configuration

**Description**: Entity Framework Core must be configured with snake_case naming conventions for all database objects, UUID primary keys, and automatic `CreatedAt`/`UpdatedAt` timestamptz columns via `SaveChangesAsync` override. Migrations must be stored in the Infrastructure project.

**Acceptance Criteria**:
- **Given** EF Core is configured, **When** a migration is generated for any entity, **Then** all table and column names use snake_case (e.g., `product_variants`, `created_at`).
- **Given** an entity inherits BaseEntity, **When** it is saved for the first time, **Then** `CreatedAt` and `UpdatedAt` are automatically set to UTC now.
- **Given** an existing entity is modified, **When** `SaveChangesAsync` is called, **Then** only `UpdatedAt` is refreshed; `CreatedAt` remains unchanged.

---

#### REQ-004: Base Entities and SharedKernel

**Description**: SharedKernel must provide `BaseEntity` (Id as Guid, CreatedAt, UpdatedAt), the `Result<T>` pattern for error handling without exceptions, and common interfaces (IRepository, IUnitOfWork). All business logic failures must use Result, reserving exceptions for truly exceptional situations.

**Acceptance Criteria**:
- **Given** a handler encounters a business rule violation, **When** it returns a Result, **Then** the Result contains an error code and message without throwing an exception.
- **Given** a handler completes successfully, **When** it returns `Result<T>`, **Then** `IsSuccess` is true and `Value` contains the response data.
- **Given** any entity is instantiated, **When** its Id is inspected, **Then** it is a non-empty Guid generated at construction time.

---

#### REQ-005: Global Exception Handler Middleware

**Description**: A middleware must catch all unhandled exceptions, log them with full stack trace via Serilog, and return a consistent JSON error response `{ error, message, statusCode }` without leaking internal details to the client. In Development, the response may include stack trace; in Production it must not.

**Acceptance Criteria**:
- **Given** an unhandled exception occurs in any endpoint, **When** the middleware catches it, **Then** it returns HTTP 500 with body `{ "error": "INTERNAL_ERROR", "message": "...", "statusCode": 500 }`.
- **Given** the environment is Production, **When** an exception is thrown, **Then** the response message is generic and the stack trace is not exposed.
- **Given** any exception occurs, **When** the middleware processes it, **Then** the full exception details are logged to Serilog with correlation ID.

---

#### REQ-006: API Versioning

**Description**: All API endpoints must be versioned using URL-based versioning with the prefix `/api/v1/`. The versioning infrastructure must support future versions (v2, v3) without breaking existing clients.

**Acceptance Criteria**:
- **Given** an API endpoint is registered, **When** a client calls it without the version prefix, **Then** it returns 404 Not Found.
- **Given** the API is versioned, **When** a client calls `/api/v1/health/live`, **Then** it responds successfully with the v1 behavior.
- **Given** a future v2 is added, **When** clients call `/api/v1/` endpoints, **Then** they continue working unchanged.

---

#### REQ-007: Health Checks

**Description**: Two health check endpoints must exist: `/health/live` (liveness — application process is running) and `/health/ready` (readiness — database connection is available and responsive). Kubernetes and load balancers can use these to determine routing decisions.

**Acceptance Criteria**:
- **Given** the application is running, **When** `/health/live` is called, **Then** it returns HTTP 200 with status "Healthy" regardless of database state.
- **Given** the database is reachable, **When** `/health/ready` is called, **Then** it returns HTTP 200 with status "Healthy".
- **Given** the database is unreachable, **When** `/health/ready` is called, **Then** it returns HTTP 503 with status "Unhealthy" and a degraded reason.

---

#### REQ-008: Serilog Structured Logging

**Description**: Serilog must be configured with console sink (always active) and Seq sink (configurable via appsettings). All logs must include correlation ID enrichment, request path, and response time. Log levels must be configurable per namespace.

**Acceptance Criteria**:
- **Given** a request is processed, **When** the response is sent, **Then** a structured log entry is written containing HTTP method, path, status code, and elapsed milliseconds.
- **Given** Seq is configured in appsettings, **When** the application starts, **Then** logs are forwarded to the Seq instance and visible in its UI.
- **Given** a correlation ID header is present on a request, **When** logs are written during that request, **Then** all log entries include the same correlation ID property.

---

#### REQ-009: Scalar API Documentation

**Description**: The API must expose an OpenAPI document and a Scalar interactive documentation UI at a configurable path. All endpoints, request/response models, and authentication schemes must be documented automatically from the code.

**Acceptance Criteria**:
- **Given** the application is running in Development, **When** the developer navigates to the Scalar UI path, **Then** they see an interactive API explorer listing all endpoints grouped by tag.
- **Given** an endpoint accepts a request body, **When** its documentation is viewed, **Then** the request schema shows all properties with types, constraints, and examples.
- **Given** the environment is Production, **When** the Scalar path is accessed, **Then** access can be restricted via configuration.

---

#### REQ-010: Correlation ID Middleware

**Description**: Every incoming request must have a correlation ID. If the `X-Correlation-ID` header is present, use it; otherwise generate a new GUID. The correlation ID must be propagated to all logs, outgoing HTTP calls, and returned in the response header.

**Acceptance Criteria**:
- **Given** a request arrives without `X-Correlation-ID`, **When** the middleware processes it, **Then** a new GUID is generated, added to the logging context, and returned in the response `X-Correlation-ID` header.
- **Given** a request arrives with `X-Correlation-ID: abc-123`, **When** the middleware processes it, **Then** all logs for that request include `CorrelationId: abc-123` and the response returns the same header value.
- **Given** the application makes an outgoing HTTP call during request processing, **When** the call is sent, **Then** the `X-Correlation-ID` header is forwarded to the downstream service.

---

## Phase 2: Identity & Access

Delivers user authentication (registration, login, JWT, refresh tokens, OAuth), password recovery, dynamic role-based access control with permission granularity, and user profile management.

---

#### REQ-011: User Registration

**Description**: New users can register with email, password, full name, and optional phone number. Email must be unique across the system. Password must be hashed with BCrypt (cost factor 12). Upon successful registration, the user is assigned the "Cliente" role automatically.

**Acceptance Criteria**:
- **Given** a valid registration payload (email, password ≥ 6 chars, name), **When** POST `/api/v1/auth/registro` is called, **Then** the user is created, assigned "Cliente" role, and a success response is returned.
- **Given** an email that already exists in the system, **When** registration is attempted with that email, **Then** HTTP 409 is returned with error code `EMAIL_ALREADY_EXISTS`.
- **Given** a registration payload with missing required fields, **When** the endpoint is called, **Then** HTTP 400 is returned with field-level validation errors.

---

#### REQ-012: Login with Credentials

**Description**: Users authenticate with email and password. On success, the system returns a JWT (24-hour expiry) and a Refresh Token (7-day expiry). The JWT contains user ID, email, and role claims. Failed login attempts must not reveal whether the email exists.

**Acceptance Criteria**:
- **Given** valid credentials (existing email + correct password), **When** POST `/api/v1/auth/login` is called, **Then** HTTP 200 is returned with `{ accessToken, refreshToken, expiresIn }`.
- **Given** an incorrect password for an existing email, **When** login is attempted, **Then** HTTP 401 is returned with a generic "Invalid credentials" message.
- **Given** a non-existent email, **When** login is attempted, **Then** HTTP 401 is returned with the same generic message (no email enumeration).

---

#### REQ-013: Refresh Token Rotation

**Description**: Refresh tokens are single-use. When a client exchanges a refresh token for a new JWT, the old refresh token is revoked and a new one is issued (rotation). Using a revoked refresh token invalidates the entire token family for that user as a security measure.

**Acceptance Criteria**:
- **Given** a valid, non-expired refresh token, **When** POST `/api/v1/auth/refresh` is called, **Then** a new JWT and a new refresh token are returned, and the old refresh token is marked as revoked.
- **Given** an already-revoked refresh token, **When** it is used to request a new JWT, **Then** HTTP 401 is returned and all refresh tokens for that user are revoked (token family invalidation).
- **Given** a refresh token that has expired (> 7 days), **When** it is used, **Then** HTTP 401 is returned requiring re-authentication.

---

#### REQ-014: OAuth Login

**Description**: Users can authenticate via Google, Apple, or Microsoft. The backend receives a provider-issued token, validates it against the provider's token endpoint, and either finds an existing linked account or creates a new user. The response is the same JWT + Refresh Token pair as credential login.

**Acceptance Criteria**:
- **Given** a valid Google/Apple/Microsoft token for a new user, **When** POST `/api/v1/auth/oauth/{provider}` is called, **Then** a new user is created with the provider linked in OAuthAccounts, assigned "Cliente" role, and JWT is returned.
- **Given** a valid provider token for an existing linked account, **When** OAuth login is called, **Then** the existing user is authenticated and JWT is returned without creating a duplicate.
- **Given** an invalid or expired provider token, **When** OAuth login is attempted, **Then** HTTP 401 is returned with error `INVALID_PROVIDER_TOKEN`.

---

#### REQ-015: Password Recovery

**Description**: Users can request a password reset by providing their email. The system generates a time-limited token (1 hour) and calls `IEmailService.SendPasswordResetAsync()`. The actual email sending is abstracted behind an interface (no provider implemented in MVP).

**Acceptance Criteria**:
- **Given** a registered email, **When** POST `/api/v1/auth/recuperar-password` is called, **Then** a reset token is generated, `IEmailService` is invoked, and HTTP 200 is returned (always, even if email doesn't exist — no enumeration).
- **Given** a valid reset token within 1 hour, **When** the password reset endpoint is called with a new password, **Then** the password is updated and all existing refresh tokens for that user are revoked.
- **Given** a reset token older than 1 hour, **When** password reset is attempted, **Then** HTTP 400 is returned with error `TOKEN_EXPIRED`.

---

#### REQ-016: Dynamic RBAC

**Description**: Authorization is managed through Roles, Permissions, and Modules stored in the database. Permissions follow the pattern `{module}.{action}` (e.g., `productos.crear`). Roles are collections of permissions. Users can have multiple roles. Changes to roles/permissions take effect on the next request (no cached policies).

**Acceptance Criteria**:
- **Given** an admin creates a new role with permissions `[productos.ver, productos.crear]`, **When** that role is assigned to a user, **Then** the user can access product listing and creation endpoints but not update or delete.
- **Given** a permission is removed from a role, **When** a user with that role makes the next request to the now-forbidden endpoint, **Then** HTTP 403 is returned.
- **Given** seed data is applied, **When** the database is initialized, **Then** SuperAdmin has all permissions, Admin has all except user/role management, and Cliente has no admin permissions.

---

#### REQ-017: Policy-Based Authorization

**Description**: Each admin endpoint is protected by a permission-based policy. The authorization handler reads the user's roles from the JWT claims, loads their permissions from the database, and checks against the required permission for the endpoint.

**Acceptance Criteria**:
- **Given** a user with role "Admin" and permission `productos.crear`, **When** they call POST `/api/v1/admin/productos`, **Then** the request is authorized and proceeds.
- **Given** a user with role "Cliente" (no admin permissions), **When** they call any `/api/v1/admin/*` endpoint, **Then** HTTP 403 Forbidden is returned.
- **Given** a user with an expired JWT, **When** they call any protected endpoint, **Then** HTTP 401 Unauthorized is returned before permission checks.

---

#### REQ-018: Seed Data

**Description**: On first database migration, the system seeds three roles (SuperAdmin, Admin, Cliente), all modules (Productos, Categorias, Marcas, Cursos, Sucursales, Ordenes, Usuarios, Roles, Configuracion, Dashboard), and permissions (Ver, Crear, Editar, Eliminar per module). A default SuperAdmin user is created from environment variables.

**Acceptance Criteria**:
- **Given** a fresh database with no data, **When** migrations are applied, **Then** 3 roles, 10 modules, and 40 permissions exist with correct role-permission assignments.
- **Given** the seed has already been applied, **When** migrations run again, **Then** no duplicate records are created (idempotent seeding).
- **Given** environment variables for SuperAdmin credentials, **When** the seed runs, **Then** a SuperAdmin user exists with the configured email and a BCrypt-hashed password.

---

#### REQ-019: User Profile CRUD

**Description**: Authenticated users can view and update their own profile (name, phone, avatar). Avatar upload goes to S3 and the URL is stored. Users cannot change their own role or email via this endpoint.

**Acceptance Criteria**:
- **Given** an authenticated user, **When** GET `/api/v1/auth/perfil` is called, **Then** their profile data (name, email, phone, avatar URL, roles) is returned.
- **Given** an authenticated user, **When** PUT `/api/v1/auth/perfil` is called with updated name and phone, **Then** the profile is updated and the response reflects the changes.
- **Given** a profile update payload that includes role or email changes, **When** the endpoint is called, **Then** those fields are ignored and only allowed fields are updated.

---

## Phase 3: Catalog

Delivers the product catalog with multi-size variants, brands with spotlight, categories with ordering, product images, specs, application steps, slug generation, and filtering/pagination.

---

#### REQ-020: Products CRUD

**Description**: Admin can create, read, update, and delete products. Each product has Name, Slug, Description, BrandId, CategoryId, ImageUrl, and computed PrecioBase (minimum active variant price). Public users can list and view products with pagination and filtering.

**Acceptance Criteria**:
- **Given** an admin with `productos.crear` permission, **When** POST `/api/v1/admin/productos` is called with valid data, **Then** the product is created with an auto-generated slug and HTTP 201 is returned.
- **Given** a product with 3 active variants priced at $299, $599, $1199, **When** the product is retrieved, **Then** `PrecioBase` equals $299 (minimum of active variant prices).
- **Given** a public user, **When** GET `/api/v1/productos` is called, **Then** only active products are returned with pagination metadata `{ data, total, page, totalPages }`.

---

#### REQ-021: Product Variants CRUD

**Description**: Each product can have multiple size variants, each with its own SKU (manually entered), name (e.g., "500 ml"), PrecioVenta, PrecioCosto, SizeValue, SizeUnit, Stock, and Barcode. PrecioCosto is only visible to users with admin permissions.

**Acceptance Criteria**:
- **Given** an admin creating a variant for product X, **When** POST is called with SKU, name, prices, size, and stock, **Then** the variant is created and the product's PrecioBase is recalculated.
- **Given** a public user viewing a product, **When** its variants are listed, **Then** PrecioVenta, name, stock availability, and size are shown but PrecioCosto is excluded from the response.
- **Given** a variant SKU that already exists in the system, **When** another variant is created with the same SKU, **Then** HTTP 409 is returned with error `SKU_ALREADY_EXISTS`.

---

#### REQ-022: PrecioBase Auto-Calculation

**Description**: A product's PrecioBase is computed as the minimum PrecioVenta across all its active variants. It updates automatically whenever a variant is created, updated, activated, or deactivated. If no active variants exist, PrecioBase is null.

**Acceptance Criteria**:
- **Given** a product with active variants at prices [$299, $599, $1199], **When** PrecioBase is computed, **Then** it equals $299.
- **Given** the cheapest variant is deactivated, **When** the recalculation runs, **Then** PrecioBase updates to the next lowest active variant price.
- **Given** a product with all variants deactivated, **When** PrecioBase is computed, **Then** it is null and the product can optionally be hidden from public listings.

---

#### REQ-023: Brands CRUD

**Description**: Admin can create, read, update, and delete brands. Each brand has a name, slug, logo URL, description, category (CERAMICOS, PULIDO, LAVADO, HERRAMIENTAS), origin country, specialty, and a "Destacada" (spotlight) toggle for featuring on the homepage.

**Acceptance Criteria**:
- **Given** an admin creates a brand with name "Gyeon EVO", **When** the brand is saved, **Then** a slug `gyeon-evo` is auto-generated and the brand is retrievable by slug.
- **Given** a brand has `Destacada = true`, **When** the public brand listing is fetched, **Then** the brand appears in the spotlight/featured section.
- **Given** a brand has associated products, **When** a delete is attempted, **Then** it performs a soft delete (IsActive = false) instead of hard delete.

---

#### REQ-024: Categories CRUD

**Description**: Admin can create, read, update, and delete categories. Each category has a name, slug, description, ordering (Orden field), and IsActive flag. Categories are displayed in the order specified by the Orden field.

**Acceptance Criteria**:
- **Given** categories exist with Orden values [3, 1, 2], **When** the public category list is fetched, **Then** they are returned sorted by Orden ascending: [1, 2, 3].
- **Given** an admin updates a category's Orden from 3 to 1, **When** the list is fetched again, **Then** the ordering reflects the change.
- **Given** a category has associated products, **When** soft delete is performed, **Then** the category becomes inactive and its products remain but may be filtered out of public listings.

---

#### REQ-025: Product Images

**Description**: Each product can have multiple images stored in S3. One image is designated as primary (the main ImageUrl on the product). Additional images are stored in the ProductImages table with ordering. Upload is handled via presigned URLs or direct multipart upload.

**Acceptance Criteria**:
- **Given** an admin uploads an image for a product, **When** the upload completes, **Then** the image URL is stored in ProductImages with the correct ordering and the S3 object is accessible.
- **Given** a product has 5 images, **When** the product detail is fetched, **Then** all images are returned ordered by their Orden field with the primary image identified.
- **Given** an admin deletes a product image, **When** the deletion completes, **Then** the image record is removed and the S3 object is deleted.

---

#### REQ-026: Product Specs and Application Steps

**Description**: Products have key-value specification pairs (e.g., "Contenido: 500ml", "pH: 7") and ordered application step instructions (e.g., Step 1: "Lavar el vehículo", Step 2: "Aplicar producto"). Both are managed by admin through sub-resource endpoints.

**Acceptance Criteria**:
- **Given** an admin adds specs to a product, **When** the product detail is fetched, **Then** specs appear as ordered key-value pairs in the response.
- **Given** an admin adds 5 application steps, **When** the product detail is fetched, **Then** steps appear ordered by StepNumber with title and description.
- **Given** a step is deleted from the middle of the sequence, **When** remaining steps are fetched, **Then** step numbers can be reordered by the admin.

---

#### REQ-027: Slug Auto-Generation

**Description**: Slugs are auto-generated from entity names using URL-safe transformation: lowercase, spaces to hyphens, remove special characters, normalize accented characters. Slugs must be unique per entity type. If a collision occurs, a numeric suffix is appended.

**Acceptance Criteria**:
- **Given** a product named "3D Glass Cleaner HD", **When** the slug is generated, **Then** it equals `3d-glass-cleaner-hd`.
- **Given** a product named "Gyeon Q² Mohs+", **When** the slug is generated, **Then** it equals `gyeon-q2-mohs` (special chars removed, accents normalized).
- **Given** a slug `iron-remover` already exists, **When** another product with the same name is created, **Then** the slug becomes `iron-remover-2`.

---

#### REQ-028: Catalog Filtering and Pagination

**Description**: The public product listing supports pagination (page/limit), filtering by brand, category, price range, and full-text search on product name and description. Results can be sorted by price (asc/desc), name, or newest first.

**Acceptance Criteria**:
- **Given** 50 products exist, **When** GET `/api/v1/productos?page=2&limit=20` is called, **Then** products 21-40 are returned with `{ total: 50, page: 2, totalPages: 3 }`.
- **Given** products across multiple brands, **When** `?brandId={id}` filter is applied, **Then** only products from that brand are returned.
- **Given** a search query `?search=ceramic`, **When** the endpoint is called, **Then** products whose name or description contain "ceramic" (case-insensitive) are returned.
- **Given** a price range filter `?minPrice=500&maxPrice=1000`, **When** applied, **Then** only products with PrecioBase within that range are returned.

---

## Phase 4: Commerce

Delivers the server-side cart with variant awareness, cart merge for anonymous-to-authenticated transition, Mercado Pago checkout integration, order lifecycle management, and shipping address management.

---

#### REQ-029: Server-Side Cart

**Description**: Each authenticated user has exactly one server-side cart. Cart items reference a specific ProductVariant (not just a product). The cart tracks quantity and unit price. Cart is persistent across sessions.

**Acceptance Criteria**:
- **Given** an authenticated user with no cart, **When** they add their first item, **Then** a cart is automatically created and the item is added with the variant's current PrecioVenta.
- **Given** a user's cart has item X (variant A, qty 2), **When** they add variant A again with qty 1, **Then** the quantity is updated to 3 (not a duplicate item).
- **Given** a user's cart has items, **When** they log out and back in, **Then** the cart persists with the same items.

---

#### REQ-030: Cart Stock Validation

**Description**: When adding items to the cart, the system must validate that the requested quantity does not exceed available stock for that variant. If stock is insufficient, the operation fails with a clear message indicating available stock.

**Acceptance Criteria**:
- **Given** a variant with stock = 5, **When** a user tries to add qty = 3, **Then** the item is added successfully.
- **Given** a variant with stock = 5, **When** a user tries to add qty = 8, **Then** HTTP 422 is returned with message indicating only 5 units available.
- **Given** a cart item with qty = 3 and stock = 5, **When** the user updates quantity to 6, **Then** HTTP 422 is returned with the stock limit.

---

#### REQ-031: Cart Merge

**Description**: When a user logs in, their localStorage cart (anonymous items) can be merged with the server cart via POST `/api/v1/carrito/merge`. The merge adds new items and updates quantities for existing variants. Server cart quantities take precedence on conflicts.

**Acceptance Criteria**:
- **Given** a server cart with [variant A: qty 2] and localStorage cart with [variant B: qty 1], **When** merge is called, **Then** the server cart contains [variant A: qty 2, variant B: qty 1].
- **Given** a server cart with [variant A: qty 2] and localStorage with [variant A: qty 3], **When** merge is called, **Then** variant A quantity becomes the server value (2) or the max of both, per business rule.
- **Given** a localStorage item references a non-existent or inactive variant, **When** merge is called, **Then** that item is silently skipped and other items are merged successfully.

---

#### REQ-032: Checkout with Mercado Pago

**Description**: POST `/api/v1/checkout` creates a Mercado Pago payment preference from the user's cart, creates an Order with status PENDIENTE, and returns the Mercado Pago preference URL for redirect. Cart is not cleared until payment is confirmed.

**Acceptance Criteria**:
- **Given** a user with items in cart and a default shipping address, **When** POST `/api/v1/checkout` is called, **Then** a Mercado Pago preference is created, an Order with status PENDIENTE is saved, and the preference `init_point` URL is returned.
- **Given** a user with an empty cart, **When** checkout is attempted, **Then** HTTP 422 is returned with error `CART_EMPTY`.
- **Given** checkout succeeds, **When** the order is inspected, **Then** OrderItems contain snapshots (ProductName, ProductImage, VariantName, Sku, UnitPrice) independent of current product data.

---

#### REQ-033: Mercado Pago Webhook

**Description**: POST `/api/v1/webhooks/mercadopago` receives IPN (Instant Payment Notification) from Mercado Pago. The handler validates the webhook signature, looks up the order by MercadoPagoPaymentId, and updates the status to PAGADA. Processing must be idempotent.

**Acceptance Criteria**:
- **Given** a valid webhook notification with payment status "approved", **When** the webhook is processed, **Then** the Order status changes from PENDIENTE to PAGADA and the user's cart is cleared.
- **Given** a webhook with an invalid signature, **When** it arrives, **Then** HTTP 400 is returned and no order is modified.
- **Given** a duplicate webhook for an already-PAGADA order, **When** processed, **Then** HTTP 200 is returned with no state change (idempotent).

---

#### REQ-034: Order Lifecycle

**Description**: Orders follow the lifecycle: PENDIENTE → PAGADA → ENVIADA → ENTREGADA or CANCELADA. Only valid transitions are allowed. Admin can advance orders through states and assign tracking numbers. Users can view their own order history.

**Acceptance Criteria**:
- **Given** an order in status PAGADA, **When** admin changes status to ENVIADA with a tracking number, **Then** the status updates and tracking is saved.
- **Given** an order in status ENVIADA, **When** someone attempts to change it back to PENDIENTE, **Then** HTTP 422 is returned with error `INVALID_STATUS_TRANSITION`.
- **Given** an order in status PENDIENTE for more than 30 minutes without payment, **When** inspected, **Then** it can be marked CANCELADA by admin or a background job.

---

#### REQ-035: Order Items Snapshot

**Description**: When an order is created, OrderItems store a point-in-time snapshot of product data: ProductName, ProductImage, VariantName, Sku, and UnitPrice. These values never change even if the product or variant is later modified or deleted.

**Acceptance Criteria**:
- **Given** an order was placed with product "3D Glass Cleaner" at $299, **When** the product name is later changed to "3D Premium Glass Cleaner", **Then** the order item still shows "3D Glass Cleaner" and $299.
- **Given** a variant is deleted after an order, **When** the order detail is fetched, **Then** all snapshot fields (name, image, variant name, SKU, price) are intact.
- **Given** a new order is created, **When** order items are saved, **Then** each item has non-null ProductName and UnitPrice regardless of current product state.

---

#### REQ-036: Shipping Addresses

**Description**: Authenticated users can manage multiple shipping addresses. Each address has: alias, street, exterior/interior numbers, colonia, city, state, postal code, and reference. One address can be marked as default (EsPrincipal). CRUD operations are scoped to the authenticated user.

**Acceptance Criteria**:
- **Given** a user creates their first address with EsPrincipal = true, **When** saved, **Then** that address becomes the default.
- **Given** a user has 3 addresses with address B as default, **When** address C is set as default, **Then** address B's EsPrincipal becomes false and address C's becomes true (only one default).
- **Given** a user tries to access another user's address by ID, **When** the endpoint is called, **Then** HTTP 404 is returned (resource not found for their scope).

---

#### REQ-037: Admin Order Management

**Description**: Admins can list all orders with filters (status, date range, user), change order status, assign tracking numbers, and view full order details including user information.

**Acceptance Criteria**:
- **Given** an admin with `ordenes.ver` permission, **When** GET `/api/v1/admin/ordenes?estado=PAGADA` is called, **Then** all orders with status PAGADA are returned with pagination.
- **Given** an admin assigns tracking "FEDEX123456" to an order, **When** the user views their order, **Then** the tracking number is visible.
- **Given** an admin with `ordenes.editar` permission, **When** PUT `/api/v1/admin/ordenes/{id}/estado` is called with a valid transition, **Then** the order status is updated and an audit log entry is created.

---

## Phase 5: Content & Business

Delivers course listing (display-only), branch management, and exchange rate service with Banxico API integration and caching.

---

#### REQ-038: Courses CRUD

**Description**: Admin can create, read, update, and delete courses. Each course has title, description, level (BASICO, INTERMEDIO, AVANZADO), duration in hours, image, and nested modules and topics. Courses are display-only for public users — no enrollment or payment.

**Acceptance Criteria**:
- **Given** an admin creates a course with 3 modules and 5 topics, **When** the public fetches the course detail, **Then** all modules and topics are returned in order.
- **Given** a course has status DRAFT, **When** the public listing is fetched, **Then** draft courses are excluded; only ACTIVE courses appear.
- **Given** a public user, **When** GET `/api/v1/cursos` is called, **Then** courses are returned with title, description, level, duration, and image — no admin-only fields.

---

#### REQ-039: Branches CRUD

**Description**: Admin can manage physical branch locations. Each branch has name, address, phone, email, Google Maps URL, and image. Public users can view all active branches.

**Acceptance Criteria**:
- **Given** an admin creates a branch with all fields populated, **When** the public listing is fetched, **Then** the branch appears with name, address, phone, email, Google Maps link, and image.
- **Given** a branch is set to inactive, **When** the public listing is fetched, **Then** that branch is excluded.
- **Given** a branch has a Google Maps URL, **When** the detail is viewed, **Then** the URL is returned for the frontend to render a maps embed.

---

#### REQ-040: Exchange Rate Service

**Description**: The system fetches the USD→MXN exchange rate from Banxico API (serie SF43718) every 4 hours via a background job. The rate is cached in IMemoryCache with 1-hour TTL. If the API is unavailable, the last rate stored in the database is used as fallback. A public endpoint exposes the current rate.

**Acceptance Criteria**:
- **Given** the Banxico API returns rate 17.25, **When** the background job runs, **Then** the rate is saved to ExchangeRates table and cached in memory.
- **Given** the cache has a valid rate (< 1h old), **When** GET `/api/v1/tipo-cambio` is called, **Then** the cached rate is returned without hitting the database.
- **Given** the Banxico API is unreachable, **When** the background job runs, **Then** the system logs a warning and the last database rate continues serving requests.
- **Given** both cache and API are unavailable, **When** the endpoint is called, **Then** the most recent rate from the ExchangeRates table is returned.

---

## Phase 6: Intelligence

Delivers AI-powered product recommendations using local ML.NET + ONNX embeddings with pgvector similarity search.

---

#### REQ-041: Embedding Model Setup

**Description**: The all-MiniLM-L6-v2 ONNX model is loaded at application startup as a singleton service. It produces 384-dimensional sentence embeddings. The model remains in memory for the application's lifetime (~90MB RAM).

**Acceptance Criteria**:
- **Given** the ONNX model file exists in the configured path, **When** the application starts, **Then** the model is loaded once and available as a singleton `IEmbeddingService`.
- **Given** the model is loaded, **When** `GenerateEmbeddingAsync("test sentence")` is called, **Then** it returns a float array of exactly 384 dimensions.
- **Given** the model file is missing or corrupt, **When** the application starts, **Then** it logs an error but starts successfully with recommendations degraded (no crash).

---

#### REQ-042: Product Embedding Generation

**Description**: For each product, an embedding is generated by concatenating: name + brand name + category name + description + specs (all key-value pairs). The concatenated text is tokenized and passed through the model. The resulting vector is stored in the product's Embedding column (vector(384)).

**Acceptance Criteria**:
- **Given** a product with name, brand, category, description, and 3 specs, **When** the embedding is generated, **Then** the concatenated input includes all those fields and produces a 384-dimension vector stored in the Embedding column.
- **Given** a product is created or updated, **When** the domain event fires, **Then** a background job is enqueued to regenerate that product's embedding.
- **Given** a product has no description or specs, **When** the embedding is generated, **Then** it still succeeds using available fields (name + brand + category minimum).

---

#### REQ-043: Recommendation Endpoint

**Description**: GET `/api/v1/productos/{id}/recomendaciones` finds the top-10 most similar products by cosine distance on their embedding vectors, then returns a random subset of 3-6 from those 10. This provides variety on repeated calls while maintaining relevance.

**Acceptance Criteria**:
- **Given** a product with a valid embedding and 20+ other products with embeddings, **When** the recommendation endpoint is called, **Then** it returns between 3 and 6 products that are semantically similar.
- **Given** the same product ID is called twice, **When** the random selection differs, **Then** the results may vary while all remaining within the top-10 similar set.
- **Given** a product with a null embedding, **When** recommendations are requested, **Then** an empty array is returned with HTTP 200 (graceful degradation, no error).

---

#### REQ-044: Graceful Degradation

**Description**: If the embedding model is unavailable (failed to load, OOM, or model file missing) or a product has no embedding, the recommendation endpoint returns an empty array with HTTP 200. No error is surfaced to the end user. The system logs a warning for monitoring.

**Acceptance Criteria**:
- **Given** the embedding service reports unavailable, **When** any recommendation request arrives, **Then** HTTP 200 with an empty array `[]` is returned.
- **Given** the model crashed mid-request, **When** the error is caught, **Then** a warning is logged with the exception details and an empty result is returned.
- **Given** observability is configured, **When** degradation occurs, **Then** a metric/log entry is recorded for alerting purposes.

---

## Phase 7: Admin Operations

Delivers bulk import/export functionality for products, brands, and categories via Excel files, with async processing for large files and full error tracking.

---

#### REQ-045: Bulk Import Upload

**Description**: Admin can upload an Excel (.xlsx) file via POST multipart to `/api/v1/admin/importar/{tipo}` where tipo is "products", "brands", or "categories". The system validates the file format, parses each row, and creates/updates entities. A BulkImportLog record tracks the operation.

**Acceptance Criteria**:
- **Given** an admin uploads a valid Excel file with 50 product rows, **When** processing completes, **Then** a BulkImportLog is created with status "completed", TotalRows = 50, SuccessCount reflecting created/updated rows.
- **Given** an uploaded file has invalid format (not .xlsx), **When** the upload is attempted, **Then** HTTP 400 is returned with error `INVALID_FILE_FORMAT`.
- **Given** a file row references a brand that doesn't exist, **When** processed, **Then** the brand is auto-created and the product is associated with it.

---

#### REQ-046: Import Row Validation

**Description**: Each row is validated against the entity schema: SKU uniqueness, required fields present, numeric values valid, foreign key references exist (or are auto-created for brands/categories). Invalid rows are recorded in ErrorsDetail (jsonb) without stopping the entire import.

**Acceptance Criteria**:
- **Given** a row with a duplicate SKU, **When** processed, **Then** the row is skipped, ErrorCount increments, and ErrorsDetail records the row number and reason.
- **Given** a file with 100 rows where 5 have errors, **When** import completes, **Then** SuccessCount = 95, ErrorCount = 5, and ErrorsDetail contains 5 entries with row numbers and error messages.
- **Given** a row with a missing required field (e.g., product name), **When** validated, **Then** it is marked as error with message "Campo requerido: Name" and the row number.

---

#### REQ-047: Async Processing

**Description**: Files with more than 100 rows are processed asynchronously via Hangfire. The upload endpoint returns HTTP 202 Accepted with the import ID. The client can poll the import status. Files ≤ 100 rows are processed synchronously and return the final result immediately.

**Acceptance Criteria**:
- **Given** an upload with 150 rows, **When** the endpoint is called, **Then** HTTP 202 is returned with `{ importId, status: "processing" }` and a Hangfire job is enqueued.
- **Given** an upload with 50 rows, **When** the endpoint is called, **Then** processing happens synchronously and HTTP 200 is returned with the final BulkImportLog.
- **Given** an async import is in progress, **When** GET `/api/v1/admin/importar/historial` is called, **Then** the import appears with status "processing" and updates to "completed" or "failed" when done.

---

#### REQ-048: Bulk Export

**Description**: GET `/api/v1/admin/exportar/{tipo}` generates and returns an Excel file with current data. Supported types: products (with variants), brands, categories. The export can accept query parameters for filtering.

**Acceptance Criteria**:
- **Given** an admin calls export for products, **When** the file is generated, **Then** it contains all active products with their variants as rows, with appropriate column headers.
- **Given** an admin calls export with filter `?brandId={id}`, **When** the file is generated, **Then** only products from that brand are included.
- **Given** the export completes, **When** the response is returned, **Then** it has Content-Type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` and Content-Disposition with a meaningful filename.

---

#### REQ-049: Template Download

**Description**: GET `/api/v1/admin/importar/plantilla/{tipo}` returns an empty Excel file with correct column headers and one example row demonstrating the expected format. This helps admins prepare import files correctly.

**Acceptance Criteria**:
- **Given** an admin requests the products template, **When** the file is downloaded, **Then** it contains headers matching the import schema (SKU, Name, Brand, Category, PrecioVenta, PrecioCosto, SizeValue, SizeUnit, etc.) and one example row.
- **Given** an admin requests the brands template, **When** downloaded, **Then** it contains brand-specific headers (Nombre, Descripcion, Categoria, Origen, etc.) with an example.
- **Given** the template is filled and re-uploaded as import, **When** processed, **Then** it passes validation (the template format matches what the importer expects).

---

## Phase 8: Observability & Hardening

Delivers distributed tracing, audit logging, rate limiting, security headers, CORS, and input validation hardening for production readiness.

---

#### REQ-050: OpenTelemetry Tracing

**Description**: Distributed tracing is configured for all HTTP requests, EF Core database queries, and outgoing HTTP calls (Mercado Pago, Banxico, S3). Traces include the correlation ID as a tag. Export is configurable (OTLP endpoint or console in dev).

**Acceptance Criteria**:
- **Given** a request hits an endpoint that queries the database and calls an external API, **When** the trace is inspected, **Then** it shows spans for: HTTP receive → DB query → External HTTP call, all linked by the same trace ID.
- **Given** correlation ID middleware runs before tracing, **When** a trace is recorded, **Then** the correlation ID appears as a span attribute/tag.
- **Given** OpenTelemetry is configured, **When** slow queries exceed a threshold, **Then** they appear as spans with duration visible for performance analysis.

---

#### REQ-051: Audit Log

**Description**: All write operations (Create, Update, Delete) on business entities are logged to the AuditLogs table. Each entry captures: UserId, Action, EntityType, EntityId, OldValues (jsonb), NewValues (jsonb), IpAddress, UserAgent, and CorrelationId. System actions have null UserId.

**Acceptance Criteria**:
- **Given** an admin updates a product's name from "A" to "B", **When** the update completes, **Then** an AuditLog entry is created with Action="Update", EntityType="Product", OldValues containing `{"Name":"A"}`, NewValues containing `{"Name":"B"}`.
- **Given** a user creates an order via checkout, **When** the order is persisted, **Then** an AuditLog entry records Action="Create", EntityType="Order", NewValues with the order data, and the user's IP and UserAgent.
- **Given** audit logs are queried, **When** filtering by EntityType="Product" and EntityId="{id}", **Then** the full change history of that product is returned chronologically.

---

#### REQ-052: Rate Limiting

**Description**: API rate limiting is enforced per client: 100 requests/minute for public (unauthenticated) endpoints by IP, 200 requests/minute for authenticated users by user ID, and 500 requests/minute for admin users. Webhook endpoints are exempt (rely on IP whitelist).

**Acceptance Criteria**:
- **Given** a public client sends 101 requests within one minute, **When** the 101st request arrives, **Then** HTTP 429 Too Many Requests is returned with `Retry-After` header.
- **Given** an authenticated user sends 200 requests within one minute, **When** the 201st arrives, **Then** HTTP 429 is returned.
- **Given** rate limit headers are configured, **When** any response is sent, **Then** `X-RateLimit-Remaining` and `X-RateLimit-Reset` headers are included.

---

#### REQ-053: Security Headers

**Description**: All responses include security headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Strict-Transport-Security: max-age=31536000; includeSubDomains`. These prevent common web vulnerabilities (MIME sniffing, clickjacking, protocol downgrade).

**Acceptance Criteria**:
- **Given** any API response is returned, **When** the response headers are inspected, **Then** all three security headers are present with their correct values.
- **Given** a browser attempts to frame the API in an iframe, **When** `X-Frame-Options: DENY` is set, **Then** the browser blocks the framing attempt.
- **Given** HSTS is configured, **When** a client connects via HTTP, **Then** they are redirected to HTTPS and subsequent requests use HTTPS for the max-age duration.

---

#### REQ-054: CORS Configuration

**Description**: CORS allows only the configured frontend origin(s). Allowed methods: GET, POST, PUT, DELETE, OPTIONS. Allowed headers include Authorization, Content-Type, and X-Correlation-ID. The X-Correlation-ID header is exposed to the client.

**Acceptance Criteria**:
- **Given** a request from the configured frontend origin, **When** a preflight OPTIONS request is made, **Then** the response includes `Access-Control-Allow-Origin` matching that origin.
- **Given** a request from an unauthorized origin, **When** it arrives, **Then** the CORS headers are not included and the browser blocks the response.
- **Given** CORS is configured with exposed headers, **When** a response is received by the frontend, **Then** JavaScript can read the `X-Correlation-ID` and `X-RateLimit-Remaining` headers.

---

#### REQ-055: Input Validation with FluentValidation

**Description**: All command DTOs are validated via FluentValidation in a MediatR pipeline behavior. Validation runs before the handler executes. Validation errors return HTTP 400 with field-level error details. Max string lengths are enforced. SQL injection is prevented via parameterized queries (EF Core default).

**Acceptance Criteria**:
- **Given** a CreateProductCommand with name exceeding 200 characters, **When** sent, **Then** HTTP 400 is returned with `{ "errors": { "name": ["Name must not exceed 200 characters"] } }`.
- **Given** a command with multiple invalid fields, **When** validated, **Then** all field errors are returned in a single response (not one at a time).
- **Given** any user input containing SQL injection patterns, **When** passed through EF Core queries, **Then** the input is parameterized and treated as literal data, never executed as SQL.

---

## Cross-Cutting Requirements

These requirements span all phases and must be maintained throughout the entire development lifecycle.

---

#### REQ-056: Consistent Error Responses

**Description**: All error responses across the API follow a consistent JSON shape: `{ "error": "ERROR_CODE", "message": "Human-readable message", "statusCode": 400 }`. Validation errors extend this with an `errors` object containing field-level details. No endpoint returns unstructured error responses.

**Acceptance Criteria**:
- **Given** any endpoint returns an error (4xx or 5xx), **When** the response body is inspected, **Then** it matches the standard error shape with error code, message, and status code.
- **Given** a validation failure, **When** the response is inspected, **Then** it includes the `errors` object with field names as keys and arrays of messages as values.
- **Given** an unauthorized request (401), **When** the response is returned, **Then** it follows the same shape: `{ "error": "UNAUTHORIZED", "message": "...", "statusCode": 401 }`.

---

#### REQ-057: Soft Delete Convention

**Description**: Business entities (Products, Brands, Categories, Users, Courses, Branches) use soft delete via the IsActive flag. Hard deletes are never performed on business data. Queries for public endpoints filter by `IsActive = true` by default. Admin endpoints can optionally include inactive records.

**Acceptance Criteria**:
- **Given** an admin deletes a product, **When** the operation completes, **Then** `IsActive` is set to false and the record remains in the database.
- **Given** a soft-deleted product, **When** the public product listing is fetched, **Then** the product does not appear.
- **Given** a soft-deleted product, **When** an admin lists products with `?includeInactive=true`, **Then** the product appears with its IsActive status visible.

---

#### REQ-058: Pagination Convention

**Description**: All list endpoints follow the same pagination convention: query parameters `page` (1-based, default 1) and `limit` (default 20, max 100). Response includes `{ data: [...], total, page, totalPages }`. Invalid pagination parameters are coerced to defaults.

**Acceptance Criteria**:
- **Given** no pagination parameters, **When** a list endpoint is called, **Then** page=1 and limit=20 are applied by default.
- **Given** `limit=200` is requested, **When** the endpoint processes it, **Then** limit is capped at 100.
- **Given** `page=0` or a negative number, **When** processed, **Then** page is coerced to 1.

---

## Traceability Matrix

| Phase | Requirements | Count |
|-------|-------------|-------|
| Phase 1: Foundation | REQ-001 to REQ-010 | 10 |
| Phase 2: Identity & Access | REQ-011 to REQ-019 | 9 |
| Phase 3: Catalog | REQ-020 to REQ-028 | 9 |
| Phase 4: Commerce | REQ-029 to REQ-037 | 9 |
| Phase 5: Content & Business | REQ-038 to REQ-040 | 3 |
| Phase 6: Intelligence | REQ-041 to REQ-044 | 4 |
| Phase 7: Admin Operations | REQ-045 to REQ-049 | 5 |
| Phase 8: Observability & Hardening | REQ-050 to REQ-055 | 6 |
| Cross-Cutting | REQ-056 to REQ-058 | 3 |
| **Total** | | **58** |

---

## Dependencies Between Phases

```
Phase 1 (Foundation) ──→ All other phases depend on this
Phase 2 (Identity)   ──→ Phase 4 (Commerce), Phase 7 (Admin Operations)
Phase 3 (Catalog)    ──→ Phase 4 (Commerce), Phase 6 (Intelligence), Phase 7 (Admin Operations)
Phase 4 (Commerce)   ──→ Phase 8 (Observability) for audit logging
Phase 5 (Content)    ──→ Independent after Phase 1
Phase 6 (Intelligence) ──→ Depends on Phase 3 (product data)
Phase 7 (Admin Ops)  ──→ Depends on Phase 2 (auth) + Phase 3 (catalog entities)
Phase 8 (Hardening)  ──→ Applies after all phases; can be incremental
```
