# Scaffold del marketplace local

Backend ASP.NET Core en seis proyectos y frontend Next.js. La página inicial
comprueba la API desde el servidor Next y muestra su disponibilidad. No requiere
PostgreSQL/PostGIS ni implementa funcionalidades del marketplace.

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

Los proyectos productivos son Api, BLL, DAL, IoC, DTOs y Models. En este slice no
necesitan referencias entre sí; el proyecto de pruebas referencia Api para
probar el contrato HTTP. BLL nunca referencia DAL ni IoC. No hay Utility,
integraciones, persistencia ni capas de negocio implementadas todavía.

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

`NEXT_PUBLIC_API_URL` debe establecerse antes de compilar. Tras cambiarla,
reconstruir y reiniciar producción, o reiniciar el servidor de desarrollo.
No colocar secretos en esa variable. `.env.local` está ignorado; `.env.example`
contiene únicamente la dirección local de ejemplo.

El navegador recibe HTML; la consulta a la API la hace el servidor Next.
Por eso no se configura CORS en este slice.

Para reproducir la verificación integrada y el experimento de URL, compilar el
frontend con `NEXT_PUBLIC_API_URL=http://localhost:5080` y, desde la raíz, ejecutar
`node App/tools/verify-connectivity.mjs`. Requiere el SDK local y el build Release
del backend; usa los puertos libres 3000, 5080 y 5081, inicia con URL B (5081),
comprueba que se usa A (5080), detiene A y verifica el estado no disponible.
Cierra sus procesos y guarda el resultado en `.tools/smoke-result.json`.

## Evidencia y mantenimiento

Las tareas y resultados están en
`../openspec/changes/scaffold-backend-frontend/`. Conservar tests y documentación
en la misma unidad que el comportamiento. Los cuatro cortes de revisión son
estructura backend, liveness, toolchain frontend y conectividad.
No se inicializa Git ni se publica o contrata hosting desde este scaffold.
