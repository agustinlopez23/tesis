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
