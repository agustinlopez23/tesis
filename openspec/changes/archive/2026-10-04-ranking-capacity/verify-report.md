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

Unidad de concurrencia: commit `072164a`.

## Carga integrada

Prueba de verificación posterior a la implementación, sin RED atribuido:
`RankingLoadTests.Thousand_candidates_with_concurrent_creates_and_pages_preserve_complete_snapshots`.
Ejecutar `.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~RankingLoadTests'`
con PostgreSQL/PostGIS local. Fixture crea/migra/elimina su base temporal;
WebApplicationFactory ejecuta HTTP en proceso con DAL real. No usa la demo.
Para conservar métricas, ejecutar dotnet test con el mismo entorno y
`--logger trx --results-directory .tools/ranking-capacity-test-results`;
el JSON queda en Output/StdOut de la prueba dentro del TRX.

Primera corrida: **1/1**, sin omitidas ni advertencias. 1.000 candidatos, 17
listas, 16 abandonadas tras primera página, cuatro oleadas de 4 POST y 16 GET
simultáneos. Todas completan sin 429/503/500. Recorrido completo: 50 páginas,
1.000 IDs exactos sin repetidos. Después de releer/reintentar: secuencia 17,
17 snapshots, 17 oportunidades y 17.000 contribuciones.

| Medición local | POST (16) | GET (64) |
| --- | ---: | ---: |
| p50, ms | 201,63 | 16,11 |
| p95, ms | 394,17 | 129,95 |

Escenario completo sin siembra: 2.127,30 ms. Asignaciones acumuladas del proceso:
139.179.920 bytes. No es memoria pico ni exclusiva del endpoint: incluye cliente,
servidor, EF y harness. La regresión paralela confirmó la integridad y obtuvo
p95 POST 773,17 ms / GET 154,82 ms, ilustrando dependencia de la carga del equipo.
No se imponen umbrales temporales frágiles ni se certifica capacidad productiva.
No mide red/Kestrel, varias instancias, memoria pico, trabajo SQL con 30 días
de contribuciones ni frontera de 10.000 saldos. El rechazo y frontera de saldos
se verificaron con límite configurable 2, sin presentar eso como ensayo de escala.

## Regresión y cierre

Comando: `dotnet test App/backend/Marketplace.sln --no-restore --configuration Release -m:1`
con SDK local y TEST_DATABASE_CONNECTION del harness. API/HTTP/PostGIS **100/100**,
cero omitidas, sin advertencias, 9 s. BLL/evaluación **99/99**, cero omitidas,
sin advertencias, 43 s. `git diff --check` correcto.
Frontend/visual N/A: este incremento no cambia UI. Sin migraciones nuevas,
sin activar previsualización ni reiniciar demo, sin cambiar anotaciones/borrador.
Rollback de cierre: prueba de carga y documentación/especificaciones asociadas;
las dos unidades previas se revierten independientemente en orden inverso.
Tres unidades locales sobre `56a238e`, sin push/PR. Próximo incremento: proxy/UI
de previsualización; la activación productiva requiere política evaluada y
dimensionamiento real.
