# API — Pendientes y huecos para un ecommerce

Análisis del estado de `apps/api` frente a lo que necesita una tienda online real: qué endpoints existen pero tienen la forma equivocada, cuáles faltan, y qué validaciones dejan pasar cosas que no deberían.

Complementa a [`api.md`](./api.md), que documenta lo que la API hace **hoy**, y a [`api-architecture.md`](./api-architecture.md), que explica **cómo está construida y por qué**. Este documento apunta a lo que **debería** hacer.

> **Actualizado: 2026-09-25.** Desde la revisión anterior se mergeó la rama `Roles` (PR #5): rol `Admin`, seed de roles al arranque, claims de rol en el JWT, CORS, `/health` y rate limiting en Auth. Cada afirmación de este documento se verificó leyendo el código actual y, en los puntos críticos, arrancando la API y mirando el log real — no se repite nada de la versión anterior sin comprobarlo de nuevo.

---

## 0. Estado general

Hay progreso real: el hueco de autorización más ancho que existía — cualquier cuenta administraba el catálogo entero — está cerrado. `Program.cs` ya tiene `.AddRoles<IdentityRole>()`, siembra `Admin`/`User` al arrancar, y `IssueTokensAsync` mete cada rol del usuario como claim en el JWT. Verificado en vivo:

```
POST   /api/products                              Admin
POST   /api/clients                                Admin
POST   /api/product-categories                     Admin
```

Pero el problema de fondo no era "falta el rol Admin" — era, y sigue siendo, que **ningún endpoint sabe de quién es cada recurso**. Roles resuelve la mitad de eso (*"¿esta cuenta es administradora?"*); la otra mitad (*"¿esta orden, este cliente, son de quien hace la petición?"*) sigue sin existir, porque `Client` sigue sin FK a `IdentityUser`.

Y ahora hay una prueba más contundente que antes de que ese segundo hueco sigue abierto — está en el mismo recurso, contradiciéndose a sí mismo. Verificado arrancando la API:

```
GET    /api/orders                                Admin
GET    /api/orders/{id:int}                       Admin
POST   /api/orders                                JWT
PUT    /api/orders/{id:int}                        JWT
DELETE /api/orders/{id:int}                        JWT
```

**Leer una orden exige ser Admin. Crearla, editarla o borrarla no exige nada más que estar logueado.** Una cuenta `User` normal hoy no puede ver ni sus propias órdenes — 403 seguro, porque no existe ningún endpoint de lectura que no sea Admin-only — pero **sí puede crear, editar o borrar órdenes a nombre de cualquier otro cliente**, porque `OrderService.ValidateReferencesAsync` solo comprueba que el `clientId` del cuerpo exista, nunca que sea el suyo. Es el mismo endpoint siendo paranoico para leer y permisivo para escribir.

| Lo que ya funciona bien | El hueco estructural que queda |
|---|---|
| Arquitectura por capas, `Result<T>`, logging, manejo de errores | `Client` sigue sin FK a `IdentityUser` — cero endpoints pueden preguntar "¿esto es tuyo?" |
| Rol `Admin` real: catálogo y clientes ya lo exigen donde corresponde | `Orders` en escritura sigue sin dueño — y ahora en lectura excluye hasta al dueño legítimo |
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

### 1.2 `POST` / `PUT` / `DELETE /api/orders` como reemplazo/borrado total — ❌ Sigue exactamente igual

Una orden es un registro contable; no se "reemplaza" por otra con el mismo id, ni se borra. Este punto no se tocó en el PR de roles, y con la lectura ahora bloqueada, el contraste es más visible que antes:

```
POST   /api/orders                                JWT
PUT    /api/orders/{id:int}                        JWT
DELETE /api/orders/{id:int}                        JWT
```

Cualquier cuenta autenticada — rol `User`, sin ser `Admin` — puede crear una orden a nombre de cualquier `clientId` que exista, editar cualquier orden por id, o borrarla. Nada de esto pasa por un chequeo de propiedad.

**Reemplazo:** `POST /api/orders/{id}/cancel` (solo si el estado lo permite) y `PATCH /api/orders/{id}/status` (solo `Admin`) — ambos en §3.4, bloqueados hasta que exista `Order.Status` (§5). Y el `ClientId` de la creación debe salir del token, no del cuerpo — ver §4.

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
| `GET` / `PUT /api/clients/me` | El cliente del token propio. Con §1.1 resuelto, es la **única** vía que le queda a un cliente para ver o editar su propio perfil — hoy no existe ninguna. Requiere `Client.UserId` (§5) |
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
| `POST` / `PUT /api/orders` | **Crítico, sin cambios.** `ClientId` viene del body y solo se valida que exista — nunca que pertenezca al token. Cualquier `User` opera órdenes ajenas |
| `POST` / `PUT /api/orders` | No calcula ni congela precio (`OrderDetail` sin `UnitPrice`, §5) |
| `GET /api/orders` | **Nuevo, causado por este mismo PR.** Ahora exige `Admin` sin excepción — un `User` no tiene ninguna vía para ver sus propias órdenes, ni siquiera con el `?clientId=` que antes existía como filtro opcional. La lectura pasó de "demasiado abierta" a "cerrada también para el dueño legítimo" |
| Gestión de roles | No hay endpoint ni seed para el primer `Admin` — ver §3.7 |
| `GET /api/authentication/confirm-email` | Sin cambios: `200` con texto plano en éxito. Sigue pendiente la redirección — ver [`API_llama_al_frontend.md`](./API_llama_al_frontend.md) |
| Catálogo público (`GET /api/products`, `/api/product-categories`) | Sin rate limiting — el que llegó con este PR cubre solo `AuthenticationController` |

### Resuelto en este PR, ya no listar como pendiente

- ~~Mutaciones de catálogo sin rol~~ — `Products`, `ProductCategories`: `[Authorize(Roles = "Admin")]` verificado en las tres mutaciones de ambos controladores.
- ~~`Clients` expuesto a cualquiera~~ — clase completa en `Admin` (§1.1).
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
| `Client.UserId` → FK a `AspNetUsers.Id` | ❌ **El más bloqueante.** Desbloquea `/me`, propiedad real de órdenes, y que `ClientId` se derive del token |
| `OrderDetail.UnitPrice` | ❌ Sin esto, cambiar un precio corrompe en silencio el histórico contable |
| `Order.Status`, `Order.Total`, `Order.Currency` | ❌ Sin ciclo de vida no se pueden sustituir `PUT`/`DELETE` (§1.2) |
| Snapshot de dirección de envío en `Order` | ❌ |
| `Product`: `Description`, `Stock`, `IsActive`, `Sku`, `Slug`, `CreatedAt` | ❌ |
| Entidades nuevas: `Cart`, `CartItem`, `Address` | ❌ |

---

## 6. Orden sugerido

1. **`Client.UserId`** (§5). Sigue siendo el prerrequisito de más cosas que ningún otro cambio — ahora con evidencia más clara de por qué: es lo único que permite que `GET /api/orders` deje de excluir al dueño legítimo y que `POST`/`PUT`/`DELETE /api/orders` (§1.2, §4) dejen de operar a ciegas.
2. **Seed de un `Admin` inicial + endpoint para promover usuarios** (§3.7). Barato, y hoy bloquea probar el resto de la administración sin tocar SQL a mano.
3. **Arreglar `POST`/`PUT`/`DELETE /api/orders`** para exigir dueño — depende de (1). Es la exposición más concreta que queda abierta en todo el backend.
4. **`OrderDetail.UnitPrice` y `Order.Status`** (§5). Antes de que haya órdenes reales cuyo histórico se corrompa.
5. **Cuenta y direcciones** (§3.1, §3.2) — `/me` es más urgente que antes por §1.1.
6. **Campos de `Product`** y **categoría embebida** en el GET (§2).
7. **Carrito** (§3.3) → **Checkout** (§3.4) → **Pagos y envíos** (§3.5) → **Imágenes de administración** (§3.6).

Rate limiting en el catálogo público, en cuanto haya tráfico real que lo justifique — es independiente de todo lo anterior.

---

## 7. Deuda conocida, no bloqueante

| Tema | Detalle |
|---|---|
| **Sin proyecto de tests** | Sigue sin existir. Todo lo del PR de roles se verificó a mano, igual que todo lo anterior |
| **Dos viajes a la base por página** | Sin cambios, sigue sin medirse el efecto real |
| **Búsqueda sensible a acentos** | Sin cambios — §2 |
| **`docs/api.md`** | Revisar que siga describiendo los controladores actuales tras este PR |
| **Frontera `Application` porosa** | Sin cambios |
| **Rate limiting solo en Auth** | El catálogo público y los futuros endpoints de §3.1 no lo comparten todavía |
