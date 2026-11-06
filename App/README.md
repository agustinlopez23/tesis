# Guía técnica del prototipo

Instrucciones para preparar, ejecutar y probar Cerca. La presentación general y la
relación con las historias de usuario están en el [README de la raíz](../README.md);
la evolución del prototipo, con evidencia por incremento, en la
[bitácora de desarrollo](../Doc/bitacora-desarrollo.md).

Todos los comandos se ejecutan en PowerShell desde la raíz del repositorio, salvo que
se indique otro directorio.

## Requisitos

| Herramienta | Versión | Fuente |
| --- | --- | --- |
| .NET SDK | **10.0.400** exacta | `backend/global.json` (`rollForward: disable`) |
| Node / npm | **24.20.0** / **11.19.0** | `frontend/.nvmrc` y `engines` de `frontend/package.json` |
| Docker | Compose v2 | `compose.yaml` (imagen `postgis/postgis:17-3.5`) |
| PowerShell | 5.1 o superior | scripts de `tools/` |

Las dependencias directas del frontend usan versiones exactas con lockfile. Para las
pruebas de navegador se instalan además Chromium, Firefox y WebKit de Playwright
(ver [Pruebas](#pruebas)).

En el workspace del autor el SDK está en `.tools/dotnet`, fuera de `App/` e ignorado
por Git; en otra máquina se instala el SDK desde Microsoft. Para usar ese SDK local,
en cada terminal:

```powershell
$env:DOTNET_ROOT = (Resolve-Path .tools/dotnet).Path
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.tools/cli'
dotnet --version
```

## Configuración del entorno

1. `App/tools/setup-local.ps1` crea `App/.env.local` con `POSTGRES_PASSWORD` aleatoria.
   No modifica un archivo existente.
2. En `App/frontend`, copiar `.env.example` a `.env.local` (valores locales, sin
   credenciales). `.env.local` está ignorado por Git.

Las variables de la API usan la convención de ASP.NET Core (`Seccion__Clave`). Las de la
web se leen en el servidor de Next; deben establecerse antes de compilar y, tras
cambiarlas, hay que reconstruir y reiniciar (o reiniciar `npm run dev`).

### API (`Marketplace.Api`)

| Variable | Por defecto | Uso |
| --- | --- | --- |
| `ConnectionStrings__Marketplace` | marcador `configure-locally` | Conexión a PostgreSQL/PostGIS. Obligatoria. |
| `ASPNETCORE_ENVIRONMENT` | `Development` (perfil `http`) | `Production` activa la validación de arranque. |
| `FrontendOrigin` | `http://localhost:3000` solo en Development | Origen web permitido para mutaciones. Obligatorio en Production. |
| `Proxy__SharedSecret` | sin valor | Secreto compartido con el proxy web (`API_PROXY_SECRET`); ≥ 32 caracteres en Production. |
| `Database__MigrateOnStartup` | `false` | Aplica migraciones antes de aceptar tráfico. |
| `PORT` | sin valor | Puerto de escucha si `ASPNETCORE_URLS` no está definido (Railway). |
| `Auth__PermitLimit` | `5` | Solicitudes de autenticación por minuto y por IP. |
| `Ranking__Enabled` | `false` | Ranking `equity-v1` en `/explorar` y `/productos`, en cualquier entorno. |
| `Ranking__PreviewEnabled` | `false` | Vista previa `/revista`; solo Development o Testing. |
| `Ranking__WithdrawPages` | `false` | Retira creación y lectura de listas con 410. |
| `Ranking__CreatePermitLimit` / `Ranking__PagePermitLimit` | `60` / `600` | Cuota por minuto y proceso, sin cola (429). |
| `Ranking__CreateConcurrencyLimit` / `Ranking__PageConcurrencyLimit` | `4` / `16` | Concurrencia por proceso, rango 1–64. |
| `Ranking__MaxCandidates` / `Ranking__MaxBalanceRows` | `1000` / `10000` | Topes por contexto, rangos 1–10000 y 1–100000. |
| `Catalog__LowStockThreshold` | `3` | «Disponibilidad baja» cuando el saldo es mayor que cero y menor al umbral. |
| `OwnerActivity__DedupMinutes` | `60` | Ventana en la que una actualización del responsable cuenta una sola vez por tipo. |
| `OwnerActivity__RankingCap` | `10` | Actualizaciones en 30 días que saturan la señal de actividad del ranking. |
| `CatalogAi__Enabled` | `false` | Habilita la asistencia de redacción. |
| `CatalogAi__Provider` | `OpenAI` | Proveedor de IA: `OpenAI` o `Gemini`. Un valor desconocido impide el arranque. |
| `CatalogAi__Model` | vacío | Modelo con visión y salida JSON del proveedor elegido (por ejemplo, `gpt-4o-mini` en OpenAI o un modelo Flash en Gemini); recibe la foto del producto. |
| `CatalogAi__ApiKey` | sin valor | Clave del proveedor elegido, solo mediante el gestor de secretos del entorno. |

La configuración de ranking inválida impide el arranque. En `Production` la API no
arranca sin conexión, `FrontendOrigin` (solo origen, sin barra final) y
`Proxy__SharedSecret`.

### Web (`frontend`)

| Variable | Por defecto | Uso |
| --- | --- | --- |
| `APP_ORIGIN` | sin valor (el proxy responde 503) | Origen web permitido. |
| `API_BASE_URL` | sin valor (el proxy responde 503) | Dirección privada de la API usada por el proxy. |
| `API_PROXY_SECRET` | sin valor | Igual a `Proxy__SharedSecret`; habilita el reenvío de la IP del visitante. |
| `NEXT_PUBLIC_API_URL` | sin valor | Alternativa para la señal de disponibilidad de `/estado`. |
| `RANKING_ENABLED` | `false` | Usa el ranking en `/explorar` (variable de servidor, sin `NEXT_PUBLIC`). |
| `RANKING_PREVIEW_ENABLED` | `false` | Permite la vista previa `/revista` solo en desarrollo. |

Ninguna variable web debe contener credenciales salvo `API_PROXY_SECRET`, que se
configura como secreto del entorno.

### Seed y pruebas

| Variable | Uso |
| --- | --- |
| `SEED_OWNER_PASSWORD` / `SEED_ADMIN_PASSWORD` | Contraseñas iniciales de las cuentas de demostración (distintas, 12–128 caracteres). `seed-demo.ps1` las genera. |
| `DEMO_SEED_CONFIRM_HOST` | Host exacto de una base hosteada; se usa junto con `--allow-hosted-demo`. |
| `TEST_DATABASE_CONNECTION` | Conexión de las pruebas de integración, con base `postgres`. |

## Base de datos y migraciones

```powershell
cd App
.\tools\setup-local.ps1
docker compose --env-file .env.local up -d database
$localPassword = (Get-Content .env.local) -replace '^POSTGRES_PASSWORD=', ''
$env:ConnectionStrings__Marketplace = "Host=127.0.0.1;Port=55432;Database=marketplace;Username=marketplace;Password=$localPassword"
dotnet run --project backend/src/Marketplace.Api -- --migrate
```

PostGIS escucha solo en `127.0.0.1:55432`. `--migrate` aplica las migraciones
pendientes y termina; `Database__MigrateOnStartup=true` hace lo mismo al arrancar.
Los downgrades de las migraciones con datos (catálogo, solicitudes, ventas, campañas,
actividad con hash de política) se bloquean para no perder información.

## Datos de demostración

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File App/tools/seed-demo.ps1
```

Requiere la base migrada. Carga tres comercios, doce productos, stock inicial, planes
ficticios y dos promociones publicadas; crea `comerciante.seed@example.test` y
`admin.seed@example.test`. La primera ejecución genera contraseñas aleatorias en
`.tools/seed-demo/credentials.json` (ignorado por Git). Se puede repetir sin duplicar
ni restablecer ediciones; renueva la vigencia de las promociones. Una base hosteada
requiere `--allow-hosted-demo` y `DEMO_SEED_CONFIRM_HOST`.
[Datos, accesos e instrucciones](seed/README.md).

El rol `administrator` no se asigna por API. Para otorgarlo a otra cuenta registrada y
habilitada, un operador del entorno puede usar psql con la variable `admin_email`:

```sql
INSERT INTO user_account_roles (user_id, role_id)
SELECT id, 2 FROM user_accounts
WHERE email = lower(:'admin_email') AND NOT is_suspended
ON CONFLICT DO NOTHING
RETURNING user_id;
```

## Ejecución

### API

```powershell
cd App/backend
dotnet restore Marketplace.sln
dotnet build Marketplace.sln --no-restore --configuration Release -m:1
$env:Ranking__Enabled = 'true'     # opcional
dotnet run --project src/Marketplace.Api --launch-profile http
```

El perfil `http` usa `http://localhost:5080` sin credenciales y requiere
`ConnectionStrings__Marketplace` en la misma terminal. `GET /health/live` responde 200
con `{"status":"ok"}`: indica que el proceso HTTP vive, no que la base esté disponible.

### Web

```powershell
cd App/frontend
Copy-Item .env.example .env.local
npm ci
$env:RANKING_ENABLED = 'true'      # opcional
npm run dev                        # o: npm run build; npm start
```

Abrir `http://localhost:3000` para explorar, `/mi-negocio` para administrar y `/estado`
para el diagnóstico: con la API iniciada muestra «API disponible»; detenida, «API no
disponible». La consulta aborta a los 3 segundos y solo acepta HTTP 200 con el objeto
exacto de liveness.

El navegador recibe HTML y llama a la API a través del proxy de Next
(`/api/marketplace/...`), por lo que no se configura CORS. Las mutaciones exigen el
origen configurado y el encabezado `X-Requested-With: marketplace`. El token de sesión
viaja solo en una cookie HttpOnly, se guarda como hash y cerrar sesión lo revoca.

## Pruebas

| Suite | Comando | Notas |
| --- | --- | --- |
| Backend (unitarias e integración) | `cd App/backend; dotnet test Marketplace.sln --configuration Release -m:1` | Requiere `TEST_DATABASE_CONNECTION` (misma conexión, base `postgres`). Cada clase crea y elimina su base temporal. |
| Solo BLL / ranking | `dotnet test App/backend/tests/Marketplace.BLL.Tests --configuration Release` | No necesita base de datos. |
| Frontend | `cd App/frontend; npm test; npm run lint; npm run build` | Vitest, ESLint y build de producción. |
| Política de auditoría npm | `node --test App/tools/audit-policy.test.mjs` | También en CI. |

La suite API limita a dos las clases xUnit simultáneas para reducir la carga al crear
y migrar bases PostGIS; evitar ejecutarla junto con builds intensivos.

### Verificaciones integradas (`App/tools`)

Requieren PostGIS local, backend compilado en Release y frontend compilado con
`npm run build`. Usan bases temporales que eliminan al terminar. No compilar el
backend mientras un script está sirviendo: Windows bloquea las DLL cargadas.

| Comando | Qué verifica |
| --- | --- |
| `node App/tools/verify-connectivity.mjs` | Señal de disponibilidad (frontend compilado con `NEXT_PUBLIC_API_URL=http://localhost:5080`; puertos 3000, 5080 y 5081). |
| `node App/tools/verify-onboarding.mjs` | Registro, negocio, publicación en la guía, proxy y revocación de sesión. |
| `node App/tools/verify-catalog.mjs` | Catálogo y planes (puertos 3200/5280). |
| `node App/tools/verify-catalog.mjs --orders` | Solicitudes de productos. |
| `node App/tools/verify-catalog.mjs --sales` | Ventas, inventario y resultados comerciales. |
| `node App/tools/verify-catalog.mjs --campaigns` | Campañas, más catálogo, pedidos y ventas. |
| `node App/tools/verify-ranking-preview.mjs` | Vista previa del ranking: 45 negocios, páginas 20/20/5, replay y cierre productivo (puertos 3100/3101/5180). |

`--serve` mantiene la sesión hasta 20 minutos para revisión en navegador; el archivo
`session.json` o `runtime.json` en `.tools/<verificación>/` indica un `stopFile` para
terminar antes. Esos artefactos locales pueden contener credenciales temporales.

### Navegadores y accesibilidad

```powershell
npm --prefix App/tools ci
npm --prefix App/tools run install:browsers
npm --prefix App/tools run test:browser          # Chromium, Firefox y WebKit
npm --prefix App/tools run test:accessibility    # axe-core sobre el flujo comercial
```

[Instalación, cobertura y resultados](tools/README.md).

## Módulos funcionales

### Catálogo y planes

**Mi negocio → Editar negocio → Administrar catálogo**: productos con nombre,
descripción, categoría, precio ARS/USD, disponibilidad y estado. Guardar antes de subir
la foto. Archivar conserva el producto. El plan Impulso vigente habilita escrituras y
lectura pública; Presencia conserva perfil y lectura privada. «Comparar planes Presencia
e Impulso» muestra capacidades sin precios. Vencer o revocar oculta productos y fotos
sin borrar historial y no cambia el ranking orgánico.

`/administracion/planes` registra y revoca vigencias con motivo (inicio incluido, fin
excluido, hora local). El prototipo no procesa cobros.
API: `GET /businesses/{id}/plan`, `GET /businesses/{id}/products`,
`PUT /businesses/{id}/products/{uuid}` (`version: 0` para crear; 409 ante conflicto),
foto en `.../{uuid}/photo`; públicas en `GET /public/businesses/{id}/products`.

**Fotos del negocio**: hasta cinco por negocio (**Administrar fotos**); entrada JPEG,
PNG o WebP de hasta 5 MB y 20 MP; salida JPEG sin metadatos de hasta 1600 px y 1 MB,
guardada en `business_photos` (incluir en los respaldos).

### Solicitudes y WhatsApp

En `/negocios/{id}`, el visitante arma una solicitud que se guarda antes de abrir
WhatsApp, con mensaje de contexto y totales separados por moneda. El enlace privado
`/pedido/{id}` vale 30 días, se muestra oculto por defecto y no viaja en el mensaje.
El responsable la atiende en **Ver solicitudes** (atención, contacto recibido o
cancelación, auditados).

### Ventas e inventario

**Inventario**: cada producto empieza «Sin cargar»; los ajustes conservan actor, fecha,
motivo y saldo. **Registrar venta de esta solicitud** descuenta todas las unidades o
rechaza todo por conflicto, informando el saldo disponible. **Ventas reportadas**
permite ventas presenciales (con plan vigente) y anulaciones con motivo y unidades
devueltas. **Resultados comerciales** resume 7/30 días por moneda y origen.

### Campañas y promociones

**Ver campañas** prepara campañas de producto (plan vigente) o promociones (también
con plan gratuito), de hasta 30 días y tres reservas superpuestas por zona.
`/administracion/campanas` aprueba o rechaza con motivo, registra un pago ya verificado,
pausa y reanuda. Se muestran como «Patrocinado», separadas del orden orgánico; las
promociones tienen página propia en `/promociones/{id}`.

### Moderación y auditoría

`/administracion/rubros` crea, renombra, deshabilita y elimina rubros. Un rubro
deshabilitado no se elige para negocios nuevos; los existentes siguen visibles. Un rubro
en uso no se puede eliminar: la API responde 409 e indica cuántos negocios lo usan.
`/administracion/moderacion` suspende y reactiva cuentas de comercio, negocios y
productos con motivo. `/administracion/auditoria` lista `admin_actions`, registradas en
la misma transacción que el cambio. Tras 5 ingresos fallidos la cuenta queda bloqueada
15 minutos con el mismo 401 genérico; una cuenta suspendida se informa solo tras una
contraseña correcta.

### Ranking hiperlocal

Con `Ranking__Enabled=true` y `RANKING_ENABLED=true`, `/explorar` ordena por la política
`equity-v1` al elegir zona (Centro, Nueva Córdoba o Alberdi) y rubro; sin ellos o ante
un fallo se mantiene la guía habitual. La exposición se registra en tablas de listas y
saldos (candidatos, posición y versión), con listas de 30 minutos e historial de 30 días.
En `/productos`, la búsqueda tolera errores de tipeo (`pg_trgm`, `fuzzystrmatch`) y,
con zona, ordena por la posición del negocio sin registrar exposición propia.
[Contrato](../Doc/ranking-public-integration.md).

La vista previa `/revista` (solo Development/Testing con `Ranking__PreviewEnabled=true`
y `RANKING_PREVIEW_ENABLED=true`) usa `POST /public/ranking-snapshots` y
`GET /public/ranking-snapshots/page?cursor=...`.

**Evaluación reproducible** (sin base de datos):

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --output .tools/ranking-reproduction
```

Usar un directorio vacío. `--suite ablation` ejecuta las ablaciones; también existen
`--validate-review`, `--prepare-review`, `--collect-review`, `--evaluate-review`,
`--calibrate-review`, `--prepare-freeze` y `--verify-freeze`, y
`node App/tools/verify-ranking-rebuild.mjs` reconstruye un borrador congelado.
Uso y resultados: [piloto](../Doc/evaluation/business-ranking-pilot/README.md),
[ablaciones](../Doc/evaluation/business-ranking-ablations/README.md) y
[protocolo final](../Doc/evaluation/business-ranking-final/protocol.md).

### Actividad

**Ver actividad** muestra apariciones visibles, visitas al perfil y clics de WhatsApp
por negocio y período (7/30 días, hora de Córdoba). `POST /public/events` deduplica por
sesión, negocio, tipo y superficie, y excluye al dueño conectado; no guarda IP,
user-agent ni coordenadas. Los eventos se purgan a los 30 días.
Además, cada actualización del responsable (perfil, catálogo o promociones) se registra
una vez por ventana `OwnerActivity__DedupMinutes` y alimenta el ranking con tope
`OwnerActivity__RankingCap`.

### Asistencia IA

En el editor de producto, **Ayuda para redactar** envía al proveedor configurado, con consentimiento, una
foto opcional del producto y los datos mínimos que el responsable escribe, y devuelve
nombre, descripción y categoría editables; no guarda ni publica. La API valida la foto
(JPEG, PNG o WebP, hasta 5 MB y 20 megapíxeles), la reencoda como JPEG de 1024 px como
máximo sin metadatos y la envía en línea dentro de la solicitud; no la almacena.
Tras guardar el producto, el editor ofrece **Usar la foto del borrador**, que la sube
por el flujo normal de fotos. Está implementada y desactivada por defecto: requiere
`CatalogAi__Enabled=true`, `CatalogAi__Model` y `CatalogAi__ApiKey`; sin ellos informa
que la carga es manual. Límites por instancia: seis POST por minuto, dos generaciones
simultáneas, 12 s y 128 KiB de respuesta, sin reintentos.

El proveedor se elige con `CatalogAi__Provider`, sin tocar la lógica de negocio:

- **Strategy.** `ICatalogDraftProvider` (BLL) es el puerto; cada proveedor es una estrategia
  en `Marketplace.Integrations`. La pantalla nombra en el consentimiento al proveedor que
  informa la API.
- **Template Method.** `CatalogDraftProviderBase` fija el flujo común: cupo, tiempo máximo,
  tope de respuesta y validación del borrador. Cada estrategia define solo cómo arma la
  solicitud y dónde lee el texto.
- **Factory.** `CatalogDraftProviderFactory` crea la estrategia según la configuración y
  rechaza al arrancar un proveedor desconocido.

| Proveedor | Endpoint fijo | Detalles |
|---|---|---|
| `OpenAI` (por defecto) | `https://api.openai.com/v1/responses` | `store:false`, esquema estricto, foto como `input_image` con `detail: "low"`, 1000 tokens de salida. |
| `Gemini` | `https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent` | Clave en el encabezado `x-goog-api-key`, `responseJsonSchema`, foto como `inlineData`, 4096 tokens de salida. |

La capa gratuita de Gemini no requiere tarjeta, pero Google puede usar ese contenido para
mejorar sus productos; conviene usarla solo con datos de demostración.

Para agregar otro proveedor: una clase que herede de `CatalogDraftProviderBase`, un caso en
la fábrica y sus pruebas.

## Despliegue

Vercel (web), Railway (API, `backend/Dockerfile` y `backend/railway.json`) y Supabase
(base de datos): guía paso a paso en [Doc/despliegue.md](../Doc/despliegue.md). Fuera de
Development, las claves de Data Protection se guardan en la tabla `data_protection_keys`,
por lo que los tokens sobreviven reinicios y redeploys.
