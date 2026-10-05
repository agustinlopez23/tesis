# Ventas e inventario

El titular MUST poder cargar existencias reales, registrar una venta y corregirla
mediante anulación auditada. Cada operación requiere sesión y origen confiable;
las lecturas privadas y errores son no-store. Otro titular no puede acceder.

## Inventario

Un producto sin saldo MUST mostrarse «Sin cargar». La primera operación inicializa
el saldo; las siguientes aplican un delta entero con motivo de 3–300 caracteres,
actor y fecha. El saldo MUST permanecer entre 0 y 1.000.000, con versión creciente.
Una versión obsoleta o saldo insuficiente rechaza toda la operación con 409.
UUID repetido y cuerpo idéntico recuperan el resultado sin otro movimiento.

Una solicitud no reserva ni descuenta unidades. Si el saldo ya está cargado, debe
alcanzar la cantidad solicitada; cero muestra el producto como no disponible en
el catálogo público. El saldo desconocido conserva la disponibilidad declarada
del catálogo previo. Los saldos exactos se consultan solo desde el negocio.

## Registro de ventas

Una venta MUST conservar precios finales y cantidades aparte de la solicitud.
Admite 1–20 productos distintos del negocio, 1–99 unidades por línea, precio no
negativo con dos decimales y moneda ARS/USD. Cada producto debe tener saldo cargado.
La venta, descuentos y transición de solicitud se guardan en una transacción bajo
el bloqueo del negocio. Dos ventas por las últimas unidades no pueden producir
stock negativo. Cualquier línea insuficiente rechaza toda la venta.

Venta vinculada: solicitud pendiente/en atención y versión vigente; una venta por
solicitud, sin conversión parcial. Las líneas declaran `requested` o `conversation`;
`requested` exige que el producto figure en la solicitud original. Se permite
quitar, sustituir y agregar productos. Los importes MUST desglosarse por origen y
moneda. La solicitud pasa a `converted` con auditoría. La actualización común de
solicitudes no puede asignar o revertir ese estado.

Venta presencial: `orderId` nulo y origen `direct`, sin atribución inferida. Exige
plan vigente. Resolver una solicitud existente, ajustar inventario y anular sigue
permitido al vencer el plan. La autenticación de titulares suspendidos se rechaza.

La identidad y el cuerpo completo de cada operación se guardan en sessionStorage
antes de escribir. Reintentos idénticos MUST recuperar el resultado aun tras
recarga o pérdida de respuesta. Otro contenido con el mismo UUID da 409.

## Anulación y límites

Anular requiere UUID propio, motivo y cantidades efectivamente devueltas. Cero
devoluciones MUST conservar stock; devolver no puede superar lo vendido. Conservar
venta original, actor/fecha/motivo y cantidades devueltas; generar movimientos solo
por esas cantidades. La solicitud vinculada queda cancelada sin reapertura. La
anulación es definitiva; no se admite otra venta para esa solicitud.

JSON máximo 16 KiB. Listas de ventas/movimientos paginadas de 20 con orden estable;
filtros privados por solicitud/producto respectivamente. No hay pagos, reembolsos,
comprobantes fiscales, reservas ni afirmación de venta verificada por WhatsApp.
No se purga historia. Downgrade MUST rechazar saldos, movimientos o ventas existentes.

## Paneles abiertos del mismo negocio

Después de confirmar una venta, anulación o ajuste, los paneles abiertos en la
misma pestaña MUST consultar otra vez inventario, solicitudes, ventas, catálogo,
resultados comerciales y campañas del negocio afectado. Otro negocio no se
actualiza. Crear un producto MUST incorporarlo al inventario abierto sin recargar.

Escenario: con cuatro unidades y los paneles abiertos, vender dos muestra dos
unidades y una venta vigente; anular devolviendo una muestra tres unidades,
la solicitud cancelada y cero ventas vigentes. No requiere actualización manual.
Esta sincronización no ofrece notificaciones entre dispositivos o pestañas.
