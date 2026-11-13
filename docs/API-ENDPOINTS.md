# API Endpoints — LZ Detailing Store Backend

**Base URL**: `http://localhost:5297` (dev) | `https://api.lzdetailingstore.com` (prod)

---

## Authentication (Public)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/registro` | Register new user |
| POST | `/api/v1/auth/login` | Login with email/password |
| POST | `/api/v1/auth/refresh` | Refresh JWT token |
| GET | `/api/v1/auth/perfil` | Get authenticated user profile (JWT required) |

### `GET /api/v1/auth/perfil`

Returns the authenticated user's profile, role names and **effective permission slugs**.
`permisos` is the distinct set of slugs granted through all of the user's roles, sorted ascending;
it is an empty array when the user has no roles/permissions. **404** when the user no longer exists.

```json
{
  "id": "uuid",
  "nombre": "...",
  "email": "user@example.com",
  "telefono": "...",
  "roles": ["Operativo"],
  "permisos": ["ordenes.ver", "recomendaciones.gestionar"]
}
```

Login, registro and refresh return tokens only (`accessToken`, `refreshToken`, `expiresIn`); call `perfil`
after authenticating to obtain roles and permissions.

---

## Catalog (Public)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/productos` | List products (paginated, filterable) |
| GET | `/api/v1/productos/{slug}` | Product detail by slug |
| GET | `/api/v1/productos/{id}/recomendaciones` | Curated product recommendations (complementary + similar) |
| GET | `/api/v1/marcas` | List brands |
| GET | `/api/v1/categorias` | List categories |

**Query params for `/productos`**: `page`, `limit`, `brandId`, `categoryId`, `minPrice`, `maxPrice`, `search`, `sortBy` (price_asc, price_desc, name, newest)

### `GET /api/v1/productos/{id}/recomendaciones`

Returns the **approved** relations of a product, split by type. Only related products that are visible
(active, with at least one active presentation in stock) are returned, ordered by the admin-defined order.

- **Query params**: `count` (per type, default `6`, clamped to `1..6`)
- **404** when the product does not exist or is inactive.

```json
{
  "complementarios": [
    {
      "id": "uuid", "name": "...", "slug": "...", "description": "...",
      "precioBase": 199.00, "precioOriginal": null,
      "imageUrl": "...", "imageAlt": "...", "badge": null,
      "brandName": "...", "categoryName": "...",
      "variants": [ { "id": "uuid", "nombre": "500 ml", "precioVenta": 199.00, "disponible": true } ],
      "reason": "Una frase en español que explica por qué conviene este producto."
    }
  ],
  "similares": [ /* same shape */ ]
}
```

Stock quantities and cost are never exposed — variants only carry the `disponible` flag.

---

## Cart (JWT required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/carrito` | Get current cart |
| POST | `/api/v1/carrito/items` | Add item to cart |
| PUT | `/api/v1/carrito/items/{itemId}` | Update item quantity |
| DELETE | `/api/v1/carrito/items/{itemId}` | Remove item from cart |
| DELETE | `/api/v1/carrito` | Clear entire cart |
| POST | `/api/v1/carrito/merge` | Merge localStorage cart with server |

---

## Checkout & Orders (JWT required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/checkout` | Create order + MercadoPago preference |
| GET | `/api/v1/pedidos` | My orders (paginated) |
| GET | `/api/v1/pedidos/{id}` | Order detail |

---

## Shipping Addresses (JWT required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/direcciones` | List my addresses |
| POST | `/api/v1/direcciones` | Create address |
| PUT | `/api/v1/direcciones/{id}` | Update address |
| DELETE | `/api/v1/direcciones/{id}` | Delete address |

---

## Content (Public)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/cursos` | List active courses |
| GET | `/api/v1/cursos/{slug}` | Course detail with modules/topics |
| GET | `/api/v1/sucursales` | List active branches |
| GET | `/api/v1/tipo-cambio` | Get current USD/MXN exchange rate |

---

## Admin - Catalog (JWT + permissions)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| POST | `/api/v1/admin/marcas` | marcas.crear | Create brand |
| PUT | `/api/v1/admin/marcas/{id}` | marcas.editar | Update brand |
| DELETE | `/api/v1/admin/marcas/{id}` | marcas.eliminar | Delete brand |
| POST | `/api/v1/admin/categorias` | categorias.crear | Create category |
| PUT | `/api/v1/admin/categorias/{id}` | categorias.editar | Update category |
| DELETE | `/api/v1/admin/categorias/{id}` | categorias.eliminar | Delete category |
| POST | `/api/v1/admin/productos` | productos.crear | Create product |
| PUT | `/api/v1/admin/productos/{id}` | productos.editar | Update product |
| DELETE | `/api/v1/admin/productos/{id}` | productos.eliminar | Delete product |
| POST | `/api/v1/admin/productos/{id}/variantes` | productos.crear | Create variant |
| PUT | `/api/v1/admin/productos/variantes/{id}` | productos.editar | Update variant |
| DELETE | `/api/v1/admin/productos/variantes/{id}` | productos.eliminar | Delete variant |
| POST | `/api/v1/admin/productos/{id}/imagenes` | productos.editar | Upload image |
| DELETE | `/api/v1/admin/productos/imagenes/{id}` | productos.eliminar | Delete image |
| PUT | `/api/v1/admin/productos/{id}/specs` | productos.editar | Manage specs |
| PUT | `/api/v1/admin/productos/{id}/pasos` | productos.editar | Manage steps |
| PATCH | `/api/v1/admin/productos/{productId}/variantes/{variantId}/disponibilidad` | productos.disponibilidad | Mark variant available / sold out |

> Note: every endpoint in the main `/api/v1/admin/productos` group above also requires `productos.crear`
> (group-level policy). The `disponibilidad` endpoint is mapped outside that group and requires only
> `productos.disponibilidad` (granted to `SuperAdmin`, `Admin` and `Operativo`).

**`PATCH /{productId}/variantes/{variantId}/disponibilidad`** — body `{ "disponible": true | false }`.
Stock is an availability flag, not an inventory count: `true` sets the variant's stock to `99` (available),
`false` sets it to `0` (sold out / on request). Nothing else on the variant changes.
Response `200` (empty body, same as the other variant updates); `404` "Variante no encontrada." when the
variant does not exist or does not belong to the product.

---

## Admin - Recommendations (JWT + `recomendaciones.gestionar`)

AI-assisted product relations. Claude proposes relations as **drafts** (`borrador`); an admin approves,
rejects, edits or reorders them. Only approved relations reach the public endpoint.
The permission is seeded for `SuperAdmin` and `Admin` (not `Cliente`).

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/admin/recomendaciones/generar` | Enqueue AI generation (Hangfire fire-and-forget) → `202` |
| GET | `/api/v1/admin/recomendaciones` | Paginated list of relations |
| POST | `/api/v1/admin/recomendaciones` | Create a manual relation (approved immediately) → `201` |
| PATCH | `/api/v1/admin/recomendaciones/{id}` | Change status and/or reason |
| PUT | `/api/v1/admin/recomendaciones/producto/{productId}/orden` | Reorder a product's relations of one type |
| DELETE | `/api/v1/admin/recomendaciones/{id}` | Delete a relation |

**Values**: `tipo` = `complementario` \| `similar`; `status`/`estado` = `borrador` \| `aprobado` \| `rechazado`; `source` = `ia` \| `manual`.

**`POST /generar`** — body `{ "mode": "nuevos" | "completar" }`
- `nuevos`: products with no relations at all; also proposes the reverse direction (existing products that should list the new one).
- `completar`: products with fewer than 2 visible approved relations of a type.
- Response `202`: `{ "jobId": "123", "mode": "nuevos" }`. Returns **503** `AI_NOT_CONFIGURED` ("La generación de recomendaciones no está configurada.") when no Anthropic API key is configured.
- Rejected pairs are never proposed again; failed batches are skipped and logged (see the Hangfire dashboard in dev).

**`GET /`** — query `estado`, `productId`, `tipo`, `page` (default 1), `pageSize` (default 20, max 100). Items:
`{ id, productId, productName, relatedProductId, relatedProductName, tipo, reason, status, source, sortOrder, createdAt, reviewedAt, reviewedBy }`.

**`PATCH /{id}`** — body `{ "status"?: "aprobado" | "rechazado" | "borrador", "reason"?: "..." }` (at least one; `reason` max 300 chars, empty string clears it). Sets `reviewedAt`/`reviewedBy`. Returns the updated item.

**`PUT /producto/{productId}/orden`** — body `{ "tipo": "complementario", "ids": ["relationId", ...] }`. Listed ids get positions `0..n-1`; unlisted relations keep their relative order after them. `400 INVALID_RELATION_IDS` if any id does not belong to that product and type.

**`POST /`** — body `{ "productId", "relatedProductId", "tipo", "reason" }`. `409` if the pair already exists for that type (any status), `404` if a product does not exist.

---

## Admin - Orders (JWT + permissions)

Granted to `SuperAdmin`, `Admin` and `Operativo`.

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/api/v1/admin/pedidos` | ordenes.ver | All orders (paginated, filterable) |
| GET | `/api/v1/admin/pedidos/{id}` | ordenes.ver | Order detail (any customer) |
| PUT | `/api/v1/admin/pedidos/{id}/status` | ordenes.editar | Change order status |
| PUT | `/api/v1/admin/pedidos/{id}/tracking` | ordenes.editar | Assign tracking |

**Money visibility** — prices and totals additionally require `ordenes.montos` (`SuperAdmin` and `Admin` only).
Without it `incluyeMontos` is `false` and every money field is `null` (list: `total`; detail: `subtotal`,
`shippingCost`, `total`, `items[].unitPrice`, `items[].subtotal`). Status and tracking updates are unaffected.

**`GET /`** — query params (all optional):

- `page` (default `1`), `limit` (default `20`, clamped `1..100`). Ordered by `createdAt` desc.
- `status`: `Pendiente`, `Pagada`, `Enviada`, `Entregada`, `Cancelada`, `Rechazada`, `Reembolsada`.
- `desde`, `hasta` (`YYYY-MM-DD`, inclusive, Mexico City calendar days). `400 VALIDATION_ERROR` if `hasta < desde`.
- `search` (max 100 chars): case-insensitive partial match on order id (a leading `#` is ignored, e.g. `#3f2b8c1e`), customer name or email.

```json
{
  "incluyeMontos": true,
  "data": [
    {
      "id": "3f2b8c1e-5d4a-4f7b-9c2e-1a6d8e0b7f43", "status": "Pagada",
      "customerName": "Ana López", "customerEmail": "ana@correo.com",
      "total": 349.00, "totalItems": 3, "createdAt": "2026-11-10T16:00:00Z"
    }
  ],
  "total": 42, "page": 1, "totalPages": 3
}
```

`customerName`/`customerEmail` are `null` if the customer account no longer exists.

**`GET /{id}`** — `404` if the order does not exist. `allowedTransitions` lists the statuses `PUT /{id}/status` accepts next.

```json
{
  "incluyeMontos": true,
  "id": "3f2b8c1e-5d4a-4f7b-9c2e-1a6d8e0b7f43",
  "status": "Pagada",
  "allowedTransitions": ["Enviada", "Cancelada"],
  "customer": { "id": "…", "nombre": "Ana López", "email": "ana@correo.com", "telefono": "5512345678" },
  "shippingAddress": {
    "id": "…", "nombre": "Casa", "calle": "Av. Reforma", "numeroExterior": "10", "numeroInterior": null,
    "colonia": "Centro", "ciudad": "CDMX", "estado": "CDMX", "codigoPostal": "06000",
    "telefono": null, "referencia": null, "esPrincipal": true
  },
  "subtotal": 250.00, "shippingCost": 99.00, "total": 349.00,
  "trackingNumber": null, "trackingUrl": null, "notes": null, "paymentId": "123456789",
  "createdAt": "2026-11-10T16:00:00Z", "updatedAt": "2026-11-10T16:05:00Z",
  "items": [
    {
      "id": "…", "variantId": "…", "productName": "Cera Premium", "variantName": "500 ml", "sku": "CER-500",
      "imageUrl": null, "unitPrice": 100.00, "quantity": 2, "subtotal": 200.00
    }
  ]
}
```

**`PUT /{id}/status`** — body `{ "status": 2 }` (numeric enum: `0` Pendiente, `1` Pagada, `2` Enviada, `3` Entregada,
`4` Cancelada, `5` Rechazada, `6` Reembolsada). Allowed admin transitions: `Pendiente → Cancelada`,
`Pagada → Enviada | Cancelada`, `Enviada → Entregada`; `Entregada`, `Cancelada`, `Rechazada` and `Reembolsada` are final.
`400 INVALID_TRANSITION` otherwise, `404` if the order does not exist. Empty `200` on success.
Admins can never set `Pagada`: payments are confirmed only by the Mercado Pago webhook (`POST /api/v1/webhooks/mercadopago`),
so `status: 1` always returns `400 INVALID_TRANSITION` with message `"El pago solo se confirma desde Mercado Pago."`.
The detail's `allowedTransitions` comes from the same rules (a `Pendiente` order lists only `["Cancelada"]`).

**`PUT /{id}/tracking`** — body `{ "orderId", "trackingNumber", "trackingUrl" }` (`orderId` must match the route,
else `400 ID_MISMATCH`). Empty `200` on success.

---

## Admin - Dashboard (JWT + `dashboard.ver`)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/api/v1/admin/dashboard/resumen?dias=30` | dashboard.ver | KPIs, daily sales, top products, sales by category and brand, orders by status |

`dashboard.ver` is granted to `SuperAdmin`, `Admin` and `Operativo`. Money figures additionally require
`dashboard.ventas` (`SuperAdmin` and `Admin` only). Without it `incluyeVentas` is `false`, `ventasMes`,
`ticketPromedio`, `ventasPorDia[].ingresos`, `topProductos[].ingresos` and `ventasPorMarca[].ingresos` are `null`, and
`ventasPorCategoria` is `[]`; order and unit counts are still returned.

- `dias` (optional, default `30`, allowed `7..365`): `400 VALIDATION_ERROR` outside that range.
- **Paid orders** = `Pagada`, `Enviada`, `Entregada`. `Pendiente`, `Rechazada`, `Cancelada` and `Reembolsada` never count as sales.
- Day/month boundaries use Mexico City time (`America/Mexico_City`).
- `kpis.ventasMes`: total of paid orders created this calendar month. `pedidosMes`: orders created this month except `Cancelada` and `Rechazada` (rejected payment).
  `ticketPromedio`: `ventasMes / paid orders this month` (2 decimals, `0` when none). `productosAgotados`: active products with no active variant available.
- `ventasPorDia`: one entry per day of the last `dias` days including today, ascending, zero-filled, paid orders only.
- `topProductos`: top 10 by units sold in paid orders of the period; revenue = stored line subtotals (price at purchase time).
- `ventasPorCategoria`: paid orders of the period, descending by revenue, only categories with sales.
- `ventasPorMarca`: paid orders of the period grouped by product brand (stored line quantities/subtotals), only brands with sales. Always returned: ordered by `ingresos` desc when `incluyeVentas`, otherwise by `unidades` desc with `ingresos: null`.
- `pedidosPorEstado`: all orders (no date filter), every status in enum order including zero counts.

```json
{
  "incluyeVentas": true,
  "kpis": { "ventasMes": 18450.00, "pedidosMes": 42, "ticketPromedio": 512.50, "productosAgotados": 3 },
  "ventasPorDia": [
    { "fecha": "2026-11-11", "ingresos": 0, "pedidos": 0 },
    { "fecha": "2026-11-12", "ingresos": 1250.00, "pedidos": 3 }
  ],
  "topProductos": [
    { "productoId": "3f2b8c1e-5d4a-4f7b-9c2e-1a6d8e0b7f43", "nombre": "Cera Premium", "unidades": 12, "ingresos": 4200.00 }
  ],
  "ventasPorCategoria": [ { "categoria": "Ceras", "ingresos": 9800.00 } ],
  "ventasPorMarca": [ { "marca": "Meguiar's", "unidades": 25, "ingresos": 7600.00 } ],
  "pedidosPorEstado": [
    { "estado": "Pendiente", "cantidad": 4 }, { "estado": "Pagada", "cantidad": 10 },
    { "estado": "Enviada", "cantidad": 6 }, { "estado": "Entregada", "cantidad": 30 },
    { "estado": "Cancelada", "cantidad": 2 }, { "estado": "Rechazada", "cantidad": 0 },
    { "estado": "Reembolsada", "cantidad": 0 }
  ]
}
```

---

## Admin - Content (JWT + permissions)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| POST | `/api/v1/admin/cursos` | cursos.crear | Create course |
| PUT | `/api/v1/admin/cursos/{id}` | cursos.editar | Update course |
| DELETE | `/api/v1/admin/cursos/{id}` | cursos.eliminar | Delete course |
| POST | `/api/v1/admin/sucursales` | sucursales.crear | Create branch |
| PUT | `/api/v1/admin/sucursales/{id}` | sucursales.editar | Update branch |
| DELETE | `/api/v1/admin/sucursales/{id}` | sucursales.eliminar | Delete branch |

---

## Admin - Import/Export (JWT + permissions)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| POST | `/api/v1/admin/importar/{tipo}` | productos.crear | Upload Excel (brands/categories/products) |
| GET | `/api/v1/admin/importar/historial` | productos.crear | Import history |
| GET | `/api/v1/admin/importar/{id}/errores` | productos.crear | Import errors |
| GET | `/api/v1/admin/importar/plantilla/{tipo}` | productos.crear | Download template |
| GET | `/api/v1/admin/exportar/{tipo}` | productos.ver | Export to Excel |

---

## Webhooks (No auth — signature validated)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/webhooks/mercadopago` | MercadoPago IPN notification |

---

## Health Checks

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health/live` | Liveness check |
| GET | `/health/ready` | Readiness check (includes DB) |

---

## Notes for Frontend Team

- **Auth**: Send `Authorization: Bearer {accessToken}` on all protected endpoints
- **Errors**: Always return `{ error, message, statusCode }`
- **Pagination**: `{ data, total, page, totalPages }`
- **Dates**: ISO 8601 UTC
- **Cart merge**: On login, send localStorage items to `/carrito/merge`
- **Checkout**: Returns `preferenceUrl` → redirect user to MercadoPago
- **Rate Limits**: Public 100/min, Authenticated 200/min, Admin 500/min
- **CORS**: Allowed from configured origins (localhost:3000, localhost:4200 in dev)
