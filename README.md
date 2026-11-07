# Cerca — Marketplace hiperlocal

Prototipo del Trabajo Final de Grado (Universidad Siglo 21) para comercios de Córdoba.
Los consumidores descubren negocios y productos cercanos mediante un **ranking hiperlocal
con equidad de exposición** (política versionada `equity-v1`), y los responsables
administran su negocio como un SaaS comercial: catálogo, inventario, solicitudes por
WhatsApp, ventas, promociones y métricas.

Backend ASP.NET Core (.NET 10) en capas, frontend Next.js con TypeScript y
PostgreSQL con PostGIS.

**Video demostrativo:** [recorrido por el prototipo en YouTube](https://www.youtube.com/watch?v=rGrq6nDurhs&t=97s)

## Capacidades por rol

| Rol | Qué puede hacer |
| --- | --- |
| Consumidor (sin cuenta) | Explorar negocios por zona y rubro, buscar productos con tolerancia a errores de tipeo, ver perfiles, catálogos y promociones, armar una solicitud y enviarla por WhatsApp, consultarla con un enlace privado. |
| Responsable del negocio | Registrarse, publicar su negocio con ubicación y fotos, administrar catálogo, inventario, solicitudes, ventas y campañas, comparar planes y consultar actividad y resultados comerciales. |
| Administración | Habilitar planes, aprobar o rechazar campañas y registrar su pago, gestionar rubros, suspender y reactivar cuentas, negocios y productos, y consultar la auditoría. |

## Historias de usuario y su implementación

Rutas relativas a `http://localhost:3000`. «Mi negocio» es el panel `/mi-negocio`.

| HU | Historia | Dónde verla | Estado |
| --- | --- | --- | --- |
| HU-01 | Registro y acceso a la plataforma | `/mi-negocio` (registro e ingreso). Cuenta suspendida informada tras contraseña correcta; bloqueo de 15 min tras 5 intentos fallidos | Implementada |
| HU-02 | Perfil y ubicación del negocio | Mi negocio → crear/editar negocio; perfil público en `/negocios/{id}` | Implementada |
| HU-03 | Novedades y promociones del negocio | Mi negocio → Ver campañas (promoción); `/promociones/{id}`. Se publican como campañas aprobadas y pagadas | Implementada |
| HU-04 | Catálogo y precios del negocio | Mi negocio → Administrar catálogo | Implementada |
| HU-05 | Borrador de producto asistido por IA | Editor de producto → «Ayuda para redactar»: con consentimiento, propone nombre, descripción y categoría a partir de la foto del producto y sus datos mínimos; el borrador es editable y no se publica solo. Requiere clave de OpenAI o de Gemini y un modelo con visión; sin ellos, o ante una falla o demora, informa que la carga es manual | Implementada |
| HU-06 | Movimientos de inventario | Mi negocio → Inventario. Saldo disponible al rechazar y «Disponibilidad baja» según umbral | Implementada |
| HU-07 | Descubrimiento de negocios de la zona | `/explorar`: al elegir zona y rubro, orden por ranking hiperlocal `equity-v1` | Implementada |
| HU-08 | Búsqueda de productos cercanos | `/productos`: tolerancia a errores de tipeo, orden por el ranking del negocio en la zona y opción de ampliar el radio | Implementada (ver limitaciones) |
| HU-09 | Consulta de la oferta y contacto | `/negocios/{id}`: catálogo, promociones vigentes y WhatsApp con mensaje de contexto; intención de contacto deduplicada | Implementada |
| HU-10 | Creación de una solicitud para mensajería | `/negocios/{id}` → armar solicitud → WhatsApp | Implementada |
| HU-11 | Consulta del estado de la solicitud | `/pedido/{id}` (enlace privado, oculto por defecto) | Implementada |
| HU-12 | Atención de las solicitudes recibidas | Mi negocio → Ver solicitudes | Implementada |
| HU-13 | Registro de venta y descuento de stock | Ver solicitudes → Registrar venta; Ventas reportadas | Implementada |
| HU-14 | Corrección de venta y devolución física | Ventas reportadas → anular con unidades devueltas | Implementada |
| HU-15 | Consulta y habilitación de la suscripción | Administrar catálogo → «Comparar planes Presencia e Impulso» (sin precios); habilitación en `/administracion/planes` | Implementada |
| HU-16 | Gestión de campañas publicitarias | Mi negocio → Ver campañas | Implementada |
| HU-17 | Revisión y activación de la publicidad | `/administracion/campanas`: aprobación, pago y rechazo con motivo visible para el responsable | Implementada |
| HU-18 | Métricas del negocio | Mi negocio → Ver actividad; Resultados comerciales | Implementada |
| HU-19 | Administración de cuentas, contenido y categorías | `/administracion/rubros` (crear, renombrar, deshabilitar y eliminar rubros sin uso) y `/administracion/moderacion` | Implementada (ver limitaciones) |
| HU-20 | Configuración y evaluación del prototipo | Política versionada `equity-v1` (`Ranking__Enabled`/`RANKING_ENABLED`); evaluación reproducible con `App/backend/tools/Marketplace.Evaluation` | Implementada |
| HU-21 | Registro de la exposición orgánica y patrocinada | `/explorar` con zona y rubro: se registran candidatos, posición y versión; «Patrocinado» separado del orden orgánico | Implementada |
| HU-22 | Registro de la actividad del negocio | Registro interno al actualizar perfil, catálogo o promociones, con deduplicación y tope para el ranking | Implementada |
| HU-23 | Auditoría de cambios sensibles | `/administracion/auditoria` | Implementada |

**Limitaciones documentadas**

- Un rubro solo se puede eliminar si ningún negocio lo usa; si está en uso, el sistema lo impide e indica cuántos negocios lo utilizan, y la alternativa es deshabilitarlo para conservar esos negocios.
- El orden por ranking de la búsqueda de productos no persiste registros de exposición
  propios: usa el historial de exposición del ranking de negocios.

## Inicio rápido local

Requisitos: Docker, [.NET SDK 10.0.400](https://dotnet.microsoft.com/download) (fijado en
`App/backend/global.json`), Node 24.20.0 con npm 11.19.0 y PowerShell. Desde la raíz:

```powershell
# 1. Base de datos PostGIS y migraciones
cd App
.\tools\setup-local.ps1                      # genera App/.env.local con una contraseña aleatoria
docker compose --env-file .env.local up -d database
$localPassword = (Get-Content .env.local) -replace '^POSTGRES_PASSWORD=', ''
$env:ConnectionStrings__Marketplace = "Host=127.0.0.1;Port=55432;Database=marketplace;Username=marketplace;Password=$localPassword"
dotnet run --project backend/src/Marketplace.Api -- --migrate
cd ..

# 2. Datos de demostración
powershell -NoProfile -ExecutionPolicy Bypass -File App/tools/seed-demo.ps1

# 3. API con ranking habilitado (http://localhost:5080)
$env:Ranking__Enabled = 'true'
dotnet run --project App/backend/src/Marketplace.Api --launch-profile http
```

En otra terminal, desde la raíz:

```powershell
cd App/frontend
Copy-Item .env.example .env.local
$env:RANKING_ENABLED = 'true'
npm ci
npm run dev                                   # http://localhost:3000
```

`/estado` debe mostrar «API disponible». Detalle de variables, pruebas y módulos en la
[guía técnica](App/README.md).

## Cuentas de evaluación

| Cuenta | Rol |
| --- | --- |
| `comerciante.seed@example.test` | Responsable de los tres negocios de demostración |
| `admin.seed@example.test` | Administración |

`seed-demo.ps1` genera contraseñas aleatorias en la primera ejecución y las guarda en
`.tools/seed-demo/credentials.json` (local, excluido de Git). Si se ejecuta el seed .NET
directamente, las contraseñas son los valores de `SEED_OWNER_PASSWORD` y
`SEED_ADMIN_PASSWORD`. Ver [datos de demostración](App/seed/README.md) y el
[guion de demostración](Doc/guion-demo.md).

## Arquitectura

- `Marketplace.Api`: endpoints HTTP, validación de origen y límites de tasa.
- `Marketplace.BLL`: reglas de negocio, ranking y contratos de persistencia.
- `Marketplace.DAL`: EF Core + Npgsql sobre PostgreSQL/PostGIS.
- `Marketplace.DTOs`, `Marketplace.Models`, `Marketplace.Utility`: contratos, entidades y utilidades.
- `Marketplace.Integrations`: adaptadores de OpenAI y Gemini para la asistencia de catálogo, elegidos por configuración (patrones Strategy, Template Method y Factory).
- `Marketplace.IoC`: composición de dependencias.
- Next.js atiende la interfaz y reenvía las llamadas a la API mediante un proxy de servidor.

Decisiones: [ADR-001](Doc/architecture/ADR-001-layered-architecture.md). Ranking:
[integración pública](Doc/ranking-public-integration.md),
[investigación](Doc/ranking-research-2026-09-19.md) y
[evaluación](Doc/evaluation/business-ranking-pilot/README.md).

## Pruebas

Desde la raíz:

```powershell
dotnet test App/backend/Marketplace.sln --configuration Release -m:1   # requiere TEST_DATABASE_CONNECTION
npm --prefix App/frontend test; npm --prefix App/frontend run lint; npm --prefix App/frontend run build
```

La integración continua (`.github/workflows/ci.yml`) ejecuta tres trabajos: búsqueda de
secretos con Gitleaks sobre todo el historial, backend (restore con auditoría NuGet,
build y pruebas contra PostGIS) y frontend (auditoría npm, pruebas, lint y build).

## Despliegue

El despliegue en Vercel (web), Railway (API) y Supabase (base de datos) está preparado
y documentado paso a paso en [Doc/despliegue.md](Doc/despliegue.md).

## Documentación

- [Guía técnica: entorno, ejecución, pruebas y módulos](App/README.md)
- [Bitácora de desarrollo](Doc/bitacora-desarrollo.md)
- [PRD: producto, alcance y ranking](Doc/PRD.md)
- [Modelo UML y diagramas ER de diseño](Doc/model/README.md) y [modelo de datos implementado](Doc/model/implementacion/README.md)
- [Investigación y rediseño de interfaz](Doc/ux-research-2026-09-18.md)
- [Investigación y decisiones de medición](Doc/analytics-research-2026-09-19.md)
- [Seguridad del repositorio](SECURITY.md)
- [Lista de control UML (histórica, previa a la adopción de Scrum)](Doc/documentation-checklist.md)

Los documentos académicos originales se conservan localmente y están excluidos de Git
por sus datos y metadatos personales.
