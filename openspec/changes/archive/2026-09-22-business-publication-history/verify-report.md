# Verificación — 22/09/2026

Historial implementado; sin ranking público, cambios de DTO ni frontend.

- RED: cuatro pruebas fallaron con PostgreSQL 42703: columna first_published_at
  inexistente, antes de implementar. Los intentos anteriores con Docker apagado
  y EventLog bloqueado fueron fallos de entorno, no evidencia RED.
- GREEN: 4/4; alta publicada, borrador/primera publicación, invalidación atómica,
  edición/baja/republicación, escritura atrasada desde otro DbContext y migración
  de registros anteriores publicados y borradores. EF recupera la fecha generada.
- Regresión: **50/50 Marketplace.Api.Tests**, PostgreSQL/PostGIS real, bases
  temporales independientes. Incluye guía, ubicación, perfiles y actividad.
- Build Release correcto; EF `migrations has-pending-model-changes`: sin cambios.

Comandos (SDK local 10.0.400; TEST_DATABASE_CONNECTION configurada según App/README):

```powershell
dotnet test App/backend/tests/Marketplace.Api.Tests --no-restore --configuration Release -m:1 --filter FullyQualifiedName~BusinessPublicationHistoryTests
dotnet test App/backend/tests/Marketplace.Api.Tests --no-restore --configuration Release -m:1
dotnet-ef migrations has-pending-model-changes --project App/backend/src/Marketplace.DAL --startup-project App/backend/src/Marketplace.Api --configuration Release --no-build
```

Se usó `.tools/test-publication-history.ps1`, con credencial local sin imprimir.
Reintento fuera del sandbox autorizado por bloqueo del EventLog. Docker Desktop
iniciado en segundo plano, servicio local database saludable. No se migró la base
de la demo. El rollback de migración se probó únicamente en base temporal.

Revisión: campos privados generados, trigger BEFORE INSERT/UPDATE y backfill
conservador. La fecha refleja el instante de escritura, no CreatedAt. La prueba
de escritura atrasada fuerza lectura previa a otra publicación, sin depender de
timing aleatorio. Sin cambios en el borrador de evaluación ni anotaciones humanas.
Dos unidades manuales de revisión <400 líneas según design.md; metadatos EF aparte.
Siguiente: candidatos completos; luego adaptador/snapshots según contrato.
