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

Unidad de lecturas acotadas: commit `ddc4c3a`.

## Concurrencia y configuración

RED: 14/14 fallan antes de implementar: ocho configuraciones inválidas arrancan
y seis escenarios de saturación esperan en SQL en vez de recibir 429.
GREEN: **14/14**, cero omitidas, sin advertencias, con
`.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Concurrency_rejects|FullyQualifiedName~Invalid_capacity_configuration'`.
Harness HTTP/PostGIS con interceptor que retiene dos lecturas reales: una tercera
solicitud desde otro cliente se rechaza sin cola; la otra ruta sigue disponible.
Se comprueba recuperación de POST y GET tras éxito, fallo inyectado y cancelación,
con no-store incluso en 429/500. Límites inválidos de lectura/concurrencia impiden
el arranque. Los permisos cubren la ejecución y serialización de la respuesta.
Rollback: RankingConcurrencyLimits/middleware, metadata de rutas, registro y
validación de arranque, pruebas y párrafos de configuración; sin cambiar DAL.

Rama `feat/ranking-capacity`. Pendientes: carga, regresión y cierre documental.
