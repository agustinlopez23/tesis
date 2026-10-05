# Verificación — 05/10/2026

Pruebas iniciales HTTP: 404 esperado antes de implementar. Durante implementación
la proyección de DTO antes de filtrar produjo 500: corregido filtrando primero.
Guard Down: prueba roja al permitir revertir datos; agregada excepción 23514.
Proxy rojo 404 → verde. Editor probado contra catálogo privado real (array):
falló por asumir items, corregido; pérdida de respuesta conserva URL/cuerpo.

Casos backend: revisión distinta de pago, roles/propiedad/versiones, imagen
congelada de promoción con plan gratuito, elegibilidad por stock/plan, eventos
deduplicados/titular excluido, solicitud atribuida/replay tras pausa, tres cupos
y cuatro pagos concurrentes entre negocios, modelo y rollback. Suite completa
inicial: 137 aprobadas/1 fallida por prueba histórica leyendo una columna posterior
al downgrade. Se proyecta solo ID en ese punto; prueba específica volvió verde.

Frontend: 114/114; lint y compilación Next correctos. Integración real mediante
`node App/tools/verify-catalog.mjs --campaigns`: catálogo, pedidos, ventas, métricas,
búsqueda y revisión/pago/eventos/atribución/pausa/reanudación correctos. Se usaron
cuentas y pago ficticios exclusivamente en base generada, eliminada al finalizar.

Playwright: anuncio → catálogo → solicitud guardada, resultados del titular con
dos solicitudes atribuibles, nueva campaña desde editor y aprobación administrativa
sin activar pago. 1440/390/320 sin desbordamiento horizontal; captura móvil
inspeccionada. Artefactos locales ignorados en `.tools/campaigns/`. No se contactó
WhatsApp ni se modificó, migró o reinició la demo.

Repetición final completa tras corregir la prueba histórica: **138/138 API**, sin
omitidas. Frontend **114/114**. No cambios pendientes del modelo EF.
