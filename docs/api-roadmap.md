# API — Pendientes y huecos para un ecommerce

Análisis del estado de `apps/api` frente a lo que necesita una tienda online real: qué endpoints existen pero tienen la forma equivocada, cuáles faltan, y qué validaciones dejan pasar cosas que no deberían.

Complementa a [`api.md`](./api.md), que documenta lo que la API hace **hoy**, y a [`api-architecture.md`](./api-architecture.md), que explica **cómo está construida y por qué**. Este documento apunta a lo que **debería** hacer.

> **Actualizado: 2026-09-25.** Desde la revisión anterior se mergeó la rama `Roles` (PR #5) y se implementó la asociación `Client.UserId` con Identity: rol `Admin`, seed de roles al arranque, claims de rol en el JWT, CORS, `/health`, rate limiting en Auth y autorización por propietario en órdenes. La solución compila con `dotnet build apps/api/API.sln -v minimal` (0 advertencias, 0 errores). Este documento sigue describiendo el estado del roadmap completo, no un cierre de todo el negocio.

---

## 0. Estado general

Hay progreso real: el hueco de autorización más ancho que existía — cualquier cuenta administraba el catálogo entero — está cerrado. `Program.cs` ya tiene `.AddRoles<IdentityRole>()`, siembra `Admin`/`User` al arrancar, y `IssueTokensAsync` mete cada rol del usuario como claim en el JWT. Además, `Client.UserId` ya tiene FK e índice único hacia `AspNetUsers.Id`, y cada registro nuevo crea un cliente asociado. Verificado en código y compilación:

```
POST   /api/products                              Admin
POST   /api/clients                                Admin
POST   /api/product-categories                     Admin
```

Pero el problema de fondo no era "falta el rol Admin" — era que ningún endpoint sabía de quién era cada recurso. Esa parte ya está resuelta para clientes nuevos y órdenes: los servicios derivan el cliente desde el `UserId` del JWT para usuarios normales, mientras `Admin` mantiene acceso global. `Client.UserId` sigue siendo nullable de forma temporal para no romper clientes históricos que aún no se pueden mapear.

Y ahora hay una prueba más contundente que antes de que ese segundo hueco sigue abierto — está en el mismo recurso, contradiciéndose a sí mismo. Verificado arrancando la API:

```
GET    /api/orders                                Admin
GET    /api/orders/{id:int}                       Admin
POST   /api/orders                                JWT
PUT    /api/orders/{id:int}                        JWT
DELETE /api/orders/{id:int}                        JWT
```

**Este hueco de propiedad quedó corregido.** Una cuenta `User` puede leer sus órdenes y crear, editar o borrar únicamente las suyas; `Admin` puede operar sobre cualquier cliente. El `ClientId` enviado en el body no otorga propiedad: para usuarios normales se ignora o se sobrescribe con el cliente asociado al JWT.

| Lo que ya funciona bien | El hueco estructural que queda |
|---|---|
| Arquitectura por capas, `Result<T>`, logging, manejo de errores | Clientes históricos aún sin asociación; `Client.UserId` sigue nullable hasta completar el backfill |
| Rol `Admin` real: catálogo, clientes y acceso global a órdenes | Self-service, carrito, checkout, pagos y ciclo de vida de órdenes siguen pendientes |
| CORS, `/health`, rate limiting en Auth | No hay administrador inicial ni forma de promover una cuenta salvo SQL directo |
| Catálogo público paginado, filtrable, con imagen | Carrito, checkout, pagos: sigue sin existir ni un endpoint |

---

## 1. Endpoints probablemente obsoletos para este tipo de negocio

### 1.1 `Clients` como CRUD genérico — ✅ Resuelto en cuanto a exposición

Antes: cualquier autenticado listaba, editaba o borraba a cualquier cliente. Ya no — verificado en vivo:

```
GET    /api/clients                               Admin
POST   /api/clients                               Admin
PUT    /api/clients/{id:int}                       Admin
DELETE /api/clients/{id:int}                       Admin
```

`[Authorize(Roles = "Admin")]` está puesto a nivel de clase. El IDOR que describía este punto ya no existe.

Queda una consecuencia colateral, no un problema de seguridad: al cerrar esto para todos menos `Admin`, **tampoco un cliente puede ya gestionar su propio perfil** — antes al menos podía, por accidente, tocar su propio registro si conocía su id; ahora ningún `User` puede tocar ningún `Client`, ni el suyo. Cerrar el hueco de seguridad hizo más urgente, no menos, el endpoint `/me` de §3.1: hoy no hay ninguna vía de autoservicio.

### 1.2 `POST` / `PUT` / `DELETE /api/orders` como reemplazo/borrado total — ⚠️ Propiedad corregida; diseño contable pendiente

Una orden es un registro contable; no se debería "reemplazar" por otra con el mismo id ni borrar físicamente. La autorización por propietario ya está corregida, pero el diseño de ciclo de vida sigue pendiente:

```
POST   /api/orders                                JWT
PUT    /api/orders/{id:int}                        JWT
DELETE /api/orders/{id:int}                        JWT
```

Los usuarios normales ya no pueden operar órdenes ajenas. Sin embargo, los endpoints genéricos `PUT` y `DELETE` siguen existiendo y el contrato todavía acepta `ClientId`, por compatibilidad y transición; esto debe sustituirse por cancelación y cambios de estado.

**Reemplazo futuro:** `POST /api/orders/{id}/cancel` (solo si el estado lo permite) y `PATCH /api/orders/{id}/status` (solo `Admin`) — ambos en §3.4, bloqueados hasta que exista `Order.Status` (§5). El `ClientId` debe dejar de formar parte del contrato de usuario y derivarse siempre del token.

### 1.3 `PUT` de reemplazo total en Products / Categories

Sin cambios: sigue exigiendo el objeto completo para cambiar un solo campo. Menor, no urgente — candidato a `PATCH` el día que exista un panel de admin real.

---

## 2. Requisitos para los endpoints GET de productos

### Ya cumple

| Requisito | Estado |
|---|---|
| Paginación, búsqueda, filtro por categoría y rango de precio | ✅ |
| Imagen del producto (`imageUrl`) | ✅ — y ahora también **escribible** desde `POST`/`PUT /api/products`, con `[Url, StringLength(500)]` |
| Lectura pública, sin sesión | ✅ |

### Falta

| Requisito | Por qué importa |
|---|---|
| **Categoría embebida, no solo el id** | `ProductResponse.ProductCategoryId` sigue siendo un entero suelto. Sin cambios desde la última revisión |
| **Orden explícito** (`sortBy`, `sortDir`) | El orden sigue fijo (`Name`, luego `Id`) |
| **Caché HTTP** (`Cache-Control`, `ETag`) | Sigue sin ninguna cabecera de caché en el endpoint público de más tráfico |
| **Rate limiting en el catálogo** | El rate limiting que sí llegó cubre solo `AuthenticationController`. `GET /api/products` sigue anónimo y sin límite |
| **Búsqueda insensible a acentos** | `?search=lampara` sigue sin encontrar «Lámpara» — falta `unaccent` en Postgres |
| **Disponibilidad** (`Stock`/`IsActive`) | Sigue sin existir en `Product` |

---

## 3. Endpoints que faltan para cerrar el flujo de negocio

El botón "Comprar" en `apps/web` sigue deshabilitado — nada de esto cambió con el PR de roles, que fue puramente de autorización.

### 3.1 Cuenta y self-service (`/me`) — más urgente que antes

| Endpoint | Requisitos |
|---|---|
| `GET /api/authentication/me` | Usuario del token: id, email, nombre, **rol** (ya viaja en el JWT, solo falta exponerlo), `emailConfirmed` |
| `GET` / `PUT /api/clients/me` | El cliente del token propio. La relación `Client.UserId` ya existe, pero el endpoint de autoservicio todavía no está implementado; hoy ningún `User` puede ver o editar su perfil |
| `POST /api/authentication/logout` | Marca `IsRevoked = true` en el refresh token. Sigue sin escribirse nunca |
| `POST /api/authentication/resend-confirmation` | Ya hay infraestructura de rate limiting lista para reusar |
| `POST /api/authentication/forgot-password` / `reset-password` | Sin cambios, sigue faltando |

### 3.2 Direcciones — sin cambios, sigue faltando todo

### 3.3 Carrito — sin cambios, no existe absolutamente nada

Requisito transversal, no negociable: **el precio nunca viaja desde el cliente**.

### 3.4 Checkout y órdenes

| Endpoint | Requisitos |
|---|---|
| `POST /api/orders/from-cart` | `ClientId` **se deriva del token**, nunca del body — cierra §1.2/§4. Transacción con snapshot de precios, descuento de stock, estado inicial `Pending` |
| `POST /api/orders/{id}/cancel` | Solo si el estado lo permite |
| `PATCH /api/orders/{id}/status` | Solo `Admin` — ahora ya hay un rol real que lo puede exigir |
| `GET /api/orders/{id}/invoice` | Precios del momento de la compra |

> ⚠️ **Trampa conocida, sin cambios.** `EnableRetryOnFailure` es incompatible con `BeginTransaction` sin `CreateExecutionStrategy()`. El checkout será la primera transacción explícita del proyecto.

### 3.5 Pagos y envíos — sin cambios, sigue faltando todo

### 3.6 Catálogo — inventario e imágenes de administración

`ImageUrl` ya es escribible por API (novedad de este PR), pero sigue sin haber subida de archivo (multipart) ni soporte para más de una foto por producto.

### 3.7 Administración

| Endpoint | Requisitos |
|---|---|
| `GET /api/admin/orders` | Todas las órdenes, filtros, paginado |
| `GET /api/admin/clients` | Ya no urgente como reemplazo — `GET /api/clients` **ya es Admin-only** (§1.1). Sigue valiendo la pena como vista dedicada con mejores filtros |
| **Gestión de usuarios** *(nuevo, expuesto por el propio PR de roles)* | No hay administrador inicial, ni endpoint para promover una cuenta a `Admin`. Hoy es SQL directo contra `AspNetUserRoles`. Bloquea probar todo lo demás de esta sección sin acceso a la base |

---

## 4. Requisitos y validaciones que faltan en lo que ya existe

| Endpoint | Falta |
|---|---|
| `POST` / `PUT /api/orders` | **Contrato pendiente.** `ClientId` todavía se acepta en el body; para `User` se ignora o sobrescribe con el cliente del JWT, pero debe eliminarse del contrato público |
| `POST` / `PUT /api/orders` | No calcula ni congela precio (`OrderDetail` sin `UnitPrice`, §5) |
| `POST` / `PUT` / `DELETE /api/orders` | **Propiedad resuelta.** El servicio deriva el cliente desde el JWT para usuarios normales y verifica propiedad en lectura, actualización y borrado. El contrato aún acepta `ClientId`, aunque para `User` se ignora o sobrescribe; debe eliminarse en una migración de contrato |
| `GET /api/orders` | **Propiedad resuelta.** `User` consulta solo sus órdenes y `Admin` conserva el listado global. Sigue pendiente reemplazar el GET/PUT/DELETE genérico por un ciclo de vida de órdenes |
| Gestión de roles | No hay endpoint ni seed para el primer `Admin` — ver §3.7 |
| `GET /api/authentication/confirm-email` | Sin cambios: `200` con texto plano en éxito. Sigue pendiente la redirección — ver [`API_llama_al_frontend.md`](./API_llama_al_frontend.md) |
| Catálogo público (`GET /api/products`, `/api/product-categories`) | Sin rate limiting — el que llegó con este PR cubre solo `AuthenticationController` |

### Resuelto en este PR, ya no listar como pendiente

- ~~Mutaciones de catálogo sin rol~~ — `Products`, `ProductCategories`: `[Authorize(Roles = "Admin")]` verificado en las tres mutaciones de ambos controladores.
- ~~`Clients` expuesto a cualquiera~~ — clase completa en `Admin` (§1.1).
- ~~`Client` sin relación con Identity~~ — `Client.UserId` tiene FK e índice único hacia `AspNetUsers.Id`; el registro crea el cliente asociado y las órdenes aplican autorización por propietario.
- ~~Órdenes sin chequeo de propiedad~~ — `User` queda limitado a su cliente; `Admin` mantiene acceso global.
- ~~Sin CORS~~ — política `WebApp`, orígenes desde `CORS_ORIGINS`.
- ~~Sin `/health`~~ — `MapHealthChecks("/health")` con chequeo de `DbContext`.
- ~~Sin rate limiting~~ — parcial: cubre Auth completo (login, registro, refresh, confirmación), 5 intentos/minuto.
- ~~El listado de arranque no distingue roles~~ — **falso en la versión anterior de este documento.** Verificado en vivo: `AccessOf` ya lee `IAuthorizeData.Roles` de clase y de método, y el log de arranque imprime `Admin` donde corresponde, no `JWT`.

---

## 5. Cambios de modelo pendientes

| Cambio | Estado |
|---|---|
| `Product.ImageUrl` | ✅ Aplicado — y ya escribible por API |
| `IdentityRole` + roles `Admin`/`User` | ✅ Aplicado |
| `Client.UserId` → FK a `AspNetUsers.Id` | ✅ Aplicado mediante migración `AddClientUserOwnership`; nullable temporal para clientes históricos sin asociación |
| `OrderDetail.UnitPrice` | ❌ Sin esto, cambiar un precio corrompe en silencio el histórico contable |
| `Order.Status`, `Order.Total`, `Order.Currency` | ❌ Sin ciclo de vida no se pueden sustituir `PUT`/`DELETE` (§1.2) |
| Snapshot de dirección de envío en `Order` | ❌ |
| `Product`: `Description`, `Stock`, `IsActive`, `Sku`, `Slug`, `CreatedAt` | ❌ |
| Entidades nuevas: `Cart`, `CartItem`, `Address` | ❌ |

---

## 6. Orden sugerido

1. **Backfill de `Client.UserId` y endurecimiento de la FK** (§5). Asociar clientes históricos con evidencia válida y hacer `UserId` obligatorio cuando no queden huérfanos.
2. **Cuenta y self-service** (§3.1): `/api/authentication/me` y `/api/clients/me`.
3. **Seed de un `Admin` inicial + endpoint para promover usuarios** (§3.7). Sigue bloqueando probar administración sin SQL manual.
4. **Eliminar `ClientId` del contrato de usuario y cerrar el diseño genérico de órdenes** (§1.2, §4).
5. **`OrderDetail.UnitPrice` y `Order.Status`** (§5), antes de órdenes reales cuyo histórico se corrompa.
6. **Carrito** (§3.3) → **Checkout** (§3.4), derivando `ClientId` del token → **Pagos y envíos** (§3.5).
7. **Campos de `Product`**, categoría embebida e imágenes de administración (§2, §3.6).

Rate limiting en el catálogo público, en cuanto haya tráfico real que lo justifique — es independiente de todo lo anterior.

---

## 7. Deuda conocida, no bloqueante

| Tema | Detalle |
|---|---|
| **Clientes históricos sin asociación** | La migración añade `Client.UserId` nullable; hace falta un proceso de backfill verificable antes de volverlo obligatorio |
| **Datos de perfil provisionales** | El registro crea un cliente asociado con teléfono/dirección provisionales; `/api/clients/me` debe permitir completar esos datos |
| **Contrato de órdenes en transición** | `CreateOrderRequest` y `UpdateOrderRequest` aún aceptan `ClientId`; `User` no puede usarlo para cambiar propietario, pero debe eliminarse del contrato público |
| **Sin proyecto de tests** | Sigue sin existir. Todo lo del PR de roles se verificó a mano, igual que todo lo anterior |
| **Dos viajes a la base por página** | Sin cambios, sigue sin medirse el efecto real |
| **Búsqueda sensible a acentos** | Sin cambios — §2 |
| **`docs/api.md`** | Revisar que siga describiendo los controladores actuales tras este PR |
| **Frontera `Application` porosa** | Sin cambios |
| **Rate limiting solo en Auth** | El catálogo público y los futuros endpoints de §3.1 no lo comparten todavía |
