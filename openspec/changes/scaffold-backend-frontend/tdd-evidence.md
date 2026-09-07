# Evidencia de implementación — 2026-09-07

Scaffold implementado y verificado en Windows x64. Comandos .NET ejecutados con
el SDK local `.tools/dotnet/dotnet.exe`, `DOTNET_ROOT` apuntando a ese directorio
y `DOTNET_CLI_HOME` en `.tools/cli`. No hay servicios de datos ni secretos.

## Preflight

- SDK 10.0.400 descargado del índice oficial Microsoft; SHA-512 del ZIP comprobado
  contra la metadata antes de extraer. `dotnet --version` desde backend: 10.0.400.
- Node 24.20.0 y npm 11.19.0. Consulta npm: `next/latest` = 16.3.4 y todos los
  paquetes directos fijados disponibles. Engines/peers registrados localmente en
  `.tools/npm-metadata.json`. NuGet restauró las cuatro versiones exactas de tests.
- Fuente SDK: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- Fuentes paquetes: https://registry.npmjs.org/next/latest y los endpoints de
  paquete/versión de npm; https://api.nuget.org/v3-flatcontainer/ para NuGet.

## Ciclos TDD

Horas UTC del 2026-09-07; los intervalos indican la sesión de ejecución.

| Ciclo | Rojo observado | Cambio mínimo y verde |
| --- | --- | --- |
| B1, 13:00–13:05 | `dotnet test Marketplace.sln --configuration Release`: 1 fallo, esperado OK y recibido NotFound. Host operativo y ruta ausente. | Agregar `MapGet` de liveness; `dotnet test Marketplace.sln --no-restore --configuration Release -m:1 -nodeReuse:false`: 1 aprobada. |
| F1 éxito/timeout, 13:01 | `npm test`: fallan éxito HTTP, JSON con charset y señal abortada; helper inicial devuelve false. | Agregar fetch sin caché y AbortController; éxito pasa, faltan controles de error. |
| F1 errores, 13:02 | `npm test`: 16 fallos del helper al faltar validación de URL/contrato y captura de errores. Incluye HTTP 201 y cuerpo con propiedad extra; rechazo de timeout aún se propaga. | Validar URL, estado 200, media type y objeto exacto; capturar errores y limpiar timer en finally. `npm test`: 20/20 aprobadas, sin rechazos no manejados. |
| F2, 13:01–13:02 | `npm test`: 2 fallos de página, el helper recibió 0 llamadas en lugar de la URL centinela. | Pasar variable al helper y renderizar estado según booleano; 2/2 aprobadas dentro de la suite de 20. |

El primer intento Vitest dentro del sandbox falló por `spawn EPERM`, no por
comportamiento; no cuenta como rojo. Se repitió fuera del sandbox. Un intento
MSBuild sin límite de nodos quedó trabado; se cerraron sus procesos específicos
y se usó `-m:1`. Los errores de infraestructura no se cuentan como evidencia TDD.

## Verificación final

| Comprobación | Resultado |
| --- | --- |
| Restore NuGet | Correcto, siete proyectos. |
| Build Release de solución, `--no-restore -m:1 -nodeReuse:false` | Siete proyectos compilados, 0 errores y 0 advertencias. |
| `dotnet test Marketplace.sln --no-build --configuration Release -m:1 -nodeReuse:false` | 1 aprobada, 0 fallos. |
| Inspección de los siete `.csproj` | Seis proyectos productivos sin referencias; tests referencia únicamente API. |
| `npm ci --no-audit --no-fund` y `npm ls` | Instalación desde lockfile correcta, árbol directo sin peers inválidos. |
| `npm test` | 20 aprobadas. |
| `npm run lint` | Código de salida 0. |
| `npm run build` con URL A | Build y TypeScript correctos; `/` figura como ruta dinámica. |
| HTTP real y HTML de producción | API 200; página disponible; tras detener API, página no disponible. |

npm informó deprecación de ESLint 9.39.1 y un postinstall de `unrs-resolver`
sin aprobación en allowScripts. No impidieron install, lint, tests ni build;
se conservó el conjunto fijado. No se ejecutó auditoría de vulnerabilidades.

La prueba de página invoca y espera la función async aislada y renderiza su JSX;
no simula el protocolo RSC de Next. El servidor Next real fue verificado por HTTP
con el script integrado. No se afirma una prueba visual en navegador ni de `next dev`.

## Experimento build/start y reproducción

Ejecutar desde raíz `node App/tools/verify-connectivity.mjs` después de los builds
indicados en `App/README.md`. Último resultado: **2026-09-07T13:11:39.517Z**.

- Build: `NEXT_PUBLIC_API_URL=http://localhost:5080` (A, API real).
- Start: `NEXT_PUBLIC_API_URL=http://localhost:5081` (B, centinela).
- A registró dos `GET /health/live` con 200: comprobación directa y solicitud Next.
- B recibió **0 solicitudes**. La página mostró `API disponible`.
- Al detener A y recargar, mostró `API no disponible` aunque B seguía disponible.
- El script cerró API, Next y centinela. Log completo local: `.tools/smoke-result.json`.

Se confirma el binding de build para este artefacto Next 16.3.4 con `next start`.
No se extrapola a otros modos de despliegue.

## Revisión y alcance

No se inicializó Git ni se crearon PRs. Los cortes previstos separan estructura
backend y contrato HTTP, y toolchain frontend y conectividad. El lockfile se
mantiene íntegro como generado y se verificó con `npm ci`; no cabe en 400 líneas.
Una futura publicación debe declarar este artefacto generado al revisar los cortes.
No se agregaron endpoints de negocio, datos, CORS, Utility ni integraciones.

La estructura frontend se preparó mientras se descargaba el SDK, adelantando la
tarea independiente de toolchain. Se preservaron los ciclos rojo/verde de cada
comportamiento. La autorización de apply fue el mensaje del usuario «empieza».
