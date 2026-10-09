# Diseño de solicitudes

La API guarda la solicitud antes de ofrecer el contacto. Esto permite recuperar
un resumen estable después de una respuesta perdida y que el negocio vea la
solicitud aun cuando el consumidor todavía no haya enviado WhatsApp.

## Persistencia y concurrencia

`customer_orders` contiene identidad, referencia única, hashes, nombre/alias,
datos históricos del negocio, vencimiento y estado versionado. `order_lines`
conserva el precio y la versión de cada producto. `order_changes` registra cambios
con actor, fecha y versión. Relaciones restrictivas evitan perder historia.
Los totales se calculan desde líneas inmutables y se agrupan por moneda.

BLL normaliza nombre y orden de líneas antes de calcular el hash del intento.
DAL abre transacción y bloquea el negocio con `FOR UPDATE`, igual que catálogo
y planes. Primero resuelve replay; después valida elegibilidad y versiones.
La restricción única cubre colisiones de referencia/UUID entre negocios.
Actualizar estado usa el mismo bloqueo y comparación de versión.

Migración `20261005111735_AddProductOrders`: esquema, restricciones e índices
generados con EF. Down rechaza una base con solicitudes; solo el esquema vacío
se puede revertir. Prueba PostgreSQL de rechazo con conservación y roundtrip vacío.

## Contrato HTTP

| Ruta | Uso |
| --- | --- |
| POST `/public/businesses/{id}/orders` | `requestId`, `accessKey`, `customerName`, `lines[{productId,quantity,expectedVersion}]`; 201 nueva, 200 replay. |
| GET `/public/orders/{id}` | Cabecera `X-Order-Key`; respuesta histórica con estado actual y vencimiento. |
| GET `/businesses/{business}/orders` | Titular; `page`, `reference` por prefijo y `status` opcionales. |
| GET `/businesses/{business}/orders/{id}` | Detalle del titular. |
| PUT `/businesses/{business}/orders/{id}` | `version`, `status`, `contactReceived`; respuesta actualizada. |

Origen de frontend y `X-Requested-With: marketplace` en escrituras. Proxy Next
con rutas/métodos permitidos, límite de cuerpo y filtros explícitos; solo reenvía
la clave al GET privado. Respuestas nunca incluyen hashes ni clave.

## Interfaz y recuperación

La selección vive en memoria; al confirmar se conserva el intento por negocio en
sessionStorage antes de enviar. Fallos de transporte mantienen identidad para
reintentar. Rechazos conocidos 400/403/409 habilitan revisar selección. Si cambió
un producto, actualizar catálogo y quitar/agregar el producto permite revisar
su precio nuevo antes de confirmar. No se reemplazan precios silenciosamente.

El enlace privado usa fragmento, excluido del resumen de WhatsApp. La página
revalida al cambiar clave, limpia resultados obsoletos y permite actualización
manual. El portapapeles tiene alternativa visible si copiar falla. El panel del
titular carga la lista bajo demanda, con filtros y controles de transición.

Guardar la fecha de vencimiento después de confirmar/recuperar permite liberar
intentos confirmados vencidos. Los intentos sin respuesta siguen conservándose
para evitar que un error de red produzca una segunda solicitud.

## Revisión y reversión

Revisar almacenamiento y su prueba, API y pruebas reales, transporte/seguimiento,
selección, panel del titular y finalmente integración/documentación. Los archivos
generados de EF permanecen íntegros. Si se publica una PR con esas migraciones,
recomendar `size:exception` por código generado; el contrato API y sus pruebas
también constituyen una unidad cohesiva que puede superar 400 líneas.
No hay PR creada en este incremento. No se aplicó migración a la demo.
Revertir interfaz/API no autoriza eliminar historia; Down poblado está bloqueado.
