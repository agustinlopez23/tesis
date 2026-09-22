# Historial de primera publicación

El sistema distingue nunca publicado (fecha null, desconocido false), publicación
conocida (fecha presente, desconocido false) e historia desconocida (null/true).

## Primera publicación

Un negocio nuevo publicado recibe fecha de PostgreSQL. Un nuevo borrador conserva
null/false hasta su primera publicación válida. Una solicitud inválida no cambia
estado ni fecha. EF recupera los valores generados al guardar.

## Conservación

Editar, despublicar y republicar conserva exactamente la primera fecha. Una
escritura atrasada desde otro contexto tampoco la reinicia. El DTO no permite
editar el historial; PostgreSQL lo protege atómicamente en cada escritura.

## Migración

Todos los registros anteriores, publicados o borradores, se consideran de historia
desconocida: el estado actual no demuestra que nunca se hayan publicado. No se usa
CreatedAt para inventar fechas. La marca se conserva incluso al republicar.
El futuro adaptador de ranking deberá asignar N=0 a esos registros.

## Compatibilidad

La guía mantiene elegibilidad, contratos, paginación y orden vigentes. Este cambio
no incorpora candidatos completos ni activa el ranking público.
