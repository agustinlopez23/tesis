# Prototipo del marketplace local

## Catálogo y planes manuales

En **Mi negocio → Editar negocio → Administrar catálogo** se consulta el plan y
se crean/editan productos con nombre, descripción, categoría, precio ARS/USD,
disponibilidad y estado. Guardar antes de subir la foto. Archivar conserva el
producto; publicado aparece en el perfil público si negocio y plan están habilitados.
La disponibilidad se combina con el stock cargado; las solicitudes y ventas se
gestionan en los paneles del negocio descritos más abajo.

El plan Impulso vigente habilita escrituras y lectura pública. Presencia conserva
perfil y lectura privada del catálogo. Vencer/revocar oculta productos y fotos en
la siguiente petición; conserva todo el historial. No cambia el ranking orgánico.

Aplicar la migración habitual antes de ejecutar esta versión, incluida
`20261005025720_AddBusinessCatalog`. El rol `administrator` existe sin asignaciones
automáticas. Un operador del entorno puede otorgarlo a una cuenta registrada y
habilitada, usando psql con la variable `admin_email` de esa cuenta:

```sql
INSERT INTO user_account_roles (user_id, role_id)
SELECT id, 2 FROM user_accounts
WHERE email = lower(:'admin_email') AND NOT is_suspended
ON CONFLICT DO NOTHING
RETURNING user_id;
```

Verificar la cuenta y el resultado en el entorno objetivo. No hay endpoint público
para elevar roles. Ingresar con esa cuenta y abrir **Administrar planes** desde el
panel, o `/administracion/planes`. Allí se buscan negocios, registran períodos y
revocan vigencias con motivo. Fechas de entrada en hora local del dispositivo;
inicio incluido, fin excluido. El prototipo no procesa el cobro.

Catálogo: `GET /businesses/{id}/plan`, `GET /businesses/{id}/products`,
`PUT /businesses/{id}/products/{uuid}`; para crear, `version: 0`, para editar,
versión recibida. GET/PUT/DELETE de foto en `.../{uuid}/photo`; subida multipart
`file` + `version`, retiro con `?version=N`. UUID estable evita altas duplicadas;
409 exige actualizar lista antes de resolver una edición en conflicto.

Público: `GET /public/businesses/{id}/products?page=1&q=...` y fotos en
`.../{uuid}/photo`. Veinte por página, orden alfabético; búsqueda dentro del negocio.
Administrador: `GET /admin/businesses?q=...&page=1`, `GET .../{id}/plan`,
`PUT .../{id}/subscriptions/{uuid}` con `startsAt`, `endsAt`, `reason` y
`POST .../{id}/subscriptions/{uuid}/revoke` con `reason`. La repetición idéntica de
activación/revocación conserva la operación original; no admite solapamientos.

Verificación integrada, tras compilar backend Release y frontend:
`node App/tools/verify-catalog.mjs` desde la raíz. Usa base generada y puertos
3200/5280, y elimina la base al terminar. `--serve` conserva hasta 20 minutos para
revisión visual; `.tools/catalog/session.json` contiene cuentas temporales y un
`stopFile` para terminar antes. No ejecutar otra compilación del backend mientras
esté sirviendo: Windows bloquea DLL cargadas.

[Contrato, límites y rollback](../openspec/specs/business-catalog/spec.md).
Down se bloquea cuando existen productos, vigencias o administradores asignados;
revertir código conserva el esquema aditivo para evitar pérdida de datos.

## Campañas patrocinadas

Mi negocio → Editar → Ver campañas permite preparar producto (plan vigente) o
promoción (foto existente de galería, también plan gratuito). Se conserva una
copia de imagen para revisión. Hasta 30 días y tres reservas superpuestas por
zona; anuncios rotan cada minuto. Elegí zona manual en Explorar o Productos para
ver Patrocinado, separado de resultados orgánicos.

`/administracion/campanas` permite aprobar/rechazar contenido, registrar pago ya
verificado mediante referencia, pausar/reanudar y registrar resolución comercial.
No cobra. El titular puede pausar y consultar resultados. Vencimiento de plan,
stock cero o suspensión ocultan sin prórroga automática; conservan historial.
Eventos duran 30 días; solicitudes/ventas conservan vínculo, importes por moneda
y origen. Ver [contrato](../openspec/specs/sponsored-campaigns/spec.md).

Aplicar `20261005121814_AddSponsoredCampaigns` antes de ejecutar esta versión.
Down bloquea datos/atribuciones existentes. Prueba aislada tras build:
`node App/tools/verify-catalog.mjs --campaigns` (opcional `--serve`, artefactos en
`.tools/campaigns`). Incluye verificaciones previas de catálogo, pedidos y ventas.

## Ayuda opcional para redactar productos

Dentro del editor de producto, **Ayuda para redactar** permite ingresar hechos,
aceptar su envío a OpenAI, revisar/editar la sugerencia y usarla en el formulario.
No guarda ni publica automáticamente. Precio, stock y estado se completan a mano.
Sin proveedor disponible, se conserva la carga manual.

Desactivada por defecto. Para habilitarla en el proceso backend, configurar
`CatalogAi__Enabled=true`, `CatalogAi__Model` con un modelo de la cuenta compatible
con Responses/Structured Outputs y `CatalogAi__ApiKey` mediante el gestor de
secretos del entorno. No colocar la clave en Next, Git ni archivos de ejemplo.
Endpoint fijo `https://api.openai.com/v1/responses`; no se admiten hosts arbitrarios.
No se eligió modelo ni se realizó una llamada paga durante este desarrollo.

Se envía solo el texto consentido, sin leer datos privados del negocio. `store:false`
desactiva almacenamiento de respuestas para recuperación vía API; no significa
retención cero de todos los registros del proveedor. Límites locales: seis POST
por minuto, dos generaciones simultáneas, 12 s, respuesta 128 KiB y 1000 tokens de
salida solicitados. Sin reintentos automáticos. Estos límites son por instancia;
no equivalen a presupuesto mensual ni garantizan calidad factual.

GET/POST `/businesses/{id}/catalog-assistance`, privado, no-store, JSON 8 KiB;
POST `{facts, consent:true}` devuelve `{name,description,category}` o error con
carga manual disponible. [Contrato](../openspec/specs/catalog-draft-assistance/spec.md).
Integración comprobada con transporte simulado; falta verificar credenciales,
modelo y calidad real antes de habilitarla para comerciantes.

## Base implementada

Backend ASP.NET Core en ocho proyectos de aplicación y frontend Next.js. La página inicial
presenta la guía de descubrimiento con la identidad visual de trabajo **Cerca**. El backend
ya incluye el primer corte de negocio: registro, ingreso y sesión revocable del
responsable y alta/edición de negocios, persistidos en PostgreSQL/PostGIS. El
panel web permite completar el recorrido desde el navegador.
La guía pública en `/` y `/explorar` muestra negocios publicados con contacto
por WhatsApp, paginados de a 20 y ordenados por nombre. Permite buscar por nombre,
descripción o dirección y filtrar por rubro; los filtros se conservan en la URL.
La búsqueda ignora mayúsculas y tildes en vocales. Registro y gestión están en
`/mi-negocio`. El panel distingue borradores y publicados, confirma guardados
y permite cargar coordenadas manualmente o usar la ubicación del dispositivo.
Este orden todavía no implementa el ranking de exposición equitativa.

La migración `AddBusinessPublicationHistory` registra la primera publicación y la
conserva al editar o republicar. Los registros anteriores, incluidos borradores,
quedan con historia desconocida. Aplicar las migraciones habituales antes de usar
este backend; el historial se verifica con la suite HTTP/PostGIS.

El lector interno `IRankingCandidateRepository` recupera todos los elegibles por
rubro/radio con distancia e historial, sin paginar antes del futuro ranking.
Está registrado en IoC; la guía sigue usando su consulta y orden vigentes.

`RankingSnapshotService` guarda listas y exposición en una transacción, con
reintentos deduplicados y trazas reproducibles. `RankingSnapshotPages` lee páginas
estables y revalida elegibilidad actual. Son servicios internos, sin endpoints.
La migración `AddRankingSnapshots` crea las cuatro tablas SQL de infraestructura;
aplicarla con el comando de migración habitual antes de arrancar este backend.
Las listas vencen a los 30 minutos y el historial a los 30 días; un worker purga
al iniciar/cada hora. [Contrato y límites](../Doc/ranking-public-integration.md).

El visitante puede elegir Centro (Plaza San Martín), Nueva Córdoba (Buen Pastor)
o Alberdi (Plaza Colón), o pulsar **Usar mi ubicación**. Las zonas son puntos de
referencia, no límites barriales. El radio va de 500 m a 50 km en la interfaz.
Con ubicación, se ordena por distancia, nombre e id y se muestra distancia
aproximada en línea recta. Sin ubicación, continúa la guía alfabética.

La API también acepta `lat`, `lon` y `radiusKm` juntos, por ejemplo
`/public/businesses?lat=-31.416281&lon=-64.184022&radiusKm=2`.
Valida coordenadas finitas y radio de 0,1 a 50 km; filtra antes de paginar.
El DTO agrega `distanceKm` solamente cuando hay un centro de búsqueda.
Aplicar las migraciones habituales incluye el índice espacial GiST.

Zona y radio quedan en la URL. Las coordenadas del dispositivo se conservan
en el estado de la entrada del historial del navegador para recarga/volver;
la aplicación no las guarda en localStorage ni en la base de datos. El navegador
controla la retención de su historial. Una URL copiada no contiene esas coordenadas.
La petición HTTP a la API sí las incluye para calcular los resultados.

Para probarla: crear un negocio desde el panel, seleccionar **Publicado** y
guardar; abrir **Explorar negocios**. Cambiarlo a **Borrador** lo retira de la
siguiente consulta. Los negocios de responsables suspendidos tampoco aparecen.
La API pública es `GET /public/businesses?page=1&q=cafe&category=Gastronomía`
y no requiere sesión. Los parámetros `q` y `category` son opcionales; se filtra
antes de paginar. La investigación y decisiones de interfaz están en
`../Doc/ux-research-2026-09-18.md`.

## Piloto reproducible de ranking

El motor experimental de negocios está en BLL y la consola en
`backend/tools/Marketplace.Evaluation`. Desde la raíz del repositorio:

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --output .tools/ranking-reproduction
```

Usar un directorio vacío. Ejecuta cuatro variantes, 200 negocios y 5.000 consultas
por cada semilla 17/29/43. Exporta inputs, configuración, trazas comprimidas,
métricas por zona/superficie y SHA-256. No necesita base de datos.
Pruebas: `dotnet test App/backend/tests/Marketplace.BLL.Tests --configuration Release`.

El piloto no está conectado al orden de la guía. La exposición modelada no es
una impresión visible real ni evidencia de ventas. Resultados, reproducción y
limitaciones: [evidencia del piloto](../Doc/evaluation/business-ranking-pilot/README.md).

Para separar compensación, novedad y rotación, agregar `--suite ablation` al
comando (después de `--`) y elegir otro directorio vacío. Ejecuta cinco variantes
con idénticos datos y pesos; `--suite pilot` conserva el piloto original.
[Resultados de ablaciones](../Doc/evaluation/business-ranking-ablations/README.md).

El [protocolo de evaluación final](../Doc/evaluation/business-ranking-final/protocol.md)
tiene criterios principales acordados, rúbrica y plantillas humanas. La consola
valida hojas con `--validate-review RUTA_CSV --output DIRECTORIO_VACIO` (sin otras
opciones): 0 completo, 2 incompleto, 1 inválido. Ver [instrucciones](../Doc/evaluation/business-ranking-final/human-review.md).
Esto no ejecuta todavía el experimento final; `plan.json` tampoco es una
configuración de pesos para `--config`.

Paquetes humanos: `--prepare-review training|calibration --output SALIDA_VACIA`
genera fichas sintéticas y hojas en blanco; `--collect-review PAQUETE
--package-sha256 HASH_FIJADO --output SALIDA_VACIA` verifica fuentes/fichas y
recoge grados. [Descargas e instrucciones](../Doc/evaluation/business-ranking-final/packages/README.md).

`--evaluate-review PAQUETE --package-sha256 HASH --config PESOS.json --variant EquityOnly --output SALIDA`
compara una candidata con Proximity solo en calibración, con hojas verificadas,
historial completo y métricas por superficie. Sin juicios completos devuelve 2
y un reporte de pendientes, sin ranking. No decide aceptación.
[Uso, variantes y límites](../Doc/evaluation/business-ranking-final/human-ranking.md).

`--calibrate-review PAQUETE --package-sha256 HASH --output SALIDA` recorre la grilla
fija de 18 configuraciones, comprueba criterios y sugiere una candidata provisional.
Separa límites acordados de salvaguardas propuestas; sin hojas completas devuelve
2 y no ejecuta rankings. [Uso y estados](../Doc/evaluation/business-ranking-final/calibration.md).

`--prepare-freeze PAQUETE --package-sha256 HASH --source-root RAÍZ --output SALIDA`
conserva fuentes, originales, binarios y calibración en un borrador con pendientes.
`--verify-freeze CARPETA --manifest-sha256 HASH_EXTERNO --output SALIDA` verifica su
integridad. Ninguno congela política ni abre reserva.
[Uso y alcance](../Doc/evaluation/business-ranking-final/freeze.md).

`node App/tools/verify-ranking-rebuild.mjs --draft CARPETA --manifest-sha256 HASH --dotnet RUTA_DOTNET --output SALIDA_NUEVA`
reconstruye fuentes verificadas, compara los seis archivos runtime y repite la
salida de calibración. Requiere Windows/SDK local y contexto Git del repositorio.
[Evidencia y condiciones](../Doc/evaluation/business-ranking-final/rebuild.md).
La evaluación reservada todavía no se genera.

## Actividad del negocio

En **Mi negocio → Ver actividad**, elegir negocio, período (7/30 días) y métrica.
El panel ofrece resumen, evolución diaria, tabla, actualización y definiciones.
No presenta clics como conversaciones o ventas. Días según hora de Córdoba.

`POST /public/events` recibe un comprobante emitido con la guía/perfil, UUID de
sesión por pestaña, tipo y superficie. Requiere origen confiable y cabecera
`X-Requested-With: marketplace`, máximo 4 KiB y límite global de 1200/minuto por
instancia. Los comprobantes vencen a los 30 minutos. Una impresión requiere
50 % de tarjeta visible durante un segundo continuo con pestaña activa.
Se deduplica por sesión/negocio/tipo/superficie y se excluye al dueño conectado.

`GET /businesses/{id}/activity?days=7|30` exige sesión y propiedad del negocio.
Retorna totales y días sin caché. Conserva eventos 30 días, con purga automática
al arrancar/cada hora, mediante servicio BLL. La tarea no se ejecuta automáticamente
en entorno Testing: las pruebas la invocan sobre sus bases aisladas.

La tabla de eventos no guarda IP, user-agent, coordenadas, búsqueda textual ni
contactos. El contexto incluye primera posición observada, versión del orden y
origen disponible. No identifica personas únicas ni prueba ausencia de bots.
No se usan servicios analíticos externos. Configurar claves de Data Protection
compartidas si se despliegan varias instancias y definir retención de logs/backups.
Contrato: `../openspec/specs/business-activity/spec.md`. Fuentes y decisiones:
`../Doc/analytics-research-2026-09-19.md`.

## Perfiles y fotos

Cada tarjeta enlaza a `/negocios/{id}`. El perfil público presenta descripción
completa, dirección, contacto por WhatsApp y galería. Sin fotos, conserva una
presentación tipográfica. Un negocio ausente, en borrador o de un responsable
suspendido no tiene perfil ni imágenes accesibles por la API pública.

En **Mi negocio**, guardar o editar un comercio y elegir **Administrar fotos**.
Hay hasta cinco posiciones: agregar, reemplazar con otro archivo o eliminar
con confirmación. La primera posición ocupada se usa como portada de la guía.
Después de guardar el negocio permanece seleccionado para poder agregar fotos.

- Entrada: JPEG, PNG o WebP estático; máximo 5 MB y 20 megapíxeles.
- Salida: JPEG sin metadatos, orientación corregida, máximo 1600 px y 1 MB.
- Persistencia: tabla `business_photos` en PostgreSQL, cinco slots por negocio.
  Incluir esta tabla en el respaldo de la base. Pensado para el prototipo;
  separar los archivos en almacenamiento de objetos al escalar.
- Dependencia: SkiaSharp y SkiaSharp.NativeAssets.Linux.NoDependencies
  **4.152.0**, auditados durante restore. Utility no referencia las capas;
  IoC adapta su resultado a la interfaz de procesamiento definida en BLL.

API de perfil: `GET /public/businesses/{id}`.
Imágenes públicas: `GET /public/businesses/{id}/photos/{slot}`.
Listado privado: `GET /businesses/{id}/photos`. Imagen privada, carga y
eliminación: `GET/PUT/DELETE /businesses/{id}/photos/{slot}`.
PUT usa multipart con `file` y `caption` (opcional, hasta 160 caracteres).
Las rutas privadas requieren propietario; las mutaciones también validan origen.
No se cachean imágenes: despublicar bloquea nuevas lecturas públicas.

La demo local usa una imagen ilustrativa rotulada como tal; no representa una
foto verificada del negocio ficticio. Evidencia:
`../openspec/changes/archive/2026-09-18-public-business-profile/verify-report.md`.

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

Abrir `http://localhost:3000` para explorar o `/mi-negocio` para administrar.
El diagnóstico técnico está en `http://localhost:3000/estado`.
Con API iniciada debe mostrar `API disponible`.
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
edita un negocio, verifica publicación/despublicación en la guía sin sesión,
comprueba `/explorar`, revoca la sesión y elimina la base temporal al terminar.
Las pruebas automatizadas de interfaz cubren tarjetas, enlace de WhatsApp,
paginación y recuperación ante fallos; el script integrado comprueba HTTP.

## Previsualización HTTP del ranking

La creación experimental se habilita solo en `Development` o `Testing` mediante
`Ranking__PreviewEnabled=true`. Por defecto está cerrada y en Production esa
variable no habilita nada. No se activó en la demo. Requiere las migraciones del
backend, incluida `20261004100000_AddRankingSnapshots`.

`POST /public/ranking-snapshots` recibe JSON con `requestId` (UUID nuevo por
consulta, conservado al reintentar), `zoneId` (`centro`, `nueva-cordoba` o
`alberdi`), `categoryId` existente y `radiusKm: 4`. Exige `Origin` igual a
`FrontendOrigin` y `X-Requested-With: marketplace`. Cuerpo máximo 4096 bytes;
otros campos/filtros se rechazan. Crea con 201; reintento con 200; conflicto 409.

La respuesta ofrece `items`, `measurement`, `expiresAt`, `rankingVersion`, `cursor`,
`hasMore` y `nextCursor`. Usar este último en
`GET /public/ranking-snapshots/page?cursor=...`, sin agregar filtros. Cursor
inválido: 400; vencido/retirado: 410. Todas las respuestas son no-store.
Política local de prueba: EquityOnly, parámetros por defecto, semilla 17; el
hash identifica esa política sin afirmar que esté evaluada. La guía conserva
su orden; la vista local `/revista` consume estas rutas. El proxy Next
admite estas dos rutas en desarrollo:
`RANKING_PREVIEW_ENABLED=true` permite POST; GET local permite terminar cursores
vigentes aunque se apague creación. Producción rechaza ambas aun con esa opción.
El backend requiere su propia habilitación. `cursor` permite releer la página
actual vía GET, incluida la primera, sin crear otra oportunidad ni renovar TTL.
Antes de usar medición desde la revista se requiere la migración
`20261005020557_AllowRankingActivityVersions`: admite el hash de política en
actividad, conservando las versiones anteriores. Downgrade bloqueado mientras
queden eventos con hash; esperar su retención/purga en lugar de borrar atribución.

Con la habilitación local aparece «Probar la revista de tu zona». En `/revista`,
elegir zona/rubro y pulsar «Descubrir negocios». Reintentar conserva la consulta;
«Actualizar resultados» crea otra. Anterior/siguiente/recarga vuelven a consultar
la elegibilidad de la misma página. Los cursores quedan en el historial de esa
pestaña, sin tarjetas cacheadas ni tokens en la URL. Vencimiento y retirada
exigen actualización; los errores ofrecen la guía habitual.

Verificación reproducible: con PostGIS local activo, backend compilado en Release
y `npm run build` del frontend, ejecutar desde la raíz:
`node App/tools/verify-ranking-preview.mjs`. Crea/migra/elimina una base temporal,
siembra 45 negocios y verifica Next/API/medición, páginas 20/20/5, replay y cierre
productivo aun con flag true. Usa puertos libres 3100/3101/5180; no reemplaza demo.
`--serve` mantiene la vista de prueba para Playwright hasta 20 minutos o hasta
crear el `stopFile` indicado en `.tools/ranking-preview/runtime.json`.

Cuotas por proceso: `Ranking__CreatePermitLimit=60` y
`Ranking__PagePermitLimit=600` por minuto, sin cola; exceso devuelve 429.
Cada creación admite hasta `Ranking__MaxCandidates=1000` candidatos y
`Ranking__MaxBalanceRows=10000` saldos históricos por contexto. Rangos válidos:
1–10000 y 1–100000 respectivamente. Una fila adicional produce 503 y rollback;
nunca se clasifica un prefijo. Se puede reintentar con el mismo requestId.
Los saldos incluyen negocios que ya no son elegibles. Concurrencia por proceso:
`Ranking__CreateConcurrencyLimit=4` y `Ranking__PageConcurrencyLimit=16`, con rango
1–64 cada uno. Cupos independientes, sin cola; exceso devuelve 429 no-store.
Los permisos se liberan al completar, fallar o cancelar la solicitud, incluida
la serialización de la respuesta. La configuración inválida impide el arranque.
Verificación local: 1.000 candidatos, 17 listas y solicitudes simultáneas a
4 POST/16 GET; [evidencia y límites de la medición](../openspec/changes/archive/2026-10-04-ranking-capacity/verify-report.md).
Desactivar PreviewEnabled cierra creación y permite terminar listas de 30 minutos.
`Ranking__WithdrawPages=true` retira inmediatamente creación y lectura con 410
(creación ya desactivada mantiene 404). Antes de producción faltan evaluación,
dimensionamiento productivo y gestión compartida de claves Data Protection.

## Solicitudes de productos y WhatsApp

En el perfil público de un negocio con catálogo habilitado, agregar productos,
revisar cantidades y precios, indicar nombre o alias y confirmar. La solicitud
queda guardada antes de abrir WhatsApp. Se puede copiar el resumen y conservar
«Consultar mi solicitud», un enlace privado válido por 30 días. Abrir WhatsApp
no confirma envío ni venta. Los totales mantienen ARS y USD separados.

En `/mi-negocio`, editar el negocio y abrir «Ver solicitudes». Filtrar por estado
o referencia, abrir el detalle y marcar atención, contacto recibido o cancelación.
Los cambios quedan auditados. El titular conserva acceso aunque venza el plan.
La cancelación es definitiva y no se permite desmarcar contacto recibido.

Una respuesta perdida permite reintentar con la misma identidad; recargar la
pestaña intenta recuperar el pedido. No borrar los datos de esa pestaña durante
un reintento pendiente. Si el catálogo cambió, actualizarlo y quitar/agregar el
producto permite revisar su nuevo precio. Guardar el enlace privado antes de
preparar otra solicitud: su clave no se incluye en el mensaje de WhatsApp.

Requiere `20261005111735_AddProductOrders`. Su downgrade rechaza datos existentes;
no elimina solicitudes para revertir esquema. Acceso privado vencido no borra
historia. No se aplica esta migración a la demo automáticamente.

Con PostgreSQL/PostGIS activo y backend Release/frontend production compilados:

```powershell
node App/tools/verify-catalog.mjs --orders
```

Usa puertos 3200/5280 y una base temporal que elimina al finalizar. `--serve`
mantiene la sesión hasta 20 minutos para navegador; `.tools/orders/session.json`
indica `stopFile` para terminar antes. Artefactos locales ignorados pueden contener
credenciales de esa sesión temporal. Contrato y límites en
[`product-orders/spec.md`](../openspec/specs/product-orders/spec.md); evidencia en
[`verify-report.md`](../openspec/changes/archive/2026-10-05-product-orders/verify-report.md).

## Evidencia y mantenimiento

### Ventas e inventario

En `/mi-negocio`, editar el negocio y abrir Inventario. Cada producto empieza
«Sin cargar»: registrar su cantidad real y un motivo antes de vender. Los ajustes
posteriores suman o restan unidades y conservan actor, fecha y saldo resultante.

Desde una solicitud pendiente o en atención, elegir «Registrar venta de esta
solicitud», revisar productos/cantidades/precios finales y origen de cada línea.
La confirmación descuenta todas las unidades o rechaza todo por conflicto.
Ventas reportadas permite consultar el resultado o registrar una venta presencial
sin atribución a solicitudes. Esta última requiere plan vigente.

Para anular, indicar motivo y unidades realmente devueltas; los campos empiezan
en cero y no hay reposición automática. La venta queda conservada y una solicitud
vinculada termina cancelada. La anulación es definitiva y no permite una segunda
venta para la misma solicitud. Importes siempre separados por moneda y origen.

Ante respuesta perdida, reabrir el mismo formulario y reintentar: la identidad
queda guardada en esa pestaña. No borrar sus datos durante una operación pendiente.
Ante conflicto de versión, actualizar y volver a seleccionar el producto/solicitud.

Migración `20261005114423_AddSalesInventory`; downgrade poblado bloqueado.
Reproducción integrada: `node App/tools/verify-catalog.mjs --sales`, con builds
Release/production previos y PostGIS activo. `--serve` habilita navegador temporal;
sesión y stopFile en `.tools/sales/session.json`, puertos 3200/5280. Limpieza al salir.
Contrato en [`sales-inventory/spec.md`](../openspec/specs/sales-inventory/spec.md).

Resultados comerciales, dentro de editar negocio, muestra 7/30 días calendario
de Córdoba. La conversión sigue solicitudes creadas en ese período hasta ahora;
ventas e importes usan fecha de registro de venta y excluyen anuladas. Importes
separados por moneda y origen. No son pagos verificados ni evaluación experimental.
Se verifican con el mismo harness `--sales`; no requieren migración adicional.

Desde Explorar, «Buscar productos entre negocios» abre `/productos`. Texto,
rubro, zona/ubicación y radio filtran catálogos elegibles; orden alfabético o por
distancia, páginas de 20. Las tarjetas conducen al catálogo del negocio para
solicitar. Stock conocido cero se excluye; el ranking experimental sigue cerrado.
La URL conserva filtros sin publicar coordenadas del dispositivo.

La suite API limita a dos las clases xUnit simultáneas para reducir carga al crear
y migrar bases PostGIS aisladas. Las pruebas de ventas/ranking siguen lanzando sus
operaciones concurrentes internas. Evitar ejecutarla junto con builds intensivos.

El scaffold archivado está en
`../openspec/changes/archive/2026-09-08-scaffold-backend-frontend/`; el onboarding
y su evidencia están en
`../openspec/changes/archive/2026-09-08-business-owner-onboarding/`.
No se publica ni se contrata hosting desde este prototipo.
