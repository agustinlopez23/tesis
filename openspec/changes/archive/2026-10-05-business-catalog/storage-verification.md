# Almacenamiento del catálogo

Unidad: modelos, configuración EF, migración y prueba de persistencia.
Escenario rojo: un Down con producto archivado no arrojaba excepción y borraba
datos. Después de la protección, rechaza con SQLSTATE 23514, conserva precio y
permite ida/vuelta de esquema vacío. `HasPendingModelChanges` es falso.

Verificación: `CatalogSchemaTests` mediante `dotnet test` sobre la base temporal
PostgreSQL/PostGIS de `OnboardingFixture`; no se modifica la demo.
Evidencia definitiva en `verify-report.md` al cerrar el incremento.

Rollback: retirar este comportamiento exige conservar las tablas cuando hay datos.
La migración Down solo se usa en esquema vacío, sin roles administrativos asignados.
No reducir la migración ni el snapshot generado para ajustar presupuesto de diff.
