# Evidencia incremental

## Cursores

RED: 3/3 fallan con NotImplementedException. GREEN: 3/3 pasan, sin advertencias,
mediante `.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~RankingCursorTests'`.
Pruebas con reloj controlado: borde exacto de TTL, página posterior sin renovación,
token alterado, otro propósito/clave, entrada excesiva y payload autenticado inválido.
Frontera runtime N/A en esta unidad pura; se probará en HTTP en la siguiente.
Rollback: archivos RankingCursorTokens.cs y RankingCursorTests.cs; sin persistencia.
