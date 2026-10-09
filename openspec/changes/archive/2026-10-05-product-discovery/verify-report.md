# Verificación — 05/10/2026

Base `ef20283`, rama `feat/product-discovery`. Dos casos API iniciales fallaron
con 404 y el componente con import inexistente. Casos finales cubren paginación
20/2 sin duplicados, búsqueda literal/sin acentos, ubicación/radio, elegibilidad
por plan, estado/titular/stock y conservación de filtros/reintentos de interfaz.

Final: **131/131 API/HTTP/PostGIS**, **109/109 frontend**, lint/build correctos.
Comandos habituales: `.tools/test-publication-history.ps1 -Filter
'FullyQualifiedName~Marketplace.Api.Tests'`, `npm test`, `npm run lint`, `npm run build`.

Una ejecución simultánea con build falló porque PostgreSQL terminó un proceso
con código 2 y entró en recuperación a las 12:05–12:06 UTC. No se interpreta como
fallo funcional ni se afirma una causa raíz no comprobada. Se acotó xUnit a dos
clases concurrentes; las concurrencias internas de ventas/ranking se conservan.
La repetición completa pasó 131/131. Se verificó y eliminó la base temporal de esa
ejecución interrumpida, sin conexiones y con fecha de recuperación coincidente.

Harness `node App/tools/verify-catalog.mjs --sales --serve`: consulta cercana devuelve
producto/distancia 0, consulta lejana vacía y proxy no-store. Playwright: entrar
desde Explorar, buscar cafe, elegir Centro, recargar conservando filtros y abrir
catálogo para solicitar. 1440/390/320 sin desbordamiento; captura móvil inspeccionada
en `.tools/sales/discovery-mobile.png`. Servidores y base temporal eliminados.

Sin migración ni modificación del ranking, sin push/PR/despliegue. Reversión:
quitar ProductDiscovery en API/BLL/DAL/DTOs e IoC, página/componente/enlace/proxy y
pruebas del incremento. La configuración de concurrencia de pruebas puede quedarse.
Separar API, interfaz y estabilidad del runner si el presupuesto supera 400 líneas.
