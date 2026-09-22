# Verificación — 22/09/2026

Recuperación interna completa implementada; sin cambios de UI, DTO público,
migraciones, dependencias ni reglas del motor offline.

- RED: 13 fallos por NotImplementedException del lector registrado (12 casos nuevos
  y extensión de la prueba de migración). El fallo previo del EventLog por sandbox
  no se contó como RED. Reintento fuera del sandbox autorizado.
- GREEN: **13/13**. Recuperación exacta de 45 IDs, distancia en km y fecha conocida;
  borradores, suspendidos, otro rubro y fuera de radio excluidos. Nueva lectura
  refleja cambio de estado/rubro/ubicación/suspensión, sin mutar la lista previa.
- Migración anterior: negocios históricos publicados/borradores conservan null/true
  al recuperarlos tras republicación; nunca se usa CreatedAt como reemplazo.
- Validación: categoría/área inválidas, NaN/infinito, área nula y cancelación previa;
  rubro inexistente devuelve vacío. ID vacío persistido artificialmente en base
  temporal aborta con InvalidOperationException, sin ocultar el candidato.
- Regresión: **62/62 Marketplace.Api.Tests** sobre PostgreSQL/PostGIS temporal.
  Build Release sin advertencias; `git diff --check` sin errores.

Comandos con SDK local y TEST_DATABASE_CONNECTION según App/README:

```powershell
dotnet test App/backend/tests/Marketplace.Api.Tests --no-restore --configuration Release -m:1 --filter 'FullyQualifiedName~RankingCandidateTests|FullyQualifiedName~Migration_preserves_unknown_history'
dotnet test App/backend/tests/Marketplace.Api.Tests --no-restore --configuration Release -m:1
```

Helper usado: `.tools/test-publication-history.ps1 -Filter ...`; credencial no
impresa. Se revisó que la lectura usa un SELECT parametrizado con ST_DWithin,
sin Skip/Take ni count separado. La consistencia dentro de esa sentencia depende
de PostgreSQL; no implica snapshot persistente ni atomicidad con un ledger futuro.
El lector aún no tiene límite operativo: resolverlo con fallo explícito y pruebas
de carga antes de exponer la función. No hay ranking público ni evaluación final.
