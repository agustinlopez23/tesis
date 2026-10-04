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
