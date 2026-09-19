# Verificación — 19/09/2026

## Investigación aplicada

Google Business Profile, Airbnb, Etsy, GA4 e IAB documentan separación entre exposición, interés y acciones, contexto y períodos. Decisiones y fuentes: `Doc/analytics-research-2026-09-19.md`. Se adaptan patrones; no se afirma paridad de producto ni certificación de medición.

## Evidencia automatizada

- Backend: **46/46**, con bases PostgreSQL/PostGIS temporales y migraciones reales. Incluye ingesta concurrente, permisos, propio dueño, borradores/suspensión, comprobantes alterados/caducados, límite de cuerpo, posición/origen/versión del servidor, retención y día local.
- Frontend: **56/56**. Incluye umbral/tiempo continuo, pestaña oculta, desmontaje, recarga, caducidad de sesión, almacenamiento inaccesible, errores de red, atribución, estados y período del panel, proxy acotado.
- Lint y compilación de producción Next aprobados. Backend compila sin advertencias/errores. No se agregaron paquetes.
- TDD inicial: 2 fallos API por contrato ausente; 5 fallos de comportamiento UI después de crear contratos vacíos y 1 proxy; luego verdes. Tarea de retención tuvo rojo explícito antes de registrarse. Casos adicionales de tiempo/contexto se añadieron después: no se presentan como test-first.

## Playwright MCP

- Visitante en contexto separado sin cookie: búsqueda del café de demostración, tarjeta visible >1 s, recarga, perfil y clic de contacto. El enlace se activó con navegación externa interceptada; no se abrió conversación ni se envió mensaje.
- Resultado observado en API/panel del dueño: **1 aparición, 1 visita, 1 clic**. La recarga no agrega aparición. Datos de prueba del negocio ficticio, no clientes reales.
- Períodos 7/30 y 30 barras/días; selección de métrica, detalle y tabla.
- Respuesta 503 simulada: alerta, sin tarjetas de cero; reintento recupera 1/1/1.
- Anchos reales documento: 305/305, 375/375, 753/753, 1425/1425 (clientWidth/scrollWidth, viewports 320/390/768/1440). Sin desborde horizontal.
- La tarea de retención ejecutó al arrancar la demo y dejó confirmación en log sin datos de visitantes.
- Se corrigió en navegador el etiquetado de los selects; las etiquetas explícitas evitan que opciones formen parte del nombre accesible.
- Capturas locales ignoradas: `.playwright-mcp/actividad-escritorio.png`, `actividad-movil.png`, `actividad-vacia-escritorio.png`.

## Límites

UUID por pestaña no es persona única; eventos del navegador pueden bloquearse o falsificarse. Comprobante demuestra contexto emitido, no visibilidad humana. El límite global se comparte por instancia. Sin cola persistente ni reintentos ilimitados; comprobar pérdidas antes de un piloto. No medir por más de 30 minutos con un comprobante viejo sin nueva carga.

Primera impresión por sesión/negocio no equivale a cada oportunidad de cada consulta. Se almacena posición/versión para diagnóstico; el experimento de equidad aún requiere candidatos y juicios independientes. No hay porcentajes de conversión ni ventas atribuidas. La retención operativa de logs/backups y las claves compartidas entre instancias deben configurarse antes de despliegue.
