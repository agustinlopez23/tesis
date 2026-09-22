# Diseño

Models incorpora `FirstPublishedAt` y `PublicationHistoryUnknown`. La BLL sigue
validando la transición de estado; DAL registra el instante efectivo de escritura
con un trigger PostgreSQL. Esta invariancia de persistencia vive junto a sus
restricciones: evita carreras entre dos transacciones que leyeron un borrador.
EF lee ambos campos generados al insertar/actualizar; no son editables vía DTO.

Un nuevo borrador tiene null/false; su primera publicación recibe hora UTC de la
base. Una alta publicada también. Una vez registrada, la fecha es inmutable.
La migración marca **todos** los registros existentes null/true, sin derivar fecha
de CreatedAt ni asumir que un borrador nunca estuvo publicado. La marca también
es inmutable. El futuro adaptador de ranking deberá asignar N=0 a desconocidos.

Pruebas HTTP/SQL reales en base temporal: transiciones, rechazo atómico, migración
desde esquema previo y escritura atrasada. La suite existente cubre elegibilidad,
paginación y distancia. No se cambian dependencias ni UI. Rollback de migración
elimina trigger/campos y pierde esta historia: solo probarlo en base temporal.

Separación para revisión: A) Models/DAL/migración, pruebas y spec canónica;
B) artefactos OpenSpec y actualización de continuidad/documentación. Cada unidad
manual queda bajo 400 líneas. El diseñador/snapshot generado por EF se compara
separadamente como metadatos repetidos, sin fraccionar artificialmente el archivo.
La recuperación de candidatos será otro incremento.
