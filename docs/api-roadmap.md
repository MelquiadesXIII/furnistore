# API — Pendientes y huecos para un ecommerce

Análisis del estado de `apps/api` frente a lo que necesita una tienda online real: qué endpoints existen pero están mal resueltos, y cuáles faltan por exponer.

Complementa a [`api.md`](./api.md), que documenta lo que la API hace **hoy**, y a [`api-architecture.md`](./api-architecture.md), que explica **cómo está construida y por qué**. Este documento apunta a lo que **debería** hacer.

> **Actualizado: 2026-09-06**, tras el refactor de arquitectura (commits `035b2`…`7734a`). Los puntos marcados **✅ Resuelto** se comprobaron ejecutando peticiones reales contra la API. Los marcados *verificado* en el análisis original siguen reproduciéndose salvo que se indique lo contrario.

## Resumen del estado

| Bloque | Estado |
|---|---|
| §1 Bloqueantes | 2 de 4 resueltos. Quedan **roles `Admin`** y **`Client.UserId`** |
| §2 Endpoints a mejorar | 7 de 12 resueltos. Los 5 restantes dependen de §1.2 o §1.3 |
| §3 Endpoints que faltan | Sin cambios: sigue faltando todo salvo el middleware de errores |
| §4 Cambios de modelo | Sin cambios: ninguno aplicado |
| §6 Deuda del refactor | **Nuevo.** Lo que introdujo o dejó abierto el propio refactor |

---

## 1. Bloqueantes actuales

### 1.1 El registro devuelve 500 y aun así crea el usuario — ✅ Resuelto

`AuthService.RegisterAsync` envuelve el envío del correo en `TrySendVerificationEmailAsync`, que captura cualquier excepción, la registra como `EmailSendFailed` (EventId 2020) y devuelve `false`. El registro ya no falla porque el SMTP no responda:

```bash
curl -X POST http://localhost:5135/api/authentication/register \
  -H 'Content-Type: application/json' \
  -d '{"name":"Test","emailAddress":"probe@example.com","password":"Passw0rd123"}'
# → 200 {"emailSent":false}     (antes: 500 con el stack trace de MailKit)
```

La filtración de stack traces también se cerró: `GlobalExceptionHandler` devuelve `ProblemDetails` sin traza, y deja la excepción completa solo en el log del servidor.

**Sigue pendiente de §3.1:** no existe `ResendConfirmation`. Una cuenta cuyo correo nunca llegó sigue sin poder desbloquearse sola — solo que ahora el usuario recibe `{"emailSent": false}` y sabe que algo pasó, en vez de un 500 opaco.

### 1.2 Cualquier cliente registrado puede modificar el catálogo — ❌ Pendiente

Sin cambios. `Program.cs` sigue usando `AddDefaultIdentity<IdentityUser>()` sin `.AddRoles<IdentityRole>()`. `[Authorize]` sigue significando solo "trae un token válido".

El listado de rutas que ahora imprime el arranque lo hace visible en cada `pnpm dev`:

```
POST   /api/products                              JWT
DELETE /api/products/{id:int}                     JWT
```

Dice `JWT`, no `Admin`. **Es el agujero de seguridad más grave que queda en el backend.**

**Qué hace falta:** `AddRoles<IdentityRole>()`, seed del rol `Admin`, `[Authorize(Roles = "Admin")]` en toda mutación de catálogo, y un usuario administrador inicial. Cuando esté, la columna del listado de arranque debería pasar a decir `Admin` — verificación gratis en cada arranque.

### 1.3 `Client` no está conectado a `IdentityUser` — ❌ Pendiente

Sin cambios. Registrarse crea un `IdentityUser` y nunca un `Client`. No hay forma de saber qué cliente es el usuario del token.

El refactor lo dejó a medio camino de forma deliberada: los servicios ya **reciben** el `userId` del token (`OrdersController` pasa `User.UserId()` a `OrderService`), pero hoy solo se usa para trazabilidad en el log. El `ClientId` de una orden sigue viniendo del cuerpo de la petición.

Esto significa que la fontanería ya está puesta: cuando exista la FK, derivar el cliente del token es un cambio localizado en `OrderService`, no una reescritura.

**Qué hace falta:** FK `Client.UserId` → `AspNetUsers.Id`, y creación automática del `Client` al registrarse o en el primer checkout. **Sigue siendo la falla estructural de la que cuelga casi todo lo demás de este documento.**

### 1.4 El refresh token no funciona en el único caso para el que existe — ✅ Resuelto

Los tres problemas del análisis original están corregidos en `AuthService.RefreshAsync`:

- **El clon se usa.** `parameters = tokenValidationParameters.Clone()` con `ValidateLifetime = false` es el que se pasa a `ValidateToken`. Renovar con un JWT vencido —el caso de uso— ahora funciona.
- **Generador criptográfico.** `RandomGenerator` usa `RandomNumberGenerator.GetInt32` en lugar de `new Random()`, y el token pasó de 23 a 48 caracteres.
- **Vigencia estándar.** `JwtOptions.RefreshTokenLifetime` es de 30 días, no 6 meses.

Además, el flujo dejó de usar excepciones con comparación de strings: cada motivo de rechazo devuelve `Result.Fail` con su código (`auth.invalid_refresh_token`, `auth.refresh_token_expired`) y queda registrado como `RefreshTokenRejected` (EventId 2012) con la razón exacta.

Verificado: renovar funciona; reutilizar el mismo refresh token devuelve `401` (uso único).

---

## 2. Endpoints existentes que deben mejorar

| Endpoint | Problema | Estado |
|---|---|---|
| `GET /api/products` | Sin paginar, filtrar ni buscar | ✅ **Resuelto.** Acepta `page`, `pageSize`, `search`, `categoryId`, `minPrice`, `maxPrice` y responde `{ items, total, page, pageSize, totalPages }`. `apps/web` ya no pagina en memoria |
| `POST` / `PUT /api/products` | Entidad de EF como DTO; `OrderDetails` obligatorio; over-posting | ✅ **Resuelto.** DTOs propios por operación. Ninguna propiedad de navegación en el contrato |
| `PUT` y `DELETE` (todos) | Entidad completa en el body, sin id en la ruta | ✅ **Resuelto.** `PUT /api/products/{id}`, `DELETE /api/products/{id}` sin cuerpo |
| `POST` (todos) | `CreatedAtAction` mal armado, `Location` apuntaba a la colección | ✅ **Resuelto.** `Location: /api/products/17` |
| `GET /api/Products/GetByCategory/{id}` | Verbo en la ruta, segmento en PascalCase | ✅ **Resuelto de otra forma.** El endpoint se eliminó; ahora es `GET /api/products?categoryId={id}`, que además compone con el resto de filtros |
| `GET /api/Test` | Endpoint público que refleja input | ✅ **Eliminado** |
| `GET /api/Authentication/ConfirmEmail` | Devolvía un string plano | ⚠️ **Parcial.** Ahora devuelve `204` o `ProblemDetails`, consumible por una SPA. Sigue sin redirigir al frontend con el resultado |
| `GET /api/orders` | No filtra por dueño: cualquier autenticado lista las órdenes de todos | ❌ **Pendiente.** Requiere §1.3. Acepta `?clientId=` como filtro opcional, pero no lo impone |
| `GET /api/orders/{id}` | Sin verificación de propiedad (IDOR) | ❌ **Pendiente.** Requiere §1.3 |
| `POST /api/orders` | Confía en el `ClientId` del cuerpo; no calcula ni valida precios | ⚠️ **Parcial.** Ahora valida que el cliente y todos los productos existan, que haya al menos una línea, que no se repita `productId` y que `quantity >= 1`. Sigue confiando en el `ClientId` que manda el cliente, y sigue sin calcular precios |
| `PUT` / `DELETE /api/orders` | Una orden es un registro financiero: no se edita ni se borra | ❌ **Pendiente.** Ambos siguen existiendo. Requiere `Order.Status` (§4) para sustituirlos por cancelación |
| `GET /api/clients` | Expone PII de todos los clientes a cualquier autenticado | ❌ **Pendiente.** Requiere §1.2 para restringir a `Admin` |

---

## 3. Endpoints que faltan

Sin cambios respecto al análisis original, salvo donde se indica.

### 3.1 Cuenta y sesión

| Endpoint | Requisitos |
|---|---|
| `GET /api/authentication/me` | Devuelve el usuario del token: id, email, nombre, rol, `emailConfirmed`. Hoy `apps/web` decodifica el JWT a mano en `lib/session.ts` justamente porque esto no existe |
| `POST /api/authentication/logout` | Recibe el refresh token y lo marca `IsRevoked = true`. El campo existe en la tabla y **nada lo escribe nunca** |
| `POST /api/authentication/resend-confirmation` | Desbloquea el caso residual de §1.1. Rate limit obligatorio; responder siempre 200 sin revelar si el email existe |
| `POST /api/authentication/forgot-password` | Envía enlace de recuperación. Token de un solo uso, expiración corta, respuesta genérica |
| `POST /api/authentication/reset-password` | Valida el token y cambia la contraseña. Debe invalidar todos los refresh tokens activos del usuario |
| `POST /api/authentication/change-password` | Autenticado. Exige la contraseña actual |

### 3.2 Perfil del cliente

| Endpoint | Requisitos |
|---|---|
| `GET /api/clients/me` | Datos del cliente asociado al token. Requiere primero la FK del §1.3 |
| `PUT /api/clients/me` | Actualiza nombre, teléfono, fecha de nacimiento. Nunca permite cambiar el `UserId` |
| `GET /api/addresses` | Direcciones del cliente autenticado |
| `POST /api/addresses` | Alta: calle, ciudad, provincia, código postal, país, alias, `isDefault` |
| `PUT /api/addresses/{id}` · `DELETE /api/addresses/{id}` | Con verificación de propiedad. No permitir borrar una dirección referenciada por una orden histórica |

Un ecommerce necesita varias direcciones por cliente con una marcada por defecto; el campo suelto `Client.Address` (string) no alcanza.

### 3.3 Carrito — no existe absolutamente nada

| Endpoint | Requisitos |
|---|---|
| `GET /api/cart` | Carrito del usuario con líneas, subtotales y total **calculados en servidor** |
| `POST /api/cart/items` | `{ productId, quantity }`. Valida existencia, que esté activo y que haya stock. Si ya está, suma cantidad |
| `PUT /api/cart/items/{productId}` | Cambia cantidad; cantidad 0 elimina la línea |
| `DELETE /api/cart/items/{productId}` | Elimina una línea |
| `DELETE /api/cart` | Vacía el carrito |

Requisito transversal: **el precio nunca viaja desde el cliente**, siempre se lee de la base al calcular. El botón "Comprar" del frontend sigue deshabilitado porque este módulo no existe.

### 3.4 Checkout y órdenes

| Endpoint | Requisitos |
|---|---|
| `POST /api/orders/from-cart` | Convierte el carrito en orden dentro de una transacción: snapshot de precios unitarios, descuento de stock, dirección de envío copiada (no referenciada), estado inicial `Pending`. Falla completa si algún ítem quedó sin stock |
| `POST /api/orders/{id}/cancel` | Solo si el estado lo permite (`Pending`, `Paid`). Devuelve el stock reservado |
| `PATCH /api/orders/{id}/status` | Solo `Admin`. Transiciones válidas: `Pending → Paid → Shipped → Delivered`, y `Cancelled` desde los dos primeros |
| `GET /api/orders/{id}/invoice` | Comprobante. Debe reflejar los precios del momento de la compra, no los actuales |

> ⚠️ **Trampa conocida para quien implemente el checkout.** El refactor activó `EnableRetryOnFailure` en `AddDbContext` para tolerar fallos transitorios contra Supabase. Esa opción es **incompatible con `BeginTransaction`** salvo que la transacción se envuelva en una estrategia de ejecución:
>
> ```csharp
> await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
> {
>     await using var tx = await db.Database.BeginTransactionAsync(ct);
>     // ...
>     await tx.CommitAsync(ct);
> });
> ```
>
> Hoy no hay transacciones explícitas porque `SaveChangesAsync` gestiona la suya. El checkout será la primera que las necesite.

### 3.5 Pagos y envíos

| Endpoint | Requisitos |
|---|---|
| `POST /api/orders/{id}/payment-intent` | Crea la intención de pago contra la pasarela. El monto se calcula en servidor |
| `POST /api/payments/webhook` | Público, pero **con verificación de firma** e **idempotente**: la pasarela puede reenviar el mismo evento. Es lo que mueve la orden a `Paid` |
| `GET /api/shipping-methods` | Métodos disponibles con su costo base |
| `POST /api/shipping/quote` | Costo según dirección de destino y contenido del carrito |

### 3.6 Catálogo — contenido e inventario

| Endpoint | Requisitos |
|---|---|
| `POST /api/products/{id}/images` | Subida (multipart), validación de tipo y tamaño, orden de visualización. Hoy las fotos están hardcodeadas en `modules/products/list/product-images.ts`, no vienen de la API |
| `DELETE /api/products/{id}/images/{imageId}` | Solo `Admin` |
| `GET /api/products/{id}/stock` | Disponibilidad actual |
| `PATCH /api/products/{id}/stock` | Solo `Admin`. Ajuste de inventario con motivo |

### 3.7 Administración

| Endpoint | Requisitos |
|---|---|
| `GET /api/admin/orders` | Todas las órdenes con filtros por estado, rango de fechas y cliente. Paginado |
| `GET /api/admin/clients` | Listado de clientes, paginado. Sustituye al `GET /api/clients` actual |

Prerrequisito de toda esta sección: que exista el rol `Admin` (§1.2).

### 3.8 Transversal

| Tema | Estado |
|---|---|
| **Middleware global de errores** | ✅ **Resuelto.** `GlobalExceptionHandler` + `AddProblemDetails()`. Formato `ProblemDetails` consistente, sin stack traces, con `traceId` correlacionable con el log |
| **`GET /health`** | ❌ Pendiente. No existe ningún health check |
| **CORS** | ❌ Pendiente. Funciona solo porque Next actúa de proxy desde el servidor; cualquier llamada directa desde el navegador fallaría |
| **Rate limiting** | ❌ Pendiente. Login, registro y recuperación de contraseña siguen expuestos a fuerza bruta. .NET 8+ trae `AddRateLimiter` de serie |

---

## 4. Cambios de modelo de los que dependen esos endpoints

**Ninguno aplicado todavía.** Sin estos cambios de esquema, buena parte de lo anterior no se puede implementar.

| Cambio | Por qué |
|---|---|
| `Client.UserId` → FK a `AspNetUsers.Id` | Desbloquea `/me`, la propiedad de las órdenes y el checkout completo (§1.3) |
| `OrderDetail.UnitPrice` | **El más crítico.** Hoy el detalle solo guarda cantidad, así que si cambias el precio de un producto, el valor histórico de todas las órdenes pasadas cambia solo. Es corrupción silenciosa de datos contables |
| `Order.Status` | No hay ciclo de vida de la orden. Sin esto no se pueden sustituir el `PUT`/`DELETE` del §2 |
| `Order.Total`, `Order.Currency` | El total debe quedar congelado en la orden, no recalcularse desde precios vivos |
| Snapshot de dirección de envío en `Order` | La dirección del cliente puede cambiar después de la compra; el envío ya realizado no |
| `Product`: `Description`, `Stock`, `IsActive`, `Sku`, `Slug`, `ImageUrl`, `CreatedAt` | El modelo actual solo tiene `Id`, `Name`, `Price`, `ProductCategoryId` |
| Entidades nuevas: `Cart`, `CartItem`, `Address`, `ProductImage` | No existen |
| `IdentityRole` | Sin roles no hay separación cliente/administrador (§1.2) |

---

## 5. Orden sugerido

Revisado tras el refactor. Los cimientos de arquitectura ya están, así que el orden lo marca ahora qué desbloquea qué.

1. **`Client.UserId`** (§1.3). De esta FK cuelgan `/me`, la propiedad de órdenes y todo el checkout. Es el prerrequisito de más cosas que ningún otro punto.
2. **Rol `Admin`** (§1.2). Cierra el agujero de seguridad del catálogo y desbloquea toda la §3.7. Verificable de un vistazo en el listado de rutas del arranque.
3. **`OrderDetail.UnitPrice` y `Order.Status`** (§4). Antes de que haya órdenes reales en producción cuyo histórico se corrompa.
4. **Cuenta**: `/me`, `logout`, `resend-confirmation`, recuperación de contraseña (§3.1).
5. **Campos de `Product`** (`Stock`, `IsActive`, `Description`, `ImageUrl`) y luego el **carrito** (§3.3).
6. **Checkout** (§3.4) — recordando la trampa de la estrategia de ejecución.
7. **Pagos y envíos** (§3.5), **administración** e **imágenes** (§3.6, §3.7).

Transversales (§3.8) en cuanto haya una llamada directa desde el navegador (CORS) o un despliegue que necesite sondas (`/health`). El rate limiting, antes de abrir el registro al público.

---

## 6. Deuda introducida o dejada abierta por el refactor

Lo que no venía del análisis original, sino del propio trabajo de arquitectura. Está detallado en [`api-architecture.md`](./api-architecture.md) §5.

| Tema | Detalle |
|---|---|
| **Sin proyecto de tests** | Decisión explícita. La verificación es manual (32 casos ejecutados durante el refactor) y hay que repetirla entera en cada cambio. Es la deuda más cara del conjunto: los siete puntos del §5 se van a implementar sin red |
| **Dos viajes a la base por página** | La paginación exige `COUNT` + página. Contra Supabase en `us-east-1` son ~2s por petición en vez de ~1s. Mitigado con `DatabaseWarmupService` y `Minimum Pool Size=2`; **falta medir** el efecto real. Si no basta, la salida es paralelizar con `AddDbContextFactory` + `Task.WhenAll` |
| **Búsqueda sensible a acentos** | `?search=lampara` no encuentra «Lámpara». Requiere la extensión `unaccent` de Postgres y una migración. No es regresión —el filtro en memoria anterior se comportaba igual— pero es un defecto real en un catálogo en español |
| **`docs/api.md` quedó obsoleto** | Documenta las rutas viejas (`/api/Products`, `GetByCategory`), el shape de `AuthResult` y el `PUT`/`DELETE` con entidad en el cuerpo. **Nada de eso es cierto ya.** Hay que regenerarlo contra los controladores actuales |
| **Frontera `Application` porosa** | `UserManager` de Identity entra por vía transitiva desde `Data`, y el proyecto `API` sigue viendo `Data` porque el composition root necesita el tipo del `DbContext`. Lo sostiene la revisión de código, no el compilador |
