# Evidencia incremental

## Cursores

RED: 3/3 fallan con NotImplementedException. GREEN: 3/3 pasan, sin advertencias,
mediante `.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~RankingCursorTests'`.
Pruebas con reloj controlado: borde exacto de TTL, página posterior sin renovación,
token alterado, otro propósito/clave, entrada excesiva y payload autenticado inválido.
Frontera runtime N/A en esta unidad pura; se probará en HTTP en la siguiente.
Rollback: archivos RankingCursorTokens.cs y RankingCursorTests.cs; sin persistencia.

## Endpoints y validación

Primer intento bloqueado por EventLog de Windows dentro del sandbox; no cuenta
como RED funcional. Reejecución autorizada: RED 5/5 (rutas/registro/no-store
ausentes). GREEN 5/5 HTTP/PostGIS, cero omitidas y sin advertencias, con
`.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~RankingHttpTests'`.
45 negocios: 20/20/5, reintento conserva IDs/TTL/versión, despublicación deja hueco,
datos actuales y token de medición mantienen posición 22. Verifica vínculo único,
45 contribuciones, sin ID del dueño/componentes en respuesta; 409 y 410 reales.
Origen, JSON, campos extra, rubro inexistente, radio/query y cuerpo >4096 rechazados.
GET distingue cursor inválido/mezclado, expirado y snapshot ausente. Creación
cerrada por defecto y en Production incluso con PreviewEnabled=true.
Rollback: RankingHttpEndpoints/RankingContracts/RankingHttp*Tests y dos registros
en Program; sin modificar las tablas ni la guía pública.
