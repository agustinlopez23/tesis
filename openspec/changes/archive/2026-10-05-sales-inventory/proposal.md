# Ventas e inventario trazables

Registrar una venta reportada por el negocio y descontar stock en una sola
transacción, preservando la solicitud original. Incluye inventario por unidades,
saldo inicial y ajustes auditados; venta desde solicitud o presencial con origen
propio; líneas finales con precios y procedencia; anulación auditada con devolución
explícita por producto. Una venta por solicitud, sin conversiones parciales.

Inventario no cargado es desconocido, nunca se migra a cero ficticio. Para vender
se requiere inicializar cada saldo. Solicitar no reserva; saldos conocidos deben
alcanzar al solicitar y se revalidan al vender. La disponibilidad declarada sigue
siendo requisito de solicitud. Productos archivados conservan inventario e historia.

Resolver solicitudes existentes, ajustar existencias y anular ventas sigue
permitido al vencer el plan; ventas presenciales nuevas requieren Impulso.
Una anulación deja la solicitud cancelada, no la reabre ni repone automáticamente.
La devolución registrada no puede exceder lo vendido. No incluye reembolso/pagos,
contabilidad fiscal, reservas, edición destructiva ni automatización de WhatsApp.

Aceptación: aislamiento por titular, stock nunca negativo, conflicto sin cambios
parciales, reintentos idempotentes, dos ventas por últimas unidades con un solo
éxito, desglose monetario por moneda y origen, y UI recuperable. Pruebas sobre
PostGIS temporal y navegador. Sin dependencias nuevas ni cambios a la demo.

Usar commits por unidades verificables según work-unit-commits; separar esquema,
operaciones HTTP, interfaz e integración sin dividir migración generada. Si una
unidad cohesiva excede 400 líneas, documentar tamaño y recomendación de excepción
para una futura revisión. No se crean PRs ni se publica este incremento.
