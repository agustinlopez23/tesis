# Solicitudes de productos

## Creación y recuperación

El consumidor MUST poder confirmar una selección de un único negocio publicado,
con titular habilitado y plan vigente. Cada producto MUST estar publicado,
disponible y conservar la versión revisada. La transacción comparte el bloqueo
del negocio con catálogo y planes, y evalúa vigencia después de obtenerlo.

- Nombre o alias de 1–80 caracteres, sin controles; sin cuenta, teléfono ni correo.
- Entre 1 y 20 productos distintos; cantidades enteras de 1 a 99.
- UUID por intento, clave hexadecimal aleatoria de 256 bits y versiones esperadas.
- Persistir nombres, precios, monedas, cantidades, negocio y WhatsApp históricos.
- Separar totales ARS/USD. No reservar stock ni registrar ventas.

Un intento idéntico MUST devolver la misma solicitud, incluso después de retirar
el producto o vencer el plan. Cambiar el contenido con el mismo UUID produce 409;
otra clave o negocio produce 404. La recuperación no renueva los 30 días de acceso.
El navegador conserva el intento antes de POST en sessionStorage; ante respuesta
perdida permite recuperar/reintentar sin cambiar identidad. Al recargar consulta
el estado sin volver a enviar POST. Un intento confirmado y vencido libera la
selección para una nueva solicitud.

Al actualizar el catálogo, una selección todavía editable MUST incorporar precio,
moneda y versión de los productos seleccionados presentes en la respuesta, y
retirar los que ahora estén no disponibles. Mostrar aviso para revisar antes de
confirmar; no enviar una solicitud automáticamente ni permitir confirmar vacía.
La ausencia en una página o búsqueda no implica retirada del producto. Intentos
pendientes de respuesta y solicitudes guardadas MUST conservar su contenido.

Escenario: el precio cambia después de seleccionar y la API rechaza con 409;
actualizar catálogo muestra el importe vigente y la siguiente confirmación envía
su versión actual. Si se perdió una respuesta, actualizar y reintentar conserva
la identidad, clave, cantidades y versiones del intento original.

## Acceso y contacto

La referencia legible MUST ser única y no otorgar acceso. La clave se conserva
solo como SHA-256 en base de datos. El enlace `/pedido/{id}#clave=…` envía la clave
en `X-Order-Key` al consultar, nunca en query ni en el resumen de WhatsApp.
Ausencia, clave incorrecta o vencimiento MUST devolver 404. Respuestas no-store;
página noindex/nofollow y no-referrer. Cambiar el fragmento revalida y retira el
resumen anterior mientras carga o ante error.

El consumidor puede abrir WhatsApp o copiar el resumen y guardar su enlace
privado. Abrir WhatsApp MUST NOT afirmar envío, lectura, contacto recibido ni
venta. Cancelación, despublicación o suspensión deshabilitan contacto desde el
resumen actualizado. La consulta requiere actualizar para conocer cambios.

## Gestión del negocio

Solo el titular autenticado MUST listar, leer y modificar sus solicitudes. Puede
gestionarlas aunque venza el plan, con páginas de 20, prefijo de referencia y
filtro de estado. Escrituras requieren comprobación de origen y cabecera CSRF.

Estados: `pending` → `in_attention` → `cancelled`, o cancelación desde `pending`.
No se permite retroceder ni reabrir. El titular puede marcar contacto recibido
una vez. La actualización MUST comprobar versión, rechazar conflictos con 409
y registrar actor, fecha, versión y transición en la misma transacción. Repetir
una modificación con versión obsoleta no duplica cambios.

## Límites operativos

JSON máximo 16 KiB; exceso 413, contenido inválido 400. Cuotas globales por
instancia: 120 creaciones y 600 lecturas privadas de cliente por minuto; exceso
429 sin caché. No sustituyen controles distribuidos. Los datos operativos quedan
para gestión del negocio; expirar el enlace no elimina pedidos ni historial.
La migración MUST impedir downgrade si existen solicitudes.

Fuera del incremento: conversión a venta, stock, cancelación por cliente,
notificaciones, atribución de campañas y envío/lectura automáticos de WhatsApp.
