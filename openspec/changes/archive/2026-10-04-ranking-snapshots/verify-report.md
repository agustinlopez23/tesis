# Evidencia incremental

1. Cálculo/traza: RED 4/4 fallos contra implementación vacía; GREEN 16/16 pruebas
   focalizadas (RankingOpportunityTests, BusinessRankerTests, ExposureLedgerTests).
   Se conserva presupuesto top 10 y candidatos no mostrados; previsualización
   propia da ceros; hash cambia con parámetros/variante/semilla. Sin frontera
   runtime en esta unidad pura; se verifica PostGIS en las unidades siguientes.

2. Esquema: RED 2 fallos por tablas ausentes; GREEN 2/2
   RankingStorageSchemaTests. Migrar/revertir/remigrar preserva categorías y no
   deja cambios EF pendientes. Unicidad requestId, rangos y cascada comprobados.
   Migración aplicada solo a bases temporales, sin tocar la demo.

3. Almacenamiento: RED 5 fallos antes de implementar; GREEN 5/5
   RankingSnapshotStoreTests/RankingRetentionTests. Tres solicitudes concurrentes
   con el mismo ID construyen una vez; cuatro IDs distintos ven secuencias 1–4 y
   saldos confirmados 0–3. Fallo de restricción después de insertar snapshot revierte
   todo, incluida secuencia. Cancelación también revierte. TTL exacto, ventana
   abierta de 30 días, aislamiento de contexto y purga con cascada comprobados.

4. Coordinación: RED por servicio ausente; GREEN 1/1 RankingSnapshotServiceTests.
   IoC/PostGIS → 45 candidatos → traza → relectura en otro scope. Reproducción exacta
   de componentes y orden usando la traza JSON. No guarda requestId, dueño ni email
   en oportunidades. Previsualización propia conserva 45 contribuciones en cero y
   no altera saldos. Un reintento conserva la política de la lista original.

5. Paginación: RED por servicio ausente; GREEN 1/1 RankingSnapshotPageTests.
   Recorrido 20/20/5, repetición sin contribuciones nuevas, datos públicos actuales,
   exclusión por despublicación/suspensión/rubro/distancia, posiciones conservadas,
   página vacía intermedia y negocio nuevo diferido. Offsets inválidos y expiración
   rechazan la lectura. El repositorio revalida la página en un único SELECT.

6. Retención automática: RED por ausencia de ejecución; GREEN en regresión.
   RankingRetentionWorkerTests verifica arranque, fallo temporal, siguiente hora
   con reloj controlado y parada limpia. Worker sin payloads/identificadores en logs.

## Cierre

**99/99 BLL/evaluación y 72/72 HTTP/PostGIS**, 0 omitidas; Release sin advertencias.
Duraciones de pruebas: 51 s y 7 s. `git diff --check` sin errores.
Comandos desde raíz (DOTNET_ROOT=.tools/dotnet, DOTNET_CLI_HOME=.tools/cli):

```powershell
& .tools/dotnet/dotnet.exe test App/backend/tests/Marketplace.BLL.Tests --no-restore -c Release -m:1 -p:UseSharedCompilation=false
& .tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Marketplace.Api.Tests'
```

El helper HTTP usa la conexión local sin imprimir secretos; fixtures crean,
migran y eliminan bases temporales. La demo no se migró ni reinició. Frontend y
revisión visual N/A: no cambió UI. No se repitió el experimento completo ni se
tocaron los paquetes/borrador previos. La regresión incluye la guía pública.

Cadena local revisable desde e8442b8 (sin publicar PRs): cálculo 867075b (171 líneas),
esquema 1f918e7 (117), persistencia abdcd5a (276), coordinación ef3ab12 (112),
paginación 3411adb (150); cierre de retención y documentación en el siguiente commit.
Cada unidad incluye su prueba y evidencia. Rollback y fronteras en design.md.
Pendientes: endpoints/cursor protegido, controles de carga, UI y política evaluada.
