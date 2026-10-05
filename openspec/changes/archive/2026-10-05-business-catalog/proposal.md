# Catálogo por negocio y permisos por plan

Completar carga manual, edición, archivo y consulta pública de productos, con
foto procesada, precio, moneda y disponibilidad declarada. La sesión existente
identifica al responsable. El administrador habilita períodos manuales auditados;
el servidor verifica vigencia y propiedad en cada operación.

Se adopta para este incremento la propuesta Presencia/Impulso: el vencimiento
oculta catálogo e impide editar, conservando lectura privada y productos.
No cambia la participación ni el puntaje orgánico de negocios.

Fuera de este incremento: búsqueda global/ranking de productos, asistencia IA,
pedidos, stock, cobros y moderación general. Disponibilidad no equivale a stock.

Verificación: HTTP sobre PostgreSQL/PostGIS aislado, pruebas frontend, build,
lint y recorrido de navegador. Cubrir expiración, concurrencia, aislamiento,
revocación, publicación y archivos. Sin migrar la demo ni publicar cambios.

Unidades: permisos/suscripciones; catálogo API/fotos; interfaz/proxy; integración.
Cada unidad incluye pruebas y evidencia; migración generada conservada completa.
