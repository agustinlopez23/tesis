# Mostrar Cerca en siete minutos

La demo está en http://localhost:3000. Permite mostrar descubrimiento de productos,
solicitudes y gestión comercial con datos ficticios. El recorrido siguiente usa
una venta de ejemplo ya registrada; no requiere enviar mensajes ni realizar pagos.

## Antes de presentar

Abrir `/estado` y comprobar «API disponible». Si los servicios están detenidos,
seguir [el procedimiento de inicio](demo-local-2026-10-05.md). Las credenciales de
comerciante y administrador están en `.tools/seed-demo/credentials.json`; usarlas
para ingresar sin proyectar ese archivo. Preparar una ventana privada para el
cliente y otra para la gestión del negocio.

## Recorrido

| Tiempo | Acción | Qué demuestra |
| --- | --- | --- |
| 0–1 min | Abrir `/explorar`, elegir Centro y entrar a «Buscar productos entre negocios» | Descubrimiento por cercanía y rubro; datos de negocios existentes y de ejemplo |
| 1–2 min | Abrir «Demo · Almacén de la Plaza», agregar arroz y revisar cantidad y precio | Selección sin cuenta de cliente; aceite agotado y conserva no disponible no se pueden agregar |
| 2–3 min | Explicar la confirmación y el resumen de WhatsApp sin enviar mensajes | Primero se guarda la solicitud; el resumen tiene referencia y el seguimiento usa un enlace privado |
| 3–4 min | Ingresar como comerciante, editar el almacén y abrir la solicitud `CE-FFFDC9F41FD5` | Solicitud de «Demo · Cliente de ejemplo», contacto marcado y estado «Convertida en venta» |
| 4–5 min | Abrir Inventario y Ventas reportadas | Dos arroces solicitados a $2.300; venta acordada a $2.200 cada uno: $4.400 finales y saldo 28 |
| 5–6 min | Abrir Resultados comerciales | Una venta vigente; el importe final se mantiene separado del presupuesto inicial de $4.600 |
| 6–7 min | Ingresar como administrador y abrir `/administracion/planes`; mostrar también el editor de campañas | Planes con vigencia y publicidad con revisión/pago manual; ninguna acción cobra dinero |

El estado indicado corresponde a la comprobación del 05/10/2026. Si se registran
más operaciones, los saldos y resultados cambian. El seed conserva esos cambios;
ejecutarlo otra vez no devuelve el stock a 30 ni elimina la venta de ejemplo.
La selección del cliente puede dejarse sin confirmar para repetir la presentación
sin agregar pedidos. Si se confirma otra solicitud, quedará en el historial.

## Explicaciones que conviene mantener claras

- Una solicitud no reserva unidades. El descuento ocurre al registrar la venta.
- Abrir WhatsApp no acredita envío, pago ni compra. El contacto es una declaración
  del negocio; la venta de este recorrido es ficticia.
- ARS y USD tienen totales separados. En «Demo · Tienda Alberdi» hay un bolso en USD.
- La asistencia de redacción está preparada y desactivada. Se puede cargar el
  catálogo manualmente; una sugerencia de IA requeriría revisión antes de guardar.
- El ranking experimental requiere evaluación humana. La búsqueda disponible usa
  orden alfabético o distancia y no demuestra todavía la hipótesis de la tesis.

## Evidencia del recorrido

Playwright completó selección, confirmación, atención, contacto, venta e inventario
en la demo. La solicitud conserva $4.600; la venta registra $4.400 y el arroz pasó
de 30 a 28. Las métricas y el seguimiento reflejan la conversión. Al repetir el
seed permanecieron 6 negocios, 12 productos, 1 solicitud, 1 venta y 13 movimientos,
sin excepciones JavaScript. Resumen local: `.tools/seed-demo/flow-result.json`.

No se publican contraseñas ni enlaces privados de seguimiento en este guion.
