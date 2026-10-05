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

## Controles y cierre

RED: 2 fallos (sin cuota HTTP y retirada que aún permitía crear), 6 casos pasan.
GREEN: 8/8 RankingHttpTests. Se limita por proceso a 60 creaciones/minuto y 600
lecturas/minuto, cuotas separadas/configurables, sin cola. Prueba con cuota 1 y
clientes distintos verifica 429 + no-store. Son límites iniciales para desarrollo,
no dimensionamiento ni protección distribuida comprobada.
Deshabilitar creación permite terminar páginas; retirada devuelve 410 también en
POST. Offset fuera de lista y vencimiento incompatible autenticados dan 400.
Prueba adicional de registro/login/negocio propio comprueba presupuesto cero.

Regresión Release **83/83 API/HTTP/PostGIS**, cero omitidas, sin advertencias; 8 s.
Comando: `.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Marketplace.Api.Tests'`.
Incluye los 3 tests puros del cursor y 8 nuevos HTTP, además de los 72 previos.
`git diff --check` correcto. BLL/evaluación conserva evidencia previa 99/99;
no se repite porque no cambió su implementación. Frontend/visual N/A: sin UI.
La demo no fue migrada/reiniciada y la previsualización no se habilitó allí.
No se modificaron paquetes humanos, reserva ni borrador de evaluación.

Unidades desde `02af9d6`: `3acce3f` cursor (176 líneas), `b0deb07` HTTP (280),
controles/cierre en el commit siguiente. Sin push/PR. Rollback de controles:
dos políticas y middleware no-store en Program, retirada POST y pruebas de controles.
Persistencia y guía siguen funcionando al revertir este incremento completo.
Pendientes: límite de candidatos/memoria, concurrencia/carga/latencia y proxy/UI;
política evaluada antes de diseñar activación productiva.
