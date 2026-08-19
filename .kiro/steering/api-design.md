# API Design — Endpoint Reference

## Base URL

```
https://api.lzdetailing.com/v1
```

## Authentication Scheme

- Public endpoints: No authentication required
- Private endpoints: `Authorization: Bearer <JWT>`
- Admin endpoints: JWT + Role-based policy (Admin/SuperAdmin)
- Webhook endpoints: Signature verification (Mercado Pago)

## Endpoint Groups

### Public Endpoints (12 — no JWT)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/auth/login` | Login with credentials |
| POST | `/auth/login/oauth` | Login with OAuth provider |
| POST | `/auth/registro` | Create account |
| POST | `/auth/recuperar-password` | Password recovery email |
| POST | `/auth/refresh` | Renew JWT via refresh token |
| GET | `/productos` | Product catalog (filtered, paginated) |
| GET | `/productos/:id` | Product detail |
| GET | `/productos/:id/recomendaciones` | AI-powered recommendations |
| GET | `/marcas` | Brand listing |
| GET | `/cursos` | Course listing |
| GET | `/cursos/:id` | Course detail |
| GET | `/sucursales` | Branch listing |

### Private Endpoints (11 — JWT required)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/auth/perfil` | Get authenticated user profile |
| PUT | `/auth/perfil` | Update profile |
| GET | `/carrito` | Get active cart |
| POST | `/carrito/items` | Add item to cart |
| PUT | `/carrito/items/:itemId` | Update item quantity |
| DELETE | `/carrito/items/:itemId` | Remove item from cart |
| DELETE | `/carrito` | Clear entire cart |
| POST | `/carrito/merge` | Merge localStorage cart with server |
| POST | `/checkout` | Create Mercado Pago payment preference |
| GET | `/ordenes` | Order history (paginated) |
| GET | `/ordenes/:id` | Order detail |

### Admin Endpoints (JWT + Admin role)

| Method | Route | Description |
|--------|-------|-------------|
| GET/POST | `/admin/productos` | List/Create products |
| GET/PUT/DELETE | `/admin/productos/:id` | Read/Update/Delete product |
| POST | `/admin/productos/:id/imagenes` | Upload product images |
| POST | `/admin/productos/:id/generar-embedding` | Regenerate AI embedding |
| GET/POST/PUT/DELETE | `/admin/productos/:id/variantes` | Manage product variants |
| GET/POST | `/admin/categorias` | List/Create categories |
| GET/PUT/DELETE | `/admin/categorias/:id` | Read/Update/Delete category |
| GET/POST | `/admin/marcas` | List/Create brands |
| GET/PUT/DELETE | `/admin/marcas/:id` | Read/Update/Delete brand |
| PUT | `/admin/marcas/:id/spotlight` | Toggle brand spotlight |
| GET/POST | `/admin/cursos` | List/Create courses |
| GET/PUT/DELETE | `/admin/cursos/:id` | Read/Update/Delete course |
| POST/PUT/DELETE | `/admin/cursos/:id/modulos` | Manage course modules |
| GET/POST | `/admin/sucursales` | List/Create branches |
| GET/PUT/DELETE | `/admin/sucursales/:id` | Read/Update/Delete branch |
| GET | `/admin/ordenes` | All orders (filtered) |
| PUT | `/admin/ordenes/:id/estado` | Change order status |
| PUT | `/admin/ordenes/:id/tracking` | Assign tracking number |
| GET | `/admin/usuarios` | List users |
| PUT | `/admin/usuarios/:id/estado` | Activate/deactivate user |
| PUT | `/admin/usuarios/:id/roles` | Assign roles to user |
| GET/POST | `/admin/roles` | List/Create roles |
| GET/PUT/DELETE | `/admin/roles/:id` | Read/Update/Delete role |
| PUT | `/admin/roles/:id/permisos` | Assign permissions to role |
| GET/PUT | `/admin/configuracion/tipo-cambio` | Exchange rate config |
| GET | `/admin/dashboard/metricas` | Sales metrics/dashboard |
| POST | `/admin/importar/:tipo` | Bulk import (multipart Excel/CSV) |
| GET | `/admin/importar/plantilla/:tipo` | Download Excel template |
| GET | `/admin/importar/historial` | Import history (paginated) |
| GET | `/admin/importar/:id/errores` | Import error details |
| GET | `/admin/exportar/:tipo` | Export to Excel (products, brands, categories) |

### Backend-Only Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/checkout/webhook` | Mercado Pago IPN webhook |

### Utility Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/codigos-postales/:cp` | Mexican postal code lookup |

## Response Conventions

### Success Response (single item)
```json
{
  "id": "uuid",
  "field": "value"
}
```

### Success Response (paginated list)
```json
{
  "data": [...],
  "total": 100,
  "page": 1,
  "totalPages": 5
}
```

### Error Response
```json
{
  "error": "ERROR_CODE",
  "message": "Human-readable message in Spanish",
  "statusCode": 400
}
```

### Validation Error Response (400)
```json
{
  "error": "VALIDATION_ERROR",
  "message": "One or more fields are invalid",
  "statusCode": 400,
  "errors": {
    "email": ["Email is required", "Must be valid email format"],
    "password": ["Minimum 6 characters"]
  }
}
```

## Rate Limiting

- Public: 100 req/min per IP
- Authenticated: 200 req/min per user
- Admin: 500 req/min per user
- Webhook: No limit (IP whitelist)

## CORS

- Allowed origins: frontend domain(s)
- Allowed methods: GET, POST, PUT, DELETE, OPTIONS
- Allowed headers: Authorization, Content-Type, X-Correlation-ID
- Exposed headers: X-Correlation-ID, X-RateLimit-Remaining
