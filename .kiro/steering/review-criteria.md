---
inclusion: manual
name: review-criteria
description: Criterios de revisión independiente de PRs para LzDetailingStore Backend, derivados del código real del proyecto (.NET 10, Clean Architecture + Vertical Slices + CQRS con MediatR).
---

# Criterios de Review — LzDetailingStore Backend

> Estos criterios NO son una plantilla genérica. Se derivan del código real de este
> repositorio (`qubumx/lzdetailingstorebackend`, rama base `main`). La fuente de verdad
> es el código: si el steering general contradice lo que hace el código, gana el código.

## Contexto verificado del proyecto

- **Arquitectura**: Clean Architecture con 5 proyectos — `Api → Application → Domain ← Infrastructure`, más `SharedKernel`.
- **Organización**: Vertical Slices por feature dentro de `Application/Features/{Modulo}/{Commands|Queries}/{CasoDeUso}/`.
- **CQRS**: MediatR 14.2.0. Cada caso de uso usa `IRequest<Result<T>>` + `IRequestHandler`.
- **Resultados**: patrón `Result` / `Result<T>` (`SharedKernel/Result.cs`). NO se usan excepciones para flujo de negocio.
- **Endpoints**: Minimal APIs vía `IEndpointGroup` + `app.MapGroup("/api/v1/...")`, auto-registrados por reflexión (`EndpointExtensions`).
- **Validación**: FluentValidation + `ValidationBehavior` (pipeline MediatR). Mensajes de error en español.
- **Acceso a datos REAL**: los handlers inyectan `DbContext` directamente (`dbContext.Set<T>()`). Persistencia vía `IUnitOfWork.SaveChangesAsync`.
- **Persistencia**: EF Core 10, PostgreSQL + pgvector, `UseSnakeCaseNamingConvention()`, cada entidad con `IEntityTypeConfiguration<T>` (Fluent API).
- **Auth**: JWT Bearer + autorización basada en permisos dinámicos con formato `recurso.accion` (`.RequireAuthorization("productos.crear")`).
- **Stack clave**: Mapster (no AutoMapper), Hangfire (jobs), Serilog, OpenTelemetry, Scalar, ClosedXML (import/export), Bcrypt (password hashing).

## Contradicciones conocidas steering vs código (resueltas a favor del código)

1. El steering menciona repositorios (`IProductRepository`); el código usa `DbContext` directo. **Regla real: `DbContext` directo.** Existe `Repository<T>` genérico registrado en DI pero está en desuso; introducir su uso es ir contra el patrón dominante.
2. El steering dice columnas en PascalCase; el código usa `UseSnakeCaseNamingConvention()`. **Regla real: snake_case en DB.**
3. Existe el proyecto `LzDetailingStore.ArchTests` pero no contiene reglas (solo un `Test1` placeholder). No asumir que hay reglas de arquitectura verificadas automáticamente.

## Niveles de severidad

- **BLOQUEANTE**: rompe compilación, corrección, seguridad, contrato de API, o viola una regla estructural obligatoria. El PR no se aprueba hasta resolverlo.
- **ADVERTENCIA**: no rompe la funcionalidad pero degrada calidad, consistencia o mantenibilidad. Se debería corregir; si no, justificarlo.
- **SUGERENCIA**: mejora opcional. No bloquea el merge.

---

## 1. Lógica de programación

- **BLOQUEANTE** — El caso feliz y los casos de error de un handler no coinciden con lo que espera el endpoint (ej. handler devuelve `Result<Guid>` pero el endpoint lo trata como `Result` sin valor).
- **BLOQUEANTE** — Operación de escritura sin llamada a `SaveChangesAsync` (cambios que nunca se persisten), o `SaveChangesAsync` sobre datos no modificados.
- **BLOQUEANTE** — Query que puede devolver `null` y se desreferencia sin verificar (`FirstOrDefaultAsync` seguido de uso directo sin chequeo → `Result.NotFound`).
- **BLOQUEANTE** — `CancellationToken` no propagado a llamadas async de EF/servicios cuando está disponible en la firma (`ct` recibido pero no pasado).
- **ADVERTENCIA** — Lógica de negocio duplicada entre handlers que podría vivir en el dominio o en un servicio compartido.
- **ADVERTENCIA** — Validaciones de negocio (existencia de FKs, unicidad de slug/sku) ausentes en el handler cuando el patrón del proyecto sí las hace (ej. `CreateProductCommandHandler` valida `brandExists`/`categoryExists`).
- **ADVERTENCIA** — Uso de `.Result`, `.Wait()` o `.GetAwaiter().GetResult()` en vez de `await`.
- **SUGERENCIA** — Consultas que traen entidades completas cuando solo se necesitan campos (proyectar a DTO / `Select`).

## 2. Arquitectura

- **BLOQUEANTE** — Violación de la regla de dependencias: `Domain` referenciando `Application`/`Infrastructure`; `Application` referenciando `Infrastructure`; lógica de EF/Npgsql fuera de `Infrastructure`.
- **BLOQUEANTE** — Endpoint que instancia handlers o accede a `DbContext` directamente en vez de enviar el request por `ISender`/MediatR.
- **BLOQUEANTE** — Caso de uso nuevo que no sigue la estructura de slice: falta `Command/Query`, `Handler` o `Validator` en su carpeta `Features/{Modulo}/{Commands|Queries}/{CasoDeUso}/`.
- **BLOQUEANTE** — Acceso a datos por un patrón distinto al dominante sin justificación: introducir `IRepository<T>` en handlers rompe la convención real (`DbContext` directo).
- **ADVERTENCIA** — Endpoint nuevo no expuesto vía `IEndpointGroup` (implementación ad-hoc en `Program.cs`).
- **ADVERTENCIA** — Lógica que debería ser un `IPipelineBehavior` (logging, validación, auditoría) implementada dentro de handlers individuales.
- **ADVERTENCIA** — DTO definido fuera de la carpeta `DTOs/` del feature, o entidad de dominio expuesta directamente en la respuesta HTTP.
- **SUGERENCIA** — Feature que crece lo suficiente para justificar submódulos, aún mezclado en una sola carpeta.

## 3. Semántica y contrato de API

- **BLOQUEANTE** — Handler que no devuelve el `StatusCode` correcto vía `Result` (ej. recurso inexistente devolviendo 400 en vez de `Result.NotFound` → 404).
- **BLOQUEANTE** — Endpoint que no usa `ToApiResponse()` / `ToCreatedResponse()` y arma la respuesta a mano rompiendo el shape estándar `{ error, message, statusCode }`.
- **BLOQUEANTE** — Ruta que no respeta `/api/v1/` o los nombres de recurso en español del contrato (`/productos`, `/marcas`, `/cursos`, `/sucursales`, `/ordenes`, `/carrito`).
- **BLOQUEANTE** — POST de creación que no devuelve 201 vía `ToCreatedResponse` con su URI, o PUT/DELETE que no devuelve el código acordado.
- **ADVERTENCIA** — Falta la validación `ID_MISMATCH` entre ruta y body en updates cuando el patrón del proyecto la exige (ver `AdminProductEndpoints`).
- **ADVERTENCIA** — Nombres de parámetros de query inconsistentes con lo ya usado (`page`, `limit`, `search`, `sortBy`).
- **SUGERENCIA** — Falta `.WithTags(...)` para agrupar en Scalar/OpenAPI.

## 4. Buenas prácticas y convenciones

- **BLOQUEANTE** — Nombre de archivo/clase que no sigue el patrón CQRS: `{Verbo}{Entidad}Command`, `Get{Entidad}Query`, `{Comando/Query}Handler`, `{Comando/Query}Validator`, `{Entidad}Dto`.
- **BLOQUEANTE** — Configuración de entidad EF ausente: nueva entidad sin su `IEntityTypeConfiguration<T>` con Fluent API (nada de Data Annotations en entidades).
- **ADVERTENCIA** — Handler no marcado `sealed`; no usar primary constructor cuando el resto del proyecto sí lo hace.
- **ADVERTENCIA** — Command/Query definido como `class` en vez de `record`.
- **ADVERTENCIA** — Inconsistencia en la persistencia: usar `dbContext.SaveChangesAsync` directo en un command cuando el patrón preferido es `IUnitOfWork.SaveChangesAsync` (existe esta inconsistencia hoy en algunos handlers de Identity; no propagarla).
- **ADVERTENCIA** — Mensajes de validación/error de cara al usuario en inglés (el proyecto los escribe en español).
- **ADVERTENCIA** — Campos requeridos sin `required`/validación, o propiedades mutables donde el patrón usa `init`/records.
- **SUGERENCIA** — Falta de `namespace` con file-scoped; `using` innecesarios; nombres que no respetan la tabla de naming (`_camelCase` privados, `PascalCase` públicos).

## 5. Seguridad

- **BLOQUEANTE** — Endpoint admin/privado sin `.RequireAuthorization(...)`, o con el permiso equivocado (ej. un DELETE con `productos.crear` en vez de `productos.eliminar`).
- **BLOQUEANTE** — Secreto, connection string, token de Mercado Pago, `Jwt:Secret` o credencial hardcodeada en código en vez de configuración.
- **BLOQUEANTE** — Password almacenado o comparado sin `IPasswordHasher` (Bcrypt); logging de contraseñas, tokens o PII sensible.
- **BLOQUEANTE** — Consulta construida por concatenación de strings susceptible a inyección (romper el uso de LINQ/consultas parametrizadas de EF).
- **BLOQUEANTE** — Filtración de detalles internos en respuestas de error fuera de Development (romper el patrón de `GlobalExceptionMiddleware`).
- **ADVERTENCIA** — Nuevo permiso `recurso.accion` usado en endpoints pero no sembrado/definido, dejando el endpoint inaccesible o abierto según el policy provider.
- **ADVERTENCIA** — Lectura de claim de identidad frágil: emitir el userId en un claim y leerlo por otro nombre sin garantizar el mapeo (ver el par `JwtService` `sub` ↔ `PermissionAuthorizationHandler` `NameIdentifier/sub`). Verificar consistencia del claim usado.
- **ADVERTENCIA** — Endpoint que recibe archivos sin validar tamaño/tipo (`UploadImage` valida `file.Length == 0`; extender a tipo/límites).
- **SUGERENCIA** — Falta de rate limiting explícito (`public`/`authenticated`/`admin`) en endpoints nuevos sensibles.

## 6. Alcance del PR

- **BLOQUEANTE** — El PR mezcla cambios no relacionados (ej. una feature de catálogo junto con refactor de auth) que impiden una revisión aislada.
- **BLOQUEANTE** — Cambios en migraciones EF que no corresponden a los cambios de entidad del PR, o migración faltante para un cambio de esquema.
- **ADVERTENCIA** — Archivos generados (`bin/`, `obj/`) o de entorno commiteados por error.
- **ADVERTENCIA** — Cambios de comportamiento sin la prueba correspondiente cuando el módulo ya tiene tests (Unit/Integration con xUnit + FluentAssertions + Testcontainers).
- **ADVERTENCIA** — PR grande que debería dividirse por feature/slice para revisión enfocada.
- **SUGERENCIA** — Falta de actualización de `docs/API-ENDPOINTS.md` cuando se agrega/cambia un endpoint público.

---

## Cómo clasificar un hallazgo

1. ¿Rompe compilación, corrección, seguridad o contrato de API? → **BLOQUEANTE**.
2. ¿Viola una regla estructural obligatoria (dependencias, MediatR, Result, slice, EF config)? → **BLOQUEANTE**.
3. ¿Degrada consistencia/mantenibilidad sin romper nada? → **ADVERTENCIA**.
4. ¿Es una mejora opcional? → **SUGERENCIA**.

Ante la duda entre BLOQUEANTE y ADVERTENCIA, revisa el impacto en runtime y en la seguridad. Si afecta al usuario final o a la integridad de datos, sube a BLOQUEANTE.
