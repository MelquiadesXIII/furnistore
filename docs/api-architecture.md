# API — Arquitectura y decisiones de diseño

Estado de `apps/api` tras el refactor, y la defensa razonada de cada decisión que lo compone.

Complementa a los otros dos documentos: [`api.md`](./api.md) describe **qué endpoints hay**, [`api-roadmap.md`](./api-roadmap.md) describe **qué falta**. Este describe **cómo está construido y por qué así**.

> Cada decisión se presenta con la alternativa que se descartó y con las condiciones que la invalidarían. Si alguna de esas condiciones se cumple, la decisión debe revisarse — no es dogma.

---

## 1. Estado actual

Cuatro proyectos, 24 endpoints en 5 controladores, cero controladores que toquen EF Core.

| Proyecto | Archivos | Líneas | Responsabilidad |
|---|---:|---:|---|
| `API.Furnistore.API` | 16 | 917 | Solo HTTP: enlazar, autorizar, traducir `Result` a status code, middleware |
| `API.Furnistore.Application` | 12 | 1576 | Reglas de negocio, contratos de entrada/salida, catálogo de eventos |
| `API.Furnistore.Data` | 4 | 95 | `DbContext` y configuraciones de entidad |
| `API.Furnistore.Shared` | 9 | 216 | Entidades de persistencia y primitivas (`Result`, `PagedResult`) |

*(Sin contar migraciones, que son código generado.)*

Reducción en los controladores, que es donde estaba concentrado el problema:

| Controlador | Antes | Ahora |
|---|---:|---:|
| `AuthenticationController` | 354 | 46 |
| `OrdersController` | 105 | 58 |
| `ProductsController` | 88 | 67 |
| `ProductCategoriesController` | 80 | 65 |
| `ClientsController` | 74 | 58 |
| `TestController` | 26 | eliminado |
| **Total** | **727** | **294** |

Volumen de lo que antes no existía: 47 reglas de `DataAnnotations`, 3 contratos con validación entre campos, 25 reglas de negocio que devuelven `Result.Fail`, 35 eventos de log catalogados, 43 llamadas a `ILogger`, 30 usos de `CancellationToken`, 8 de `AsNoTracking`.

---

## 2. Estructura y dependencias

```
API.Furnistore.API            →  Application, Data, Shared
API.Furnistore.Application    →  Data, Shared
API.Furnistore.Data           →  Shared
API.Furnistore.Shared         →  (ninguna)
```

El flujo de una petición:

```
HTTP
 └→ RequestLoggingMiddleware      cronómetro + scope (TraceId, UserId)
     └→ UseExceptionHandler        red de seguridad: excepción → ProblemDetails
         └→ Routing / AuthN / AuthZ
             └→ Controller         enlaza, autoriza, delega. No conoce EF
                 └→ Service        reglas de negocio, devuelve Result<T>
                     └→ DbContext  AsNoTracking, CancellationToken
                         └→ Postgres
```

El orden del middleware no es casual: **`RequestLogging` va por fuera de `ExceptionHandler`**. Si fuera al revés, una excepción llegaría al logger antes de convertirse en respuesta, y la línea registraría `200` para una petición que acabó en `500`. Colocado por fuera, el handler ya escribió el status cuando el logger lo lee. Está en `Program.cs:206-207`.

---

## 3. Decisiones de diseño

### 3.1 Capa de servicios, sin repositorios

**Decisión.** Tres capas: `Controller → Service → DbContext`. Los servicios reciben el `DbContext` directamente.

**Alternativa descartada.** Cuatro capas, con `IProductRepository` y compañía entre el servicio y EF.

**Por qué.** `DbContext` **ya es** un Unit of Work y cada `DbSet<T>` **ya es** un repositorio genérico. Envolverlos en otra interfaz es el patrón que en .NET se conoce como *repositorio sobre repositorio*: añade una capa sin comportamiento propio.

El coste concreto no es teórico. La abstracción obliga a elegir entre dos males:

- Devolver `IReadOnlyList<T>`, y entonces cada filtro, orden o paginación nueva exige un método nuevo en la interfaz. El catálogo con `page`, `pageSize`, `search`, `categoryId`, `minPrice` y `maxPrice` habría necesitado o un método por combinación o un objeto de criterios que reimplementa `IQueryable` a mano.
- Devolver `IQueryable<T>`, y entonces la abstracción es ficticia: el servicio sigue acoplado a EF, solo que ahora con una capa de indirección de por medio.

`ProductService.SearchAsync` compone seis filtros opcionales sobre un `IQueryable` y los resuelve en dos consultas SQL. Con repositorios eso, o es un método con seis parámetros, o deja de ser una abstracción.

**Qué me haría cambiar de opinión.** Que apareciera una segunda fuente de persistencia real (un caché distribuido, una API externa como origen de productos), o que hubiera que soportar dos motores de base de datos a la vez. Ninguna de las dos está en el roadmap.

---

### 3.2 `Application` como proyecto separado, no como carpeta

**Decisión.** Un proyecto .NET propio para la capa de aplicación.

**Alternativa descartada.** Una carpeta `/Services` dentro de `API.Furnistore.API`.

**Por qué.** Una carpeta es una convención; un proyecto es una restricción que el compilador verifica. Sin la separación, nada impide que alguien inyecte `IHttpContextAccessor` en un servicio y ate una regla de negocio al transporte HTTP. Ese acoplamiento no duele hasta que quieres ejecutar la misma regla desde un worker, un job programado o un test — y para entonces está repartido por veinte servicios.

El caso concreto ya apareció durante el refactor. `AuthService` necesita generar el enlace de confirmación de correo, que originalmente se construía con `Request.Scheme`, `Request.Host` y `Url.Action`. Como el proyecto no puede ver `HttpContext`, hubo que definir `IEmailConfirmationLinkBuilder` en `Application/Auth/AuthAbstractions.cs` e implementarlo en la capa API. Sin la frontera, esa dependencia se habría colado sin que nadie la notara.

**Honestidad sobre el alcance real de la frontera.** No es hermética, y conviene decirlo:

- `AuthService` sí usa `Microsoft.AspNetCore.Identity`, para `UserManager<IdentityUser>`. Llega de forma transitiva desde `Data`, que necesita `Microsoft.AspNetCore.Identity.EntityFrameworkCore` porque el contexto hereda de `IdentityDbContext`. Es un compromiso aceptado: Identity aquí funciona como almacén de usuarios, no como framework web.
- El proyecto `API` sigue referenciando a `Data`, porque el *composition root* tiene que ver el tipo del `DbContext` para registrarlo (`Program.cs`) y `DatabaseWarmupService` lo necesita para precalentar. Ningún controlador lo usa — verificable con `grep -l APIFurnistoreContext API.Furnistore.API/Controllers/*.cs`, que hoy no devuelve nada — pero eso lo sostiene la revisión de código, no el compilador.

Lo que la frontera **sí** garantiza es lo que más importa: ningún servicio puede ver `HttpContext`, `IActionResult`, `IUrlHelper` ni los status codes.

**Qué me haría cambiar de opinión.** Que el proyecto se quedara permanentemente en tres o cuatro servicios triviales. Con cinco módulos y un carrito, un checkout y pagos por delante, no es el caso.

---

### 3.3 DTOs propios, nunca entidades de EF en el contrato

**Decisión.** Cada operación tiene su `Request` y su `Response` en `Application/<Módulo>/<Módulo>Contracts.cs`.

**Alternativa descartada.** Seguir enlazando y devolviendo las entidades de `Shared` directamente.

**Por qué.** Con entidades, la tabla *es* el contrato público, con tres consecuencias que ya estaban ocurriendo:

1. **El esquema se publica sin decidirlo.** Añadir `CostPrice` a `Product` para contabilidad interna lo expone en el catálogo público el mismo día.
2. **Las propiedades de navegación contaminan.** `Product.OrderDetails` es `List<OrderDetail>` no anulable, así que el enlazado del modelo la exigía: crear un producto sin `orderDetails` devolvía `400 The OrderDetails field is required`. Un campo obligatorio que no tiene ningún sentido de negocio. Y de vuelta, el frontend recibía `orderDetails: null` en cada producto.
3. **Over-posting.** Quien llama puede enviar relaciones completas y modificar lo que no debía.

`ProductResponse` es un `record` de cuatro campos. Lo que no está ahí, no sale.

**Qué me haría cambiar de opinión.** Nada dentro de este proyecto. Es la decisión menos discutible del documento.

---

### 3.4 `Result<T>` en vez de excepciones para fallos esperados

**Decisión.** Los servicios devuelven `Result<T>`; los fallos previsibles son valores, no excepciones. En `Shared/Common/Result.cs`.

**Alternativa descartada.** Excepciones de dominio (`ProductNotFoundException`) capturadas por un middleware.

**Por qué.** El código anterior es el mejor argumento. En `AuthenticationController`, un refresh token inválido se señalaba así:

```csharp
throw new Exception("Invalid Token");
// ...y 40 líneas más abajo:
catch (Exception e)
{
    var message = e.Message == "Invalid Token" || e.Message == "Expired Token"
        ? e.Message
        : "Internal Server Error";
}
```

El contrato de errores era una **comparación de strings**. Si alguien reescribe ese literal, un fallo esperado empieza a reportarse como *"Internal Server Error"*, la alerta se dispara, y no hay nada que el compilador pueda revisar. Además, la excepción se usaba para control de flujo normal: renovar un token vencido es el caso de uso, no una anomalía.

Con `Result<T>`, los modos de fallo son parte de la firma. `Error` lleva un `Code` estable (`product.category_not_found`) que el cliente puede consumir, y un `Type` que decide el status HTTP. El coste de rendimiento de lanzar excepciones en el camino normal desaparece de paso, aunque ese no es el motivo principal.

Hay simetría con el frontend, y no es casual: `apps/web/src/lib/result.ts` ya tenía el mismo tipo. La misma idea a ambos lados del cable.

**Qué me haría cambiar de opinión.** Que el equipo encontrara la propagación manual de `Result` demasiado ruidosa en cadenas largas de operaciones. Si eso pasa, la respuesta no es volver a excepciones sino añadir combinadores (`Bind`, `Map`), no cambiar el modelo.

---

### 3.5 `ProblemDetails` (RFC 7807), sin envoltorio propio

**Decisión.** Éxito devuelve el recurso directo; el error devuelve `ProblemDetails`, con el status code cargando el significado.

**Alternativa descartada.** Envolver todo en `{ success, data, errors }`, como es habitual en Express.

**Por qué.** Un envoltorio duplica información que ya está en el status code, complica el tipado de OpenAPI (cada endpoint pasa a devolver `Envelope<T>` en vez de `T`) y obliga a leer el cuerpo para saber si algo falló, cuando el protocolo ya lo dice.

Además, `ProblemDetails` es de primera clase en ASP.NET Core: `AddProblemDetails()`, `ValidationProblemDetails` automático desde `[ApiController]`, y soporte nativo en la generación de OpenAPI.

El argumento decisivo fue empírico: **el cliente HTTP del frontend ya lo parseaba sin saberlo**. `extractMessages()` en `apps/web/src/lib/api/client.ts` lee `{ errors, title }` — que es exactamente la forma de `ValidationProblemDetails` — y `kindForStatus()` ya mapea 401/404/4xx/5xx a los tipos de error de la app.

Antes había cuatro formatos conviviendo: `AuthResult` en autenticación, entidad cruda en productos, `NotFound()` sin cuerpo, y un string plano en `ConfirmEmail`. El cliente necesitaba cuatro estrategias para leer un fallo.

A `ProblemDetails` se le añaden dos extensiones propias: `traceId` (correlación con el log del servidor) y `code` (el código estable del `Error`).

**Qué me haría cambiar de opinión.** Que un consumidor que no controlamos exigiera un formato fijo. Hoy el único consumidor es `apps/web`.

---

### 3.6 Validación en tres niveles

**Decisión.** Cada tipo de regla tiene un único hogar.

| Nivel | Dónde | Cubre | Falla como |
|---|---|---|---|
| 1 · Forma | `DataAnnotations` en el DTO | requerido, longitud, rango, formato | `400` automático de `[ApiController]` |
| 2 · Entre campos | `IValidatableObject` en el mismo DTO | `deliveryDate >= orderDate`, `minPrice <= maxPrice` | igual que nivel 1 |
| 3 · Negocio | El servicio, contra la base | existencia de FK, unicidad, propiedad, estado | `Result.Fail` → 400/404/409 |

**Alternativa descartada.** FluentValidation para los niveles 1 y 2.

**Por qué esta división.** Un `Range(0.01, ...)` no necesita base de datos y debe rechazarse antes de tocarla. Que la categoría exista sí la necesita, y por tanto pertenece al servicio, donde además puede componerse con el resto de la operación en la misma unidad de trabajo.

**Por qué sin FluentValidation.** Fue decisión tuya y sale barata: los niveles 1 y 2 los cubre `DataAnnotations` sin fricción, y el nivel 3 iba al servicio de todas formas. Lo que se pierde es azúcar sintáctica — las reglas condicionales complejas quedan más verbosas dentro de `IValidatableObject`, como se ve en `ClientRules.ValidateBirthDate`.

**Qué me haría cambiar de opinión.** Que aparezcan reglas condicionales encadenadas (validar X solo si Y y Z). Ahí `IValidatableObject` se vuelve un `if` anidado y FluentValidation gana claramente.

---

### 3.7 Logging: catálogo de eventos y política de niveles

**Decisión.** `EventId` estable por evento en `Application/Common/ApiEvents.cs`, agrupados por rango: 1xxx catálogo, 2xxx autenticación, 3xxx órdenes, 5xxx infraestructura. Plantillas de mensaje, nunca interpolación.

**Alternativa descartada.** `LogInformation` con strings interpolados donde hiciera falta.

**Por qué el `EventId`.** Permite filtrar por código sin depender del texto del mensaje, que se reescribe, se traduce y se corrige. `eventId=2010` seguirá significando "login fallido" cuando el mensaje ya no diga lo mismo.

**Por qué plantillas.** `logger.LogInformation("Product {ProductId} created", id)` conserva `ProductId` como propiedad consultable. La versión interpolada `$"Product {id} created"` la funde en texto plano, y con ella se pierde la posibilidad de agregar o filtrar. Además cada mensaje interpolado es una cadena distinta, así que no hay forma de agrupar eventos del mismo tipo.

**La política de niveles**, que es la parte que más se descuida:

| Nivel | Significa | Ejemplo |
|---|---|---|
| `Debug` | detalle de desarrollo | SQL de EF |
| `Information` | algo de negocio ocurrió bien | producto creado, resumen de petición |
| `Warning` | fallo **esperado**: el sistema está sano, la petición no procede | credenciales inválidas, 404, validación rechazada |
| `Error` | fallo **inesperado**: hay algo que arreglar | excepción no controlada, SMTP caído |

La distinción entre `Warning` y `Error` es la que da valor al log. Un login con contraseña incorrecta es `Warning`: el sistema hizo exactamente lo que debía. Si eso fuera `Error`, el día que llegue una alerta real estará enterrada entre mil intentos de login normales. **`Error` significa «alguien tiene que mirar esto».**

**Qué nunca se registra.** Contraseñas, JWT completos, refresh tokens, la cadena de conexión, credenciales SMTP. Para PII se registra `UserId` en lugar del correo; cuando el correo es imprescindible para diagnosticar —el caso de `LoginFailed`, donde todavía no hay usuario— se enmascara: `d***@gmail.com`. Los query strings se redactan en el middleware: `confirm-email?userId=abc&code=***`, porque ese código es un token de un solo uso y los logs pueden acabar en un agregador de terceros.

**Correlación.** El `TraceIdentifier` de cada petición se propaga a tres sitios: el scope de todas las líneas de esa petición, el campo `traceId` del `ProblemDetails` que ve el cliente, y la respuesta. Cuando el frontend reporta un error, ese identificador lleva directo a las líneas exactas del log.

---

### 3.8 Paginación en servidor

**Decisión.** `GET /api/products` acepta `page`, `pageSize`, `search`, `categoryId`, `minPrice`, `maxPrice` y devuelve `{ items, total, page, pageSize, totalPages }`.

**Alternativa descartada.** Seguir devolviendo la tabla entera y paginar en el cliente, como hacía `product-container.tsx`.

**Por qué.** Paginar en memoria funciona con 15 productos y deja de funcionar con 15.000: se transfiere el catálogo completo en cada carga de página para mostrar 12 elementos. Es una bomba de relojería silenciosa, porque el síntoma solo aparece cuando el catálogo crece.

**El coste que introduce, y hay que decirlo.** La respuesta paginada exige dos consultas: el `COUNT` para calcular `totalPages` y la página en sí. Contra un Postgres local son dos viajes de 3 ms. Contra Supabase en `us-east-1` son dos viajes de ~1 s. **Esto es una regresión respecto al comportamiento anterior**, que hacía un solo viaje.

Mitigaciones aplicadas y disponibles:

- `DatabaseWarmupService` mueve al arranque los ~2,2 s de compilación del modelo EF que antes pagaba la primera visita.
- `Minimum Pool Size=2` mantiene conexiones calientes, evitando el handshake TLS tras periodos de inactividad.
- Si sigue sin bastar: lanzar `COUNT` y página **en paralelo** sobre dos conexiones (`AddDbContextFactory` + `Task.WhenAll`), lo que devuelve el tiempo de pared a un solo viaje. No implementado — conviene medir antes.

---

### 3.9 Rutas en minúscula, identificador en la ruta

**Decisión.** `/api/products`, `/api/product-categories`. `PUT` y `DELETE` llevan `{id:int}` en la ruta y `DELETE` no lleva cuerpo.

**Alternativa descartada.** Mantener `/api/Products` y el patrón anterior, donde `PUT` y `DELETE` recibían la entidad completa en el body sin id en la ruta.

**Por qué.** `DELETE` con cuerpo no es fiable: muchos clientes HTTP, proxies y CDNs lo descartan, y la semántica está indefinida en la especificación. Además, recibir la entidad completa para borrarla obligaba al cliente a tener el objeto entero cuando solo necesita el identificador.

Las minúsculas son convención REST y evitan la ambigüedad de mayúsculas entre sistemas de archivos y routers sensibles a caso.

**El coste.** Rompe el contrato con `apps/web`, que ya se actualizó en el mismo cambio.

---

### 3.10 `CancellationToken` y `AsNoTracking`

**Decisión.** Toda operación asíncrona acepta y propaga `CancellationToken`; toda consulta de solo lectura usa `AsNoTracking()`.

**Por qué el token.** Si el usuario cierra la pestaña, la petición se aborta pero la consulta sigue ocupando una conexión de Supabase hasta terminar. Con conexiones limitadas en la nube, eso es capacidad desperdiciada. Se observó en la práctica: cuando el frontend abortaba por timeout, Kestrel registraba `499` y la consulta seguía viva.

**Por qué `AsNoTracking`.** EF construye el grafo de seguimiento de cambios en cada consulta por defecto. Para un listado que solo se serializa a JSON, ese trabajo se descarta íntegro. Es el camino más caliente de la aplicación.

---

### 3.11 Precalentamiento y resiliencia de conexión

**Decisión.** `DatabaseWarmupService` (un `BackgroundService`) ejecuta una consulta trivial al arrancar. `EnableRetryOnFailure(3, 2s)` y `CommandTimeout(20)` en `AddDbContext`.

**Por qué el precalentamiento.** La primera consulta de un `DbContext` compila el modelo completo — con `IdentityDbContext` son unas diez entidades — y abre la primera conexión. Medido en local: **2210 ms**. Ese coste lo pagaba el primer visitante. Ahora lo paga el arranque, en segundo plano, sin bloquear a Kestrel.

**Por qué la resiliencia.** Una base local no tiene fallos transitorios de red; una base en la nube sí. `EnableRetryOnFailure` los reintenta en vez de propagarlos como error al usuario.

> **Advertencia para quien añada transacciones explícitas.** `EnableRetryOnFailure` es incompatible con `BeginTransaction` sin envolverlo en una estrategia de ejecución. Hoy no hay transacciones explícitas —`SaveChangesAsync` gestiona la suya—, pero el checkout del roadmap las necesitará. Habrá que usar `db.Database.CreateExecutionStrategy().ExecuteAsync(...)`.

---

### 3.12 Listado de endpoints al arrancar

**Decisión.** En `Development`, el arranque imprime las 24 rutas agrupadas por controlador, con una columna de acceso.

**Por qué.** Es diagnóstico barato: responde de un vistazo "¿por qué me da 404?" sin abrir Swagger. La columna `publico`/`JWT` añade algo que NestJS no da y que aquí importa: hace visible en cada arranque que las mutaciones del catálogo solo exigen `JWT`, no `Admin` — el agujero documentado en `api-roadmap.md` §1.2.

Solo en `Development`, para no ensuciar los logs JSON de producción con 24 líneas por arranque.

---

## 4. Anatomía de un módulo

`Products` es la referencia canónica. Todo módulo nuevo debería tener esta forma:

```
Application/Products/
  ProductContracts.cs     ProductQuery, CreateProductRequest,
                          UpdateProductRequest, ProductResponse
  ProductService.cs       SearchAsync, GetByIdAsync, CreateAsync,
                          UpdateAsync, DeleteAsync

API/Controllers/
  ProductsController.cs   5 acciones, ninguna con lógica
```

El controlador completo de una acción son dos líneas:

```csharp
[AllowAnonymous]
[HttpGet]
[ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
public async Task<IActionResult> Search(
    [FromQuery] ProductQuery query,
    CancellationToken cancellationToken
) => (await products.SearchAsync(query, cancellationToken)).ToActionResult(this);
```

Un servicio con una regla de negocio:

```csharp
if (!await db.ProductCategories.AnyAsync(c => c.Id == request.ProductCategoryId, ct))
{
    logger.LogWarning(ApiEvents.ProductCategoryMissing,
        "Rejected: product category {CategoryId} does not exist", request.ProductCategoryId);

    return Result.Fail<ProductResponse>(Error.Validation(
        "product.category_not_found",
        $"La categoría {request.ProductCategoryId} no existe."));
}
```

Esa comprobación es la que convertía un `500` por violación de clave foránea en un `400` con causa explicable.

### Cómo añadir un módulo nuevo

1. `Application/<Módulo>/<Módulo>Contracts.cs` — `Query`, `Create...Request`, `Update...Request`, `...Response`. Validación de forma con `DataAnnotations`; si hay reglas entre campos, `IValidatableObject`.
2. `Application/<Módulo>/<Módulo>Service.cs` — constructor primario con `(APIFurnistoreContext db, ILogger<T> logger)`. Métodos que devuelven `Result<T>` o `Result`. Lecturas con `AsNoTracking`. Todo con `CancellationToken`.
3. `Application/Common/ApiEvents.cs` — reservar un rango de `EventId` y declarar los eventos del módulo.
4. `API/Controllers/<Módulo>Controller.cs` — acciones de una línea que delegan y llaman a `ToActionResult(this)` o `ToNoContentResult(this)`.
5. `API/Extensions/ApplicationServiceCollectionExtensions.cs` — registrar el servicio con `AddScoped`.

**Regla de oro.** Si un controlador necesita un `if`, ese `if` pertenece al servicio.

---

## 5. Deuda conocida

Lo que este diseño **no** resuelve, para que nadie lo descubra por sorpresa.

| Deuda | Estado |
|---|---|
| **Sin tests** | Decisión explícita. La verificación es manual (32 casos ejecutados) y hay que repetirla entera en cada cambio. Es la deuda más cara del conjunto |
| **Dos viajes por página** | Descrito en §3.8. Mitigado con precalentamiento, no eliminado |
| **Búsqueda sensible a acentos** | `lampara` no encuentra «Lámpara». Requiere la extensión `unaccent` de Postgres y una migración |
| **Frontera `Application` porosa** | `UserManager` de Identity entra transitivamente; `API` sigue viendo `Data` por necesidad del composition root (§3.2) |
| **Sin rol `Admin`** | `api-roadmap.md` §1.2. Cualquier usuario registrado puede escribir en el catálogo. Visible en el listado de arranque |
| **`Client` sin FK a `IdentityUser`** | `api-roadmap.md` §1.3. De esto cuelga todo el checkout |
| **Sin CORS ni health check** | `api-roadmap.md` §3.8. Hoy funciona porque Next actúa de proxy desde el servidor |
| **Sin rate limiting** | Login y registro quedan expuestos a fuerza bruta |

### Código eliminado por el refactor

El refactor dejó huérfanos cinco tipos, ya borrados: `Shared/Auth/AuthResult.cs`, los tres DTOs de `Shared/DTOs/` y `API/Configuration/JwtConfig.cs`. Sus reemplazos viven en `Application/Auth/AuthContracts.cs` y `Application/Auth/JwtOptions`. La sección `JwtConfig` de `appsettings.json` se conserva: `Program.cs` la lee por clave de texto como respaldo si faltan las variables de entorno.

### Cambios hechos fuera del alcance del refactor

Tres correcciones que no eran parte del plan pero que habría sido irresponsable dejar en código que se estaba reescribiendo:

1. **`VerifyAndGenerateTokenAsync`** clonaba `TokenValidationParameters` con `ValidateLifetime = false` y después pasaba el original. El clon nunca se usaba, así que renovar un JWT vencido —el único caso para el que existe un refresh token— lanzaba excepción y devolvía "Internal Server Error".
2. **`RandomGenerator`** usaba `new Random()` para generar refresh tokens. No es criptográficamente seguro: un refresh token predecible es una vulnerabilidad real. Ahora usa `RandomNumberGenerator`.
3. **`DotEnv.Load`** sobreescribía las variables de entorno existentes, porque `overwriteExistingVars` es `true` por defecto. Eso significa que un `.env` olvidado en un contenedor gana sobre la configuración inyectada por el host. Ahora es `false`: el entorno real manda.
