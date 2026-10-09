# Resultados comerciales privados

GET `/businesses/{id}/commerce?days=7|30` MUST requerir sesión del titular y
responder no-store. Días no admitidos producen 400, sesión ausente 401 y negocio
ajeno/inexistente 404. Lectura histórica disponible aunque venza el plan.

El período empieza a medianoche de Córdoba del primer día incluido y termina
al consultar. MUST comunicar from/asOf/timeZone y usar consultas agregadas dentro
de una transacción RepeatableRead para evitar resultados parcialmente actualizados.

`orders`: solicitudes creadas en ese intervalo. `contactsReceived`: contactos
confirmados de esas solicitudes. `converted`: solicitudes de esa cohorte con venta
vigente. `conversionRate` es converted/orders o null si no existen solicitudes.
Una venta anulada no cuenta como conversión. Es seguimiento hasta ahora, no un
experimento de ventana cerrada ni una tasa sobre todas las ventas del período.

`activeSales` y `voidedSales`: ventas según su fecha de registro dentro del
intervalo. `amounts`: solo ventas vigentes, separado en requested/conversation/
direct y por moneda. Puede incluir ventas vinculadas a solicitudes anteriores.
No mezclar monedas ni atribuir ventas direct a contactos de la plataforma.

El panel MUST cargar bajo demanda, permitir 7/30 días y reintento, retirar cifras
obsoletas al fallar y explicar que son datos reportados, no pagos verificados.
