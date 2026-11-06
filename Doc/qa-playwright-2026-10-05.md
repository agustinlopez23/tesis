# Recorrido comercial con Playwright — 05/10/2026

Se probaron los recorridos de consumidor, responsable y administración sobre
Next de producción, API y PostgreSQL/PostGIS temporales. Se corrigieron cuatro
problemas y se repitieron los casos afectados. **123/123 pruebas frontend**, lint
y build correctos. No se modificó la demo ni se enviaron mensajes o pagos reales.

## Correcciones comprobadas

| Problema reproducido | Resultado después del cambio | Commit |
| --- | --- | --- |
| Inventario, solicitud y métricas abiertos conservaban datos anteriores a una venta/anulación | Se consultan otra vez tras guardar; stock 4 → 2 → 3 y solicitud cancelada, sin recarga manual | `fe84d4b` |
| Actualizar catálogo conservaba el precio/versión viejos de la selección y repetía 409 | Muestra precio vigente, avisa y permite confirmar; prueba 13000 → 13500 con 201 final | `1453aae` |
| Sesión vencida al ajustar stock daba un error genérico | Pide ingresar y conserva la operación; el mismo intento se recupera tras iniciar sesión | `1979c05` |
| Contraseña incorrecta daba un error genérico | Sugiere revisar correo y contraseña sin revelar existencia de cuenta | `1979c05` |

La selección vacía queda sin botón de confirmación habilitado. Actualizar catálogo
no altera intentos pendientes ni solicitudes confirmadas. La sincronización de
paneles se limita al mismo negocio y pestaña; otros clientes necesitan actualizar.

## Cobertura del navegador

| Recorrido | Comprobación |
| --- | --- |
| Registro y acceso | Alta por formulario, ingreso válido/inválido, mostrar contraseña, cierre persistente tras recarga |
| Alta de negocio | Nombre, rubro, dirección, WhatsApp, coordenadas y publicación desde formulario |
| Planes | Catálogo restringido en Presencia; administración concede Impulso; revocación oculta catálogo y preserva perfil e historial |
| Catálogo y fotos | Alta/edición de producto, cambio de precio, foto de producto y galería; imágenes públicas cargadas |
| Descubrimiento | Buscar productos y abrir negocio; búsqueda sin resultados y recuperación; zona Centro; ubicación fallida y luego concedida; navegación atrás |
| Solicitudes | Selección/cantidades, rechazo por stock insuficiente, confirmación, referencia, seguimiento privado y preparación de otra solicitud |
| Fallos y reintentos | POST confirmado en servidor con respuesta abortada; recarga recupera; otro caso reintenta tras actualizar catálogo con cuerpo e ID idénticos |
| Atención y ventas | Marcar contacto y estado, registrar venta vinculada con precio final, stock descontado y venta presencial |
| Anulación | Motivo y devolución parcial explícita; stock devuelve solo una unidad y seguimiento pasa a cancelado |
| Inventario | Alta de saldo, ajuste, historial; producto nuevo aparece en inventario abierto; reintento autenticado conserva intención |
| Métricas | Resultados comerciales 7/30 días, ventas anuladas separadas, actualización tras venta/anulación; apertura de actividad y resultados de campaña |
| Campañas | Preparar promoción con imagen, aprobar, registrar confirmación ficticia de pago y ver promoción pública; pausa del titular |
| Elegibilidad | Campaña de producto bloqueada con stock cero y reactivada al reponer; producto público sin stock no se puede agregar |
| Autorización | Administración rechaza anónimo y titular sin rol; formulario privado tras sesión vencida exige ingresar |
| Error de lectura | 503 simulado al cargar la guía; Reintentar recupera los resultados reales |
| Asistencia opcional | Proveedor deshabilitado: aviso y creación manual de producto funcionando |
| Móvil y teclado | Vistas de 1440/390/320 px sin desborde horizontal en guía, promoción y paneles comprobados; Tab/Enter en «Saltar al contenido» enfoca main |

No hubo excepciones JavaScript capturadas durante la repetición de regresiones.
Los errores HTTP observados corresponden a casos provocados de autenticación,
autorización, conflictos, catálogo oculto y 503 simulado. Los timeouts de selectores
del recorrido se resolvieron inspeccionando las etiquetas reales; no se contaron
como fallas de la aplicación ni como pruebas aprobadas sin repetir la comprobación.

## Reproducir y revisar evidencia

Desde `App/frontend`: `npm test`, `npm run lint`, `npm run build`.
Resultado final: 26 archivos / 123 pruebas aprobadas; lint y build salen con 0.
Las regresiones se agregaron con fallo previo.

Con API Release y PostGIS preparados según [App/README](../App/README.md), desde la
raíz ejecutar `node App/tools/verify-catalog.mjs --campaigns --serve`. El harness
crea una base aislada y expone Next en 3200/API en 5280. Las aserciones HTTP reales
de catálogo, pedidos, ventas y campañas pasaron. Abre `http://localhost:3200` con
Playwright y recorre la tabla; cuentas efímeras en `.tools/campaigns/session.json`.
No copiar cookies, contraseñas ni claves privadas al informe. El harness no ejecuta
por sí solo las interacciones Playwright: este recorrido fue dirigido mediante MCP.

El archivo `stopFile` de esa sesión permite finalizar antes de los 20 minutos;
el proceso detiene sus servicios y elimina su base. Las tres sesiones de esta
revisión terminaron correctamente. No se recompila Next mientras el harness sirve.

Artefactos locales ignorados en `.tools/campaigns/`: `result.json`,
`sales-result.json`, `campaigns-result.json` y `logs.json` corresponden a la última
sesión. Capturas conservadas: `qa-explore-320.png`, `qa-promotion-320.png`,
`qa-order-fixed-mobile.png` y `qa-login-fixed-320.png`. Para capturas completas,
desplazar a inicio antes de capturar evita artefactos de elementos fijos.

## Límites

Se usó el navegador del MCP con distintos anchos, sin matriz Firefox/WebKit,
dispositivos físicos, auditoría completa de accesibilidad ni prueba de carga.
WhatsApp se comprobó por su enlace/resumen sin enviar mensajes. La confirmación
administrativa de pago fue ficticia en la base temporal. IA real sigue pendiente
de configurar y evaluar; el ranking experimental no se activó. No se repitieron
las suites API/BLL completas porque esta revisión solo cambió el frontend; sí se
repitió el harness de integración contra el servidor real.
