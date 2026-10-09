# Verificación — 05/10/2026

Incremento completado en `feat/product-orders`, desde `58a0efd`. Solicitud desde
catálogo, resumen para WhatsApp, enlace privado y atención/cancelación por titular.
No se enviaron mensajes. Migraciones y operaciones solo sobre bases temporales.

## Evidencia automatizada

| Comprobación | Resultado |
| --- | --- |
| API/HTTP/PostGIS: `.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Marketplace.Api.Tests'` | 120/120; sin omitidas. |
| Frontend: `npm test` | 100/100 en 18 archivos. |
| `npm run lint` y `npm run build` | Correctos; ruta `/pedido/[id]` compilada. |
| `node App/tools/verify-catalog.mjs --orders --serve` | Proxy Next production, API real y PostgreSQL aislados; aserciones correctas. |
| Esquema EF | Sin cambios pendientes; downgrade poblado rechazado y vacío reversible. |

TDD: seis casos iniciales de API fallaron con 404. Los primeros componentes
fallaron por imports inexistentes. Casos adicionales reprodujeron 500 ante cuerpo
excesivo, downgrade destructivo y resumen anterior al cambiar clave, corregidos
antes de cerrar. El último caso reprodujo bloqueo de selección después de vencer
un intento confirmado; la suite frontend final pasó con 100 pruebas.

API cubre snapshots, replay concurrente, contenido conflictivo, clave incorrecta,
caducidad, aislamiento entre titulares, plan/publicación/suspensión, productos
ajenos o modificados, cantidades y monedas, atención/contacto/cancelación después
de vencer el plan y una sola entrada de auditoría por cambio efectivo.

## Integración y navegador

El harness verificó creación y replay, precio histórico tras editar catálogo,
protección de clave, búsqueda por referencia, revocación de plan, rechazo de nuevas
solicitudes, recuperación y gestión de existentes, contacto recibido y cancelación.

Playwright con API real: selección de dos unidades, confirmación y recuperación
tras interceptar POST, dejar que el servidor lo procese y abortar la respuesta.
Recargar recuperó el mismo UUID mediante GET; base final con dos solicitudes y
dos referencias distintas (una del harness, otra del navegador), tres cambios
auditados. No hubo duplicación por pérdida de respuesta.

El titular abrió la solicitud, marcó atención y contacto recibido; el enlace
privado mostró ambos cambios. Quitar la clave retiró el resumen y restaurarla lo
recuperó. WhatsApp excluyó la clave. Portapapeles rechazado mostró texto copiable.
Vistas a 1440, 390 y 320 px sin desbordamiento horizontal. Capturas locales ignoradas
en `.tools/orders/`: checkout, seguimiento y panel del titular.

Tras ese recorrido se agregó la limpieza local del intento confirmado vencido,
validada con prueba específica y suite/build/lint finales. El recorrido completo
de navegador corresponde al código previo a esa limpieza local.
Después del cambio, `node App/tools/verify-catalog.mjs --orders` volvió a pasar
con el build final; cerró servidores y eliminó su nueva base temporal.

El proceso `--serve` terminó correctamente, cerró sus servidores y eliminó la
base temporal. No se migró/reinició la demo ni se realizó push, PR o despliegue.

## Límites y siguiente paso

Las cuotas son por proceso, el enlace dura 30 días y los pedidos permanecen como
historia operativa sin purga automática. La interfaz consulta cambios al actualizar;
no hay notificaciones ni confirmación automática desde WhatsApp. Siguiente unidad:
registrar una venta vinculada a la solicitud y actualizar stock atómicamente.

## Unidades locales de revisión

Estrategia: commits dependientes en una rama local; sin ramas de PR ni publicación.
Una pasada de separación mantuvo migraciones y contrato API con sus pruebas.

| Orden / commit | Alcance | Líneas añadidas + eliminadas |
| --- | --- | --- |
| 1 · `ae0a7f7` | Persistencia y protección de rollback | 1517 |
| 2 · `eb36b40` | Creación, privacidad y transiciones API | 489 |
| 3 · `bf1cb3e` | Proxy acotado y privado | 51 |
| 4 · `111db55` | Selección y recuperación del cliente | 370 |
| 5 · `49bf028` | Panel del titular | 150 |
| 6 · commit de este informe | Harness, evidencia y continuidad | Consultar `git show --stat`. |

Dependencias: almacenamiento → API → proxy → cliente → panel → cierre.
Presupuesto de revisión: 400 líneas. Para una futura PR se recomienda
`size:exception` en almacenamiento (1276 líneas de migración/snapshot generados)
y API (contrato transaccional, autorización y pruebas HTTP inseparables en esta
división). No se comprimió código ni se omitieron pruebas para ajustar tamaño.

La prueba de esquema verifica la unidad 1; nueve casos HTTP verifican la 2;
dos pruebas de proxy la 3, seis de selección/seguimiento la 4 y dos del panel la 5.
Todas están incluidas en las suites finales. El harness cubre las fronteras
Next/API/PostgreSQL; el recorrido de navegador agrega recuperación y operación.

Reversión por dependencias en orden inverso: quitar panel y su montaje, luego
selección/checkout/seguimiento y sus estilos, después rutas del proxy, endpoints/
registro IoC/servicio/repositorio de órdenes. Conservar esquema poblado e historia;
solo el esquema vacío admite Down. Revertir el cierre quita `--orders` del harness
y las actualizaciones documentales, sin cambiar el producto.
