# Scaffold del marketplace local

Backend ASP.NET Core en seis proyectos y frontend Next.js. La página inicial
comprueba la API desde el servidor Next y muestra su disponibilidad. El backend
ya incluye el primer corte de negocio: registro, ingreso y sesión revocable del
responsable y alta/edición de negocios, persistidos en PostgreSQL/PostGIS. El
panel web permite completar el recorrido desde el navegador.

## Entorno

- .NET SDK **10.0.400**; `backend/global.json` impide usar otra versión.
- Node **24.20.0**, npm **11.19.0**; paquetes directos exactos y lockfile en frontend.
- En este workspace el SDK está en `.tools/dotnet`, fuera de `App/` e ignorado.
  En otra máquina instalar ese SDK desde Microsoft.

Para usar el SDK local, ejecutar desde la raíz del proyecto en cada terminal:

```powershell
$env:DOTNET_ROOT = (Resolve-Path .tools/dotnet).Path
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.tools/cli'
dotnet --version
```

## Backend

```powershell
cd App/backend
dotnet restore Marketplace.sln
dotnet build Marketplace.sln --no-restore --configuration Release -m:1
dotnet test Marketplace.sln --no-build --configuration Release -m:1
dotnet run --project src/Marketplace.Api --launch-profile http
```

El perfil HTTP seguro de ejemplo fija `http://localhost:5080`, sin credenciales.
`GET http://localhost:5080/health/live` responde HTTP 200 y `{"status":"ok"}`.
Esta señal indica liveness del proceso HTTP; no verifica servicios de datos.

Los proyectos productivos son Api, BLL, DAL, IoC, DTOs y Models. BLL declara los
contratos de persistencia y no referencia DAL ni IoC; DAL implementa esos contratos
con EF Core y Npgsql. No hay proyectos Utility o Integrations.

## Base de datos y autenticación

Preparar PostGIS y aplicar la migración de forma explícita:

```powershell
cd App
.\tools\setup-local.ps1
docker compose --env-file .env.local up -d database
$localPassword = (Get-Content .env.local) -replace '^POSTGRES_PASSWORD=', ''
$env:ConnectionStrings__Marketplace = "Host=127.0.0.1;Port=55432;Database=marketplace;Username=marketplace;Password=$localPassword"
dotnet run --project backend/src/Marketplace.Api -- --migrate
```

La API ofrece `POST /auth/register`, `POST /auth/login`, `GET /auth/session` y
`POST /auth/logout`. Las mutaciones exigen el origen configurado y el encabezado
`X-Requested-With: marketplace`. El token sólo viaja en una cookie HttpOnly y se
guarda como hash; cerrar sesión lo revoca en el servidor.

Las pruebas crean una base temporal y la eliminan al terminar. Para ejecutarlas,
establecer `TEST_DATABASE_CONNECTION` con la misma conexión pero base `postgres`.

## Frontend

En otra terminal, desde la raíz:

```powershell
cd App/frontend
Copy-Item .env.example .env.local
npm ci
npm test
npm run lint
npm run build
npm start
```

Abrir `http://localhost:3000`. Con API iniciada debe mostrar `API disponible`.
Detener la API y recargar: debe mostrar `API no disponible`. La consulta aborta
a los 3 segundos; solo acepta HTTP 200, JSON y el objeto exacto de liveness.
Para desarrollo usar `npm run dev` en lugar de build/start.

`NEXT_PUBLIC_API_URL` se usa para la señal de disponibilidad. `API_BASE_URL` es la
dirección privada que usa el proxy del panel y `APP_ORIGIN` fija el origen web
permitido. Deben establecerse antes de compilar; tras cambiarlas, reconstruir y
reiniciar producción, o reiniciar el servidor de desarrollo. Ninguna debe contener
credenciales. `.env.local` está ignorado y `.env.example` contiene valores locales.

El navegador recibe HTML; la consulta a la API la hace el servidor Next.
Por eso no se configura CORS en este slice.

Para reproducir la verificación integrada y el experimento de URL, compilar el
frontend con `NEXT_PUBLIC_API_URL=http://localhost:5080` y, desde la raíz, ejecutar
`node App/tools/verify-connectivity.mjs`. Requiere el SDK local y el build Release
del backend; usa los puertos libres 3000, 5080 y 5081, inicia con URL B (5081),
comprueba que se usa A (5080), detiene A y verifica el estado no disponible.
Cierra sus procesos y guarda el resultado en `.tools/smoke-result.json`.

Para verificar el onboarding completo con una base temporal, después del build
Release del backend y del build del frontend ejecutar desde la raíz:

```powershell
node App/tools/verify-onboarding.mjs
```

El script aplica migraciones, atraviesa el proxy, registra una cuenta, crea y
edita un negocio, revoca la sesión y elimina la base temporal al terminar.

## Evidencia y mantenimiento

El scaffold archivado está en
`../openspec/changes/archive/2026-09-08-scaffold-backend-frontend/`; el onboarding
y su evidencia están en
`../openspec/changes/archive/2026-09-08-business-owner-onboarding/`.
No se publica ni se contrata hosting desde este prototipo.
