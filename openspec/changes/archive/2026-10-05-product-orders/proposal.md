# Solicitudes por producto y WhatsApp

Completar selección de un negocio, confirmación con precios históricos, referencia
única, mensaje recuperable y seguimiento privado. El negocio recibe la solicitud
en su panel antes de cualquier conversación y puede atender/cancelar o registrar
que recibió el contacto. Crear/abrir WhatsApp no confirma venta ni descuenta stock.

Decisiones del incremento: nombre o alias (1–80), sin teléfono/correo del cliente;
clave aleatoria de 256 bits en fragmento de enlace, enviada a API en cabecera y
guardada solo como hash. Acceso del cliente por 30 días; pedidos e historial
permanecen para gestión del negocio. La referencia no autoriza lectura.
El resumen de WhatsApp excluye la clave y el enlace privado.

Máximo 20 productos distintos, cantidades enteras 1–99. API valida negocio,
plan, publicación, disponibilidad y versión bajo el mismo bloqueo transaccional
del catálogo. Un UUID e identidad de intento se conservan para reintentar incluso
después de perder la respuesta. Cambios de contenido con el mismo UUID dan 409.
Los totales se separan por moneda. Origen de este recorrido: catálogo del perfil.

Fuera del incremento: ventas, stock, cancelación por cliente, cuentas del consumidor,
campañas, métricas de conversión, notificaciones y lectura/envío automático de WhatsApp.
Sin nuevas dependencias. Bases temporales para HTTP/PostGIS y navegador.
Unidades: creación/persistencia, gestión privada, selección/seguimiento y cierre.
