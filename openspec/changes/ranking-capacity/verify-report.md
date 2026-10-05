# Evidencia incremental

## Candidatos

Preparación: sandbox bloqueó EventLog; reintento autorizado. Corregida una categoría
ausente en el fixture antes del RED funcional. RED: esperaba 503 y recibió 201.
Comando: `.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Candidate_capacity'`.
GREEN: 1/1 pasa, cero omitidas, sin advertencias. Prueba HTTP/PostGIS: límite 2,
exceso 3, rollback, reintento con
los dos elegibles completos y lectura idempotente aun si crece el conjunto.
Rollback: RankingReadLimits, límite DAL, registro IoC y traducción HTTP 503.

## Saldos históricos

RED: `Historical_capacity` esperaba 503 y recibió 201. GREEN: 7/7 pasan,
cero omitidas, sin advertencias, con
`.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Historical_capacity|FullyQualifiedName~Candidate_capacity|FullyQualifiedName~RankingSnapshotStoreTests|FullyQualifiedName~RankingRetentionTests'`.
HTTP/PostGIS temporal comprueba exceso de tres saldos con límite dos, incluido
un negocio despublicado; replay existente permitido, rollback de snapshot y
secuencia, una sola oportunidad previa y reintento en frontera exacta.
La consulta materializa como máximo límite+1 saldos agregados, no contribuciones.
La agregación sigue requiriendo trabajo SQL sobre el historial del contexto.
Rollback de esta unidad: RankingReadLimits, límites DAL, registro IoC,
traducción HTTP 503, pruebas asociadas y configuración documentada; sin migración.

Rama `feat/ranking-capacity`. Pendientes: concurrencia, carga, regresión y cierre.
