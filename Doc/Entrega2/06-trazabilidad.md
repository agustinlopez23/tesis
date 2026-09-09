# Trazabilidad y verificación prevista

Cada RF tiene al menos un caso de uso; cada caso tiene ficha, pantalla y entidades o artefactos relacionados. Las secuencias se seleccionan por complejidad. Las verificaciones son escenarios futuros, no pruebas ejecutadas.

| RF | Caso de uso | Pantalla | Secuencia | Entidades principales / evidencia | Verificación |
| --- | --- | --- | --- | --- | --- |
| RF-01 | CU-01 | P01 | Ficha del caso | UserAccount, UserRole | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-02 | CU-02 | P02 | Ficha del caso | Business, BusinessCategory, BusinessActivity | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-03 | CU-03 | P03 | Ficha del caso | Publication, BusinessActivity, Product | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-04 | CU-04 | P04 | S05 | Product, ProductCategory, Inventory | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-05 | CU-05 | P05 | S05 | Product, ProductCategory | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-06 | CU-06 | P06 | Ficha del caso | Inventory, StockMovement, Product | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-07 | CU-07 | P07 | S01 | DiscoveryRequest, RankingConfiguration, RankingCandidate, OrganicImpression, Business | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-08 | CU-08 | P08 | S02 | Product, Inventory, RankingCandidate, OrganicImpression | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-09 | CU-09 | P09 | S03 | Business, Product, Publication, Interaction | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-10 | CU-10 | P10 | S03 | OrderRequest, OrderItem, Interaction | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-11 | CU-11 | P11 | S03 | OrderRequest, OrderItem, Sale | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-12 | CU-12 | P12 | Ficha del caso | OrderRequest, AuditEvent | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-13 | CU-13 | P13 | S04 | Sale, SaleItem, StockMovement, Inventory, OrderRequest | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-14 | CU-14 | P14 | S08 | Sale, SaleItem, StockMovement, AuditEvent | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-15 | CU-15 | P15 | S06 | Plan, PlanCapability, Subscription, AuditEvent | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-16 | CU-16 | P16 | S07 | AdCampaign, Publication, Product | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-17 | CU-17 | P17 | S07 | AdCampaign, AdImpression, AuditEvent | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-18 | CU-18 | P18 | Ficha del caso | Interaction, OrderRequest, Sale, OrganicImpression, AdImpression | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-19 | CU-19 | P19 | Ficha del caso | UserAccount, Business, Publication, BusinessCategory, ProductCategory, AuditEvent | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-20 | CU-20 | P20 | Ficha del caso | RankingConfiguration, DiscoveryRequest, RankingCandidate, OrganicImpression; archivos de evaluación | Ejecutar flujo normal y alternativas A1/A2 de la ficha. |
| RF-21 | CU-07, CU-08, CU-17, CU-20 | P07, P08, P17, P20 | S01, S02, S07 | RankingCandidate, OrganicImpression, AdImpression | Comprobar deduplicación, separación de exposición o conservación de auditoría según RF. |
| RF-22 | CU-02, CU-03, CU-07 | P02, P03, P07 | S01 | BusinessActivity | Comprobar deduplicación, separación de exposición o conservación de auditoría según RF. |
| RF-23 | CU-12, CU-14, CU-15, CU-17, CU-19 | P12, P14, P15, P17, P19 | S06, S07, S08 | AuditEvent | Comprobar deduplicación, separación de exposición o conservación de auditoría según RF. |

## Relación con el diagnóstico

| Proceso relevado | Funciones que lo atienden |
| --- | --- |
| incorporación y publicación de productos | CU-02, CU-03, CU-04, CU-05 y descubrimiento CU-07/CU-08. |
| actualización de precios y disponibilidad | CU-04, CU-06, CU-13 y CU-14. |
| atención de consultas y venta | CU-09 a CU-14 y métricas CU-18. |

CU-01, CU-15, CU-16, CU-17, CU-19 y CU-20 son funciones de soporte, comercialización y evaluación de la solución, no procesos inventados dentro del relevamiento previo.

## Registro de ejecución futura

Por cada escenario conservar: identificador de RF/RNF/CU, versión de software, fecha, datos de entrada, resultado esperado, resultado obtenido, evidencia y defecto asociado. El estado inicial de todos los escenarios de negocio es **no ejecutado**.

Fuente: elaboración propia.

