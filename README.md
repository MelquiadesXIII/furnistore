# Furnistore

Tienda en línea de muebles: catálogo público, carrito, checkout, seguimiento de
pedidos y panel de administración con reportes exportables a PDF y CSV.
Los precios están en USD y las fechas del negocio se calculan en hora de La Habana.

Monorepo gestionado con [Turborepo](https://turbo.build) y pnpm.

## Estructura

```
apps/
  api/   ASP.NET Core Web API — .NET 10, EF Core, Identity + JWT, PostgreSQL   (@furnistore/api)
  web/   Next.js 15 — App Router, React 19, TypeScript, Tailwind, shadcn/ui    (@furnistore/web)
assets/  imágenes de productos que sube el panel y que la API sirve en /assets
docs/    documentación final en PDF
docker-compose.yml   PostgreSQL 17 local
```

## Documentación

| Documento | Contenido |
|-----------|-----------|
| [`docs/guia-api-furnistore.pdf`](docs/guia-api-furnistore.pdf) | La API por dentro: arquitectura, modelo de datos con diagramas, EF Core, migraciones, transacciones y concurrencia, seguridad, reglas del dominio, pruebas y contrato OpenAPI |
| [`docs/guia-web-furnistore.pdf`](docs/guia-web-furnistore.pdf) | La web por dentro: App Router, sesión, árbol de componentes, cada pantalla de la tienda y del panel, reportes y exportación a PDF/CSV |

## Requisitos

| Herramienta | Versión | Necesaria para |
|-------------|---------|----------------|
| .NET SDK    | 10.0.x (ver `apps/api/global.json`) | `apps/api` |
| Node.js     | 20+ (ver `.node-version`)           | `apps/web`, Turborepo |
| pnpm        | 9 (`corepack enable`)               | workspace |
| Docker      | con Compose                         | base de datos y pruebas de integración |

## Puesta en marcha

1. Levantar la base de datos (PostgreSQL en `127.0.0.1:5433`):

   ```bash
   docker compose up -d
   ```

2. Variables de entorno:
   - `apps/api/.env` a partir de `apps/api/.env.example`: `DATABASE_URL`, `JWT_SECRET`,
     `JWT_ISSUER`, `JWT_AUDIENCE` y las credenciales SMTP para enviar los códigos de
     verificación por correo.
   - `apps/web/.env.local` a partir de `apps/web/.env.example`: `API_BASE_URL`, la URL
     de la API (solo la usa el servidor de Next.js).

3. Aplicar las migraciones:

   ```bash
   cd apps/api
   dotnet tool restore
   dotnet ef database update --project API.Furnistore.Data --startup-project API.Furnistore.API
   ```

4. Arrancar todo desde la raíz:

   ```bash
   pnpm install
   pnpm dev
   ```

   La web queda en `http://localhost:3000` y la API en la URL que fije `ASPNETCORE_URLS`.
   En desarrollo, Swagger UI está en `/swagger` y el estado de la API en `/health`.

5. Crear un administrador: registrar la cuenta en la web y después promoverla:

   ```bash
   cd apps/api
   dotnet run --project API.Furnistore.API -- admin promote <correo>
   ```

   La persona tiene que cerrar sesión y volver a entrar para ver el panel en `/admin`.

## Comandos

Desde la raíz (orquestado por Turborepo):

```bash
pnpm dev                               # API + web a la vez
pnpm dev --filter=@furnistore/web      # solo web
pnpm dev --filter=@furnistore/api      # solo API (dotnet watch)
pnpm build                             # compila todo
pnpm test                              # pruebas de integración de la API
```

Las pruebas de integración (`apps/api/API.Furnistore.IntegrationTests`) levantan su
propio PostgreSQL con Testcontainers, así que necesitan Docker en marcha.

## Contrato entre la API y la web

La web no escribe a mano los tipos de la API: los genera del OpenAPI.

```bash
pnpm --filter @furnistore/api openapi   # regenera apps/api/openapi.json
pnpm --filter @furnistore/web gen:api   # regenera apps/web/src/lib/api/schema.d.ts
```

Si cambia un endpoint o un DTO, hay que regenerar los dos archivos y subirlos.
La CI (`.github/workflows/ci.yml`) falla si alguno no coincide con el código.
