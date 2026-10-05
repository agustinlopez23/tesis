# Catálogo por negocio y suscripciones manuales

## Permisos y vigencia

- El rol `administrator` se asigna por operación del entorno, nunca por registro
  público. Cada solicitud administrativa revalida sesión, suspensión y rol actual.
- Un administrador registra períodos Impulso con inicio incluido y fin excluido,
  motivo de 3–500 caracteres, actor y fecha. La revocación conserva el registro,
  motivo, actor y fecha. Períodos no revocados de un negocio no pueden superponerse.
- El ID de activación pertenece al intento: repetir el mismo ID y contenido no
  duplica; cambiarlo devuelve 409. Fechas normalizadas a microsegundos UTC.
- El responsable consulta su plan e historial. Un plan futuro, vencido o revocado
  no habilita escrituras. El control efectivo está en API/DAL, no en la interfaz.
- Escenario: dos altas simultáneas superpuestas producen un éxito y un 409.
  Períodos contiguos sí son válidos.

## Gestión privada

- Cada negocio admite hasta 200 productos, incluidos archivados. Datos: nombre
  1–160, descripción hasta 2000, categoría textual 1–80, precio decimal 0 a
  9.999.999.999,99, moneda ARS/USD, disponibilidad declarada, estado y versión.
- Estados: borrador, publicado y archivado. Archivar no elimina el ID ni los datos.
  Cambiar un producto no modifica snapshots de futuros pedidos/ventas.
- PUT con UUID y versión 0 crea; las ediciones requieren la versión actual y la
  incrementan. Dos escrituras simultáneas no se pisan. Un reintento después de una
  respuesta perdida puede dar 409; actualizar la lista permite recuperar lo guardado.
- Negocio ajeno devuelve 404; sesión ausente/inválida 401. Las mutaciones verifican
  origen y cabecera CSRF. Vencimiento devuelve 403, preserva lectura privada y datos.
- Foto opcional: JPEG/PNG/WebP hasta 5 MiB y 20 MP, procesada por el componente
  existente a JPEG sin metadatos; persistencia máxima 1 MiB. Alta/retiro requiere
  versión actual. No se permite que cambiar foto descarte texto aún sin guardar.

## Lectura pública

- Solo negocios publicados, con dueño habilitado y plan vigente. Solo productos
  publicados; los no disponibles llevan indicación explícita. Borradores/archivados
  no aparecen. La foto comprueba la misma elegibilidad en cada lectura.
- Página de 20 productos, orden por nombre e ID, filtro textual por nombre,
  descripción o categoría sin distinguir mayúsculas. No es ranking de productos;
  no modifica el orden ni mérito orgánico de negocios.
- Vencimiento o revocación retira el catálogo de la próxima lectura, incluidas
  fotos, sin quitar el perfil gratuito. Todas las respuestas son `no-store`.
- El panel conserva el borrador frente a errores; permite actualizar, muestra
  errores recuperables y cancela lecturas obsoletas al cambiar de negocio.

## Persistencia y límites

Las escrituras de planes/productos/fotos bloquean primero la fila del negocio en
transacción. La vigencia se comprueba después de obtener el bloqueo. La prohibición
de superposición la implementa este camino de escritura; las operaciones SQL
directas no deben modificar períodos fuera del servicio.

La migración `20261005025720_AddBusinessCatalog` agrega tablas y rol sin asignarlo
a cuentas. Down rechaza un catálogo, historial o asignaciones administrativas no
vacíos. Para revertir aplicación con datos reales, conservar este esquema aditivo.

No incluye stock cuantitativo, pedidos, IA, búsqueda global/ranking de productos,
cobros, campañas ni moderación general. El nombre del plan no fija precio comercial.
