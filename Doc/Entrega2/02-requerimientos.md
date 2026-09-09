# Requerimientos del prototipo

Versión 1.0 · 2026-09-07 · Requisitos propuestos para revisión y posterior implementación. Derivan del alcance de [diagnóstico y prototipo](01-diagnostico-y-alcance.md). No son evidencia de funcionalidad existente.

## Requerimientos funcionales

El sistema deberá:

| Código | Requerimiento | Caso de uso |
| --- | --- | --- |
| RF-01 | Registrar cuentas, iniciar y cerrar sesión y rechazar el acceso a cuentas suspendidas. Los permisos se comprobarán en servidor. | CU-01 |
| RF-02 | Crear, editar y archivar el perfil propio con nombre, descripción, rubro, ubicación y WhatsApp, validando los datos antes de publicarlo. | CU-02 |
| RF-03 | Crear, editar, publicar y archivar novedades o promociones propias con condiciones y vigencia; vincular productos solo cuando el plan habilite catálogo. | CU-03 |
| RF-04 | Crear, editar, publicar y archivar productos con nombre, descripción, categoría, imagen, precio, moneda y disponibilidad, preservando los valores históricos de pedidos y ventas. | CU-04 |
| RF-05 | Solicitar sugerencias de nombre, descripción y categoría desde una imagen y datos mínimos, editarlas o descartarlas, sin publicar automáticamente y permitiendo carga manual ante fallos. | CU-05 |
| RF-06 | Registrar entradas y ajustes de stock por producto con cantidad, motivo y responsable, mantener historial y rechazar saldos negativos y operaciones duplicadas. | CU-06 |
| RF-07 | Mostrar al ingresar una revista de negocios ordenados por el ranking hiperlocal, con ubicación manual o del dispositivo, filtros de rubro y búsqueda textual opcional, incluyendo negocios gratuitos elegibles. | CU-07 |
| RF-08 | Buscar productos de catálogos habilitados por texto, categoría y zona, ordenando con la fórmula de productos y sus límites de exposición por negocio. | CU-08 |
| RF-09 | Consultar perfil, catálogo o promoción vigente y abrir WhatsApp con contexto del negocio, registrando intención de contacto sin considerarla mensaje recibido ni venta. | CU-09 |
| RF-10 | Crear una solicitud de productos de un solo negocio con referencia única, copia de cantidades y precios y resumen para WhatsApp, sin reservar stock y sin duplicar la solicitud al reintentar. | CU-10 |
| RF-11 | Permitir consultar el resumen y estado de una solicitud mediante acceso privado autorizado, sin permitir acceso por la referencia pública aislada. | CU-11 |
| RF-12 | Listar y localizar solicitudes propias por referencia, confirmar recepción del contacto, pasar a atención o cancelar, preservando el contenido original. | CU-12 |
| RF-13 | Registrar una venta vinculada a solicitud o presencial, conservar artículos finales y descontar stock atómicamente, impidiendo conversiones duplicadas y stock negativo. | CU-13 |
| RF-14 | Anular una venta con motivo y auditoría sin reponer stock automáticamente; registrar devoluciones físicas relacionadas con líneas vendidas sin exceder las cantidades originales. | CU-14 |
| RF-15 | Mostrar ventajas de Presencia e Impulso sin precios, permitir al administrador habilitar períodos no superpuestos y aplicar capacidades según vigencia, conservando historial al vencer. | CU-15 |
| RF-16 | Crear y consultar campañas para una promoción o un producto propio elegible con zona y fechas, enviarlas a revisión y pausarlas; la suscripción no compra posición orgánica. | CU-16 |
| RF-17 | Aprobar o rechazar campañas, registrar confirmación manual del pago y habilitar entrega solo con fechas, zona, negocio y destino válidos, identificándola como patrocinada. | CU-17 |
| RF-18 | Mostrar métricas por período y capacidades: visitas y clics para Presencia y además solicitudes, ventas reportadas y atribución por producto o campaña para Impulso, separando exposición orgánica y paga. | CU-18 |
| RF-19 | Gestionar categorías y suspender o rehabilitar cuentas, negocios y publicaciones con motivo y auditoría, preservando historial comercial. | CU-19 |
| RF-20 | Versionar parámetros y ejecutar evaluación reproducible del ranking con datos sintéticos y de la asistencia de catálogo con una muestra documentada, exportando métricas sin alterar evidencia de producción. | CU-20 |
| RF-21 | Registrar candidatos elegibles, versión del ranking e impresiones visibles deduplicadas; conservar exposición orgánica y patrocinada en registros separados. | CU-07, CU-08, CU-17, CU-20 |
| RF-22 | Registrar actividad sustantiva del negocio con fecha y deduplicación y aplicar su aporte acotado según la configuración; una edición repetida no podrá acumular ventaja ilimitada. | CU-02, CU-03, CU-07 |
| RF-23 | Registrar cambios sensibles de estado, plan, moderación y venta con actor, instante y motivo, sin reemplazar el historial comercial ni registrar secretos. | CU-12, CU-14, CU-15, CU-17, CU-19 |

## Requerimientos no funcionales

| Código | Atributo | Requerimiento verificable | Verificación prevista |
| --- | --- | --- | --- |
| RNF-01 | Autorización | Toda operación privada comprueba identidad, rol, propietario y capacidad en servidor. | Pruebas de acceso cruzado con dos negocios; las operaciones ajenas deben rechazarse sin modificar ni revelar sus datos. |
| RNF-02 | Integridad y concurrencia | Venta, líneas, movimientos y saldo se confirman juntos; nunca stock negativo ni doble conversión. | Dos ventas simultáneas sobre la última unidad: solo una descuenta. Un fallo intermedio revierte toda la operación. |
| RNF-03 | Idempotencia | Repetir una misma operación de solicitud, venta o ajuste con la misma clave conserva un único efecto. | Reenviar tres veces la misma petición y comprobar una solicitud, venta o movimiento respectivamente. |
| RNF-04 | Usabilidad y accesibilidad | Recorridos principales utilizables a 360 y 1280 píxeles, con teclado, etiquetas y mensajes de error asociados. | Revisión de todas las maquetas y prueba posterior sin ratón; evitar desplazamiento horizontal de formularios y ofrecer alternativa legible a tablas. |
| RNF-05 | Tiempo de búsqueda | Objetivo propuesto: p95 de la API de descubrimiento inferior a 1 s con 200 negocios y 3000 productos, carga de 10 consultas concurrentes durante 5 min tras calentamiento. | Registrar hardware, versión, errores y latencias; no declarar cumplimiento antes de medir. El objetivo está sujeto a validación de infraestructura. |
| RNF-06 | Fallos externos | La geolocalización denegada admite zona manual; WhatsApp fallido conserva la solicitud; IA fallida admite formulario manual. | Escenarios de permiso denegado, enlace no abierto y proveedor simulado con error o timeout de 15 s propuesto. |
| RNF-07 | Privacidad | La referencia pública no autoriza acceso a solicitudes. Guardar hash del token privado y excluir secretos, mensajes completos y coordenadas de logs generales. | Pruebas de referencia enumerada, token inválido y revisión de logs. Política de retención de datos reales pendiente antes de piloto. |
| RNF-08 | Reproducibilidad | Mismo conjunto, secuencia temporal, versión y semilla producen el mismo experimento de ranking. | Dos corridas aisladas deben coincidir en orden y métricas dentro de tolerancia numérica documentada. |
| RNF-09 | Auditabilidad | Cambios sensibles conservan actor, fecha UTC, transición y motivo; no pueden editarse desde los paneles ordinarios. | Inspeccionar auditoría de suspensión, activación de plan y anulación de venta; verificar rechazo de modificación de eventos. |
| RNF-10 | Compatibilidad | Interfaz web responsiva, sin app obligatoria; validar al menos Chromium y Firefox en las versiones registradas en el acta de pruebas. | Ejecutar recorridos de aceptación en ambos motores y registrar versiones, dispositivo y limitaciones. |
| RNF-11 | Recuperación | Conservar respaldo de datos de demostración y procedimiento de restauración; no prometer disponibilidad comercial 24×7. | Restaurar copia en una instancia aislada y verificar conteos e integridad de una venta; registrar duración. |
| RNF-12 | Mantenibilidad | Mantener separación API/BLL/DAL e inversión de dependencias; reglas de ranking y ventas verificables sin proveedores externos. | Revisar referencias de proyectos y ejecutar pruebas unitarias BLL e integración DAL con PostgreSQL/PostGIS real. |
| RNF-13 | Seguridad de entradas | Validar longitudes, cantidades, imágenes y destinos externos; no aceptar HTML ejecutable ni enlaces arbitrarios como destino de contacto. | Casos de valores fuera de límite, contenido malicioso y archivo no admitido; construir enlace de WhatsApp desde número validado. |

Los valores de rendimiento y timeout son objetivos de diseño propuestos, no resultados observados. Las decisiones pendientes se reúnen en [decisiones y validación](07-decisiones-y-validacion.md).

Fuente: elaboración propia.
