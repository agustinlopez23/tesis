# Evidencia incremental

## Cursor actual y proxy

RED API: Http_pages_retries falla al buscar `cursor` ausente. GREEN:
`.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~RankingHttpTests'`
24/24 HTTP/PostGIS temporal, cero omitidas, sin advertencias. Primera y segunda
página releídas vía sus propios cursores conservan IDs y una sola oportunidad.

RED proxy: 11/11 fallan por ruta inexistente/no-store ausente. Corregido un Request
mal construido en el fixture (Request usado como RequestInit), sin cambiar contrato.
GREEN: `npm test -- tests/ranking-proxy.test.ts tests/marketplace-proxy.test.ts`,
17/17. Harness llama handlers Next con Request/Response reales y upstream simulado:
sesión, cuerpo, query íntegra, estados 200/400/409/410/429/503, origen/cuerpo,
cancelación y fallo de red no-store. Producción cerrado incluso con flag true;
GET local continúa al deshabilitar POST. Integración real Next/API pendiente del
cierre de UI. Rollback: campo Cursor/traducción HTTP y aserciones; rutas/guard
del proxy, propagación de cancelación y no-store; configuración y pruebas asociadas.

Unidad contrato/proxy: `9289572`.

## Estado de navegación

Pruebas añadidas junto a implementación (sin RED atribuido):
`npm test -- tests/ranking-navigation.test.tsx`, **8/8**. No crea al montar;
reintento conserva requestId incluso tras remonte; navegación/recarga usa GET,
sin almacenar tarjetas ni poner tokens en URL. Frontera React/fetch simulada:
429/503/502, cancelación y respuesta tardía, huecos, popstate, cambio de versión,
vencimiento local y actualización con nueva identidad. Integración real pendiente
de presentación. Rollback: hook use-ranking-preview y sus pruebas, sin tocar proxy.

Unidad de navegación: `1bdf7e1`.

## Compatibilidad de medición descubierta en navegador

Playwright encontró 503 en `/public/events`: varchar(40) y CK_activity_context
solo admitían name-v1/distance-v1, mientras el token del ranking lleva SHA-256.
RED: Http_pages_retries ampliado espera 204 y recibe 503 al enviar una impresión.
Se amplía ranking_version a 64 y el CHECK acepta hash hexadecimal únicamente
para directory con posición válida y contexto no nulo. No altera deduplicación,
exclusión del dueño, reporte ni presupuesto del ranking.
Migración `20261005020557_AllowRankingActivityVersions`, generada con EF local;
Designer automático explica el tamaño de esta unidad. Down falla explícitamente
mientras existan eventos con hash; no trunca ni descarta atribución para revertir.
Rollback de aplicación: revertir uso de tokens de ranking y conservar esquema
compatible; downgrade solo después de retención/purga de sus eventos.
GREEN **9/9 HTTP/PostGIS**, cero omitidas, con
`.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Http_pages_retries|FullyQualifiedName~RankingActivitySchemaTests|FullyQualifiedName~BusinessActivityTests|FullyQualifiedName~RankingStorageSchemaTests'`.
Incluye impresión/visita/contacto duplicados que conservan tres eventos, versión
real/posición, restricciones inválidas, bloqueo de downgrade sin pérdida y
Down/Up posterior conservando actividad anterior. EF sin cambios pendientes.

Unidad de actividad: `bcf67d0` (452 líneas de Designer generadas por EF).

## Presentación y navegación real

`/revista` usa guard server-side y noindex; enlace de acceso solo con habilitación
local. Zona/rubro obligatorios, acción explícita de creación, tarjetas compartidas
con guía y medición original, foco en resultados, carga, huecos y errores con
acciones de recuperación. 404/403 ofrecen guía; 400/409/410 actualización explícita;
429/503/red reintentan identidad/cursor. No se muestran IDs/hash ni datos internos.
**83/83 frontend**, `npm test`; `npm run lint` y `npm run build` correctos.
Se corrigieron tipos de retorno de navegación detectados por build; no se omitieron
tests del chequeo TypeScript. Pruebas de UI agregadas junto a implementación,
sin atribuir RED: selección explícita bajo StrictMode, tarjetas/atribución,
página vacía intermedia, estados 429/503/502/410/404, cambio de filtros y rubros.

Playwright MCP contra Next dev/API/PostGIS temporal: 20/20/5 y 45 únicos,
atrás/recarga y perfil→atrás conservan página 2 mediante relectura. Retirar un
negocio desde el fixture reduce página 2 a 19, mantiene hueco y permite avanzar.
Expirar snapshots en base produce 410 sin tarjetas ni regeneración automática;
actualizar crea una nueva consulta. 429 inyectado en navegador seguido de
reintento real confirma requestId idéntico y recuperación. Sin errores de página
ni fallos de red inesperados tras corregir actividad. Los 410/429 inducidos generan
las entradas de consola esperadas. Escritorio 1440px y móvil 390/320px sin overflow.
Capturas inspeccionadas: `.tools/ranking-preview/desktop-initial.png`,
`desktop-results.png`, `mobile-controls.png`, `mobile-results.png`, `mobile-expired.png`.
Rollback UI: ruta/componente/CSS revista, enlace condicional de layout, extracción
BusinessCard (reintegrable a Explore), pruebas y pequeños ajustes del hook;
proxy y API pueden conservarse cerrados. Demo intacta.

Unidad de presentación: `63957a2`.

## Cierre integrado

`node App/tools/verify-ranking-preview.mjs --serve` pasó con código 0 al cerrar:
base temporal nueva, migraciones reales, 45 negocios por API, Next producción
con flags activos retorna 404 en página/POST/GET. Next dev retorna 20/20/5 y
45 únicos; reread/replay conserva una oportunidad. Impresión/visita/contacto
enviados dos veces dejan exactamente tres eventos con hash de política.
Los recorridos posteriores de Playwright terminan con cuatro oportunidades:
una del harness, una del navegador, actualización tras 410 y recuperación del
intento 429. Navegación/perfil/recarga no agregan oportunidades. Quedan 10 eventos
observados con hash tras el recorrido, separados del ledger modelado.
Procesos propios detenidos y base temporal eliminada al finalizar; capturas y
metadatos locales en `.tools/ranking-preview/`. Logs persistidos con cursor redactado.
El harness es repetible sin navegador; --serve permite revisión visual manual/MCP.

Regresión final: **101/101 API/HTTP/PostGIS**, cero omitidas, 29 s, mediante
`.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Marketplace.Api.Tests'`.
**83/83 frontend**, `npm test`; `npm run lint` sin advertencias y `npm run build`
correcto con `/revista` dinámico. BLL/evaluación 99/99 conserva evidencia previa:
no cambió su implementación en este incremento. `git diff --check` correcto.
Cinco unidades locales sobre `37cad23`, sin push/PR. Archivado y sincronizado con
especificaciones, contrato y continuidad. Demo y borrador/anotaciones intactos.
Rollback del cierre: harness y documentos; para revertir toda la funcionalidad,
deshabilitar primero flags y seguir las fronteras anteriores. Conservar migración
de actividad mientras existan hashes evita pérdida de atribución.
