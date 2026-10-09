# Adaptador interno de revista

## Requirement: alcance canónico
Con zona centro/nueva-cordoba/alberdi y rubro positivo, consultar el punto público
a 4 km. Zona inválida o secuencia negativa falla antes de consultar. Igual zona y
rubro conserva clave entre instantes/semillas/secuencias; otro rubro la cambia.

## Requirement: conjunto completo e historia
Con 45 elegibles, devolver 45 candidatos con R=1, distancia original y dueños
internos. Historia desconocida conserva PublishedAt=null, sigue elegible y recibe
N=0. Fecha conocida conserva decaimiento de novedad y antigüedad al republicar.
Lote vacío produce entrada vacía válida. Ningún ID de dueño llega a un DTO público.

## Requirement: fallo explícito
Duplicados, IDs vacíos, historia contradictoria, fecha futura y distancias no
finitas/negativas/fuera de radio abortan, sin resultado parcial. Cancelación y error
del repositorio se propagan. No se modifica el orden de la guía ni sus endpoints.
