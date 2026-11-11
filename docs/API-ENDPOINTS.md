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

---

## Catalog (Public)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/productos` | List products (paginated, filterable) |
| GET | `/api/v1/productos/{slug}` | Product detail by slug |
| GET | `/api/v1/productos/{id}/recomendaciones` | AI-powered product recommendations |
| GET | `/api/v1/marcas` | List brands |
| GET | `/api/v1/categorias` | List categories |

**Query params for `/productos`**: `page`, `limit`, `brandId`, `categoryId`, `minPrice`, `maxPrice`, `search`, `sortBy` (price_asc, price_desc, name, newest)

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

---

## Admin - Orders (JWT + permissions)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/api/v1/admin/pedidos` | pedidos.ver | All orders (filterable by status) |
| PUT | `/api/v1/admin/pedidos/{id}/status` | pedidos.editar | Change order status |
| PUT | `/api/v1/admin/pedidos/{id}/tracking` | pedidos.editar | Assign tracking |

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
