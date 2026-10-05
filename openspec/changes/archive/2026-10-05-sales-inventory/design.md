# Diseño y contratos

El saldo se separa del producto para conservar «desconocido» sin migrar existencias
ficticias. `inventory_balances` tiene producto, cantidad y versión;
`inventory_movements` registra operación/producto, delta, saldo, motivo, actor,
fecha y venta opcional. `sales` conserva identidad y anulación;
`sale_lines` guarda precio, origen y devolución. Relaciones restrictivas y checks
protegen historia, cantidades y monedas. Índice único limita una venta por solicitud.

El servicio normaliza cuerpo y calcula SHA-256 por intento. El repositorio bloquea
el negocio como catálogo/planes/pedidos. Lee replay antes de validar nuevas reglas,
verifica todas las líneas antes de mutar y confirma en una transacción. Precios
finales son declarados por el titular, no se reemplazan con el precio del catálogo.
Cada modificación de estado de solicitud conserva auditoría versionada.

| Ruta del titular bajo `/businesses/{business}` | Uso |
| --- | --- |
| GET `/inventory` | Productos y saldos; quantity null/version 0 si no se cargó. |
| GET `/inventory/movements?page=1&productId=…` | Historial de 20 con hasNextPage. |
| PUT `/inventory/adjustments/{id}` | productId, delta, version, reason. |
| GET `/sales?page=1&orderId=…` | Ventas históricas de 20. |
| GET `/sales/{id}` | Venta y desglose por origen/moneda. |
| PUT `/sales/{id}` | orderId/orderVersion opcionales, lines con productId/quantity/unitPrice/currency/origin. |
| PUT `/sales/{id}/void` | operationId, reason, returns con productId/quantity. |

201 para venta nueva; 200 para replay/ajuste/anulación. 400 validación, 401 sesión,
403 origen o plan presencial, 404 aislamiento, 409 conflicto, 413 cuerpo excesivo.
Proxy Next acota rutas, query, cuerpo y cabeceras; no-store también en errores.

UI separa inventario, venta desde solicitud y registro presencial. Campos se
bloquean mientras un intento no tiene respuesta; reabrir la misma operación
recupera su identidad y permite reintentar. Rechazos definidos liberan edición.
Después de conflicto de versión, actualizar y volver a elegir producto/solicitud.
La lista de ventas identifica anuladas y muestra sus importes como originales
anulados; nunca los suma como ingresos vigentes.

Reversión: retirar primero pantallas y proxy, luego API/IoC/servicio/repositorio y
lecturas de saldos en catálogo/pedidos. Mantener historia. La migración
`20261005114423_AddSalesInventory` solo permite Down sobre esquema vacío; no borrar
movimientos para conseguirlo. No se aplicó a la demo.
