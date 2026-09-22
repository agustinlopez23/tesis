# Diseño

`IRankingCandidateRepository.GetEligibleAsync(categoryId, area, cancellationToken)`
devuelve IDs de negocio/dueño, distancia y fecha/marca histórica. El dueño se usa
internamente para la futura exclusión de previsualizaciones propias; no es DTO
público ni se incorpora automáticamente a un snapshot o ledger.

DAL obtiene todo en un SELECT parametrizado: publicado, dueño no suspendido,
rubro exacto y ST_DWithin sobre geography. Sin Skip/Take, consultas por página ni
count separado; una sentencia conserva una vista consistente de esos datos.
Orden por ID solo para estabilidad técnica, no como ranking. No calcula mérito.
Valida argumentos, IDs únicos/no vacíos, distancia finita/no negativa e historia
coherente. Fallo de integridad o cancelación aborta sin entregar una parte.

El lector interno admite DiscoveryArea válida; el futuro coordinador de revista
deberá restringir a zonas predefinidas y 4 km. No amplía el alcance público.
No hay límite operativo ni política activada: se deben resolver antes de exponer
la función. Un límite futuro deberá fallar explícitamente, nunca truncar.

Pruebas contra PostgreSQL/PostGIS temporal: 45 candidatos y exclusiones, cambios
de elegibilidad entre lecturas, historia desconocida migrada, entrada inválida,
sin coincidencias, cancelación y corrupción. Regresión HTTP cubre la guía.
Sin migraciones/dependencias nuevas; rollback elimina lector/registro IoC.
Revisión en dos unidades <400 líneas: código/pruebas/spec; documentación OpenSpec
y continuidad. Se preserva el laboratorio y su borrador inmutable.
