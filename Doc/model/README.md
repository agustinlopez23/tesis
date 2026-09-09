# Modelo de entidades y base de datos

Versión 1.0 · 2026-09-07 · Propuesta de diseño para documentar y revisar antes de implementar.

El modelo deriva del [PRD](../PRD.md) y de la primera entrega del seminario. La entrada del consumidor muestra **negocios ordenados por el algoritmo**, incluso sin búsqueda textual. El catálogo y los pedidos complementan ese recorrido. La adaptación de la fórmula de productos a negocios sigue pendiente de calibración.

## Archivos

| Archivo | Uso |
| --- | --- |
| [entities.puml](entities.puml) | Código PlantUML del diagrama de clases de entidades, con atributos y multiplicidades. |
| [ER.md](ER.md) | Diagramas ER en bloques Mermaid, divididos en tres módulos para su lectura. |
| [database.mmd](database.mmd) | Código Mermaid del ER completo. |
| [01-negocios.mmd](01-negocios.mmd) | Identidad, negocios, planes y publicaciones. |
| [02-comercio.mmd](02-comercio.mmd) | Catálogo, pedidos, ventas e inventario. |
| [03-descubrimiento.mmd](03-descubrimiento.mmd) | Ranking, campañas y métricas. |
| [data-dictionary.md](data-dictionary.md) | Significado, nulabilidad, restricciones y decisiones de persistencia. |
| [field-dictionary.md](field-dictionary.md) | Diccionario de los 203 campos, con descripción, tipo y longitud. |
| [classes.md](classes.md) | Figuras de clases por módulo, con operaciones propuestas. |
| [model.json](model.json) | Definición común para regenerar clases, DER y diccionario por campo. |

El UML es un diagrama de entidades del dominio, sin controllers, DTOs ni repositorios. El ER es un modelo lógico orientado a PostgreSQL/PostGIS: no incluye todavía SQL, migraciones ni tablas de autenticación de un proveedor. Los diagramas contienen el núcleo de atributos; las restricciones del diccionario también son parte del modelo.

Los archivos `.puml` pueden abrirse con un visor PlantUML y los `.mmd` con un visor Mermaid. Se incluyen SVG, PNG y fuentes DOT en `figures/`, generados desde la misma definición de entidades con Graphviz local. Estas figuras no se presentan como exportaciones realizadas por PlantUML o Mermaid; se conservan también esas fuentes para edición con sus herramientas.

## Decisiones de modelado

1. **Identidad y negocio separados.** Un usuario puede ser consumidor y responsable de negocio. Los roles se asignan, no son subclases de persona. Cada negocio tiene un responsable y una ubicación; se permite que una cuenta gestione varios negocios como propuesta pendiente de validación.
2. **Presencia e Impulso sin precios.** Los planes almacenan capacidades, no importes. Presencia es el acceso por defecto sin suscripción vigente; Impulso exige un período habilitado. Contratar publicidad no cambia el ranking orgánico.
3. **Publicaciones unificadas.** Una publicación puede ser novedad o promoción. Una promoción tiene condiciones y vigencia y puede existir sin catálogo. Una campaña promociona exactamente un producto o una publicación promocional.
4. **Solicitud y venta independientes.** El mensaje puede no enviarse y el negocio puede vender cantidades distintas. Se conserva una copia de productos y precios en ambas etapas. Solo una venta reportada descuenta stock; una solicitud no reserva unidades.
5. **Saldo e historial de inventario.** Inventory mantiene el saldo de lectura y control concurrente; StockMovement explica cada cambio. Ambos se actualizan junto con la venta en una transacción.
6. **Equidad medible.** RankingCandidate conserva todos los candidatos elegibles, incluidos los que no se muestran. OrganicImpression conserva exposición realmente observada. Registrar solo ganadores impediría reconstruir las oportunidades del resto.
7. **Puntajes por contexto.** El ranking tiene versión y parámetros; no hay un puntaje global permanente en Business. Distancia, consulta y exposición hacen que cambie en cada solicitud.
8. **Publicidad separada.** AdImpression no alimenta la exposición orgánica. Interaction permite vincular una visita o clic con una impresión y luego con una solicitud; la venta hereda esa atribución explícita.
9. **Exploración anónima.** ConsumerId es opcional; una referencia de pedido no autoriza a leer datos privados. Se propone un token de acceso cuyo hash se guarda, sujeto a definir identidad y retención.

## Cómo documentar los recorridos

**Entrada a la revista:** DiscoveryRequest con contexto `business_feed` y consulta opcional → candidatos Business → cálculo con RankingConfiguration → posición → OrganicImpression cuando la tarjeta cumple el contrato de visibilidad.

**Pedido y venta:** Interaction opcional → OrderRequest → OrderItem → Sale → SaleItem → StockMovement e Inventory. Una venta presencial tiene Sale sin OrderRequest.

**Publicidad:** Business → AdCampaign dirigida a Product o Publication → AdImpression → Interaction → OrderRequest, si existe catálogo y solicitud. Un clic por sí solo nunca se considera venta.

## Alcance y pendientes

- No se modelan pagos del comprador, logística, sucursales, variantes, cantidades fraccionarias, reseñas ni IA de catálogo. Esta última sigue como compromiso académico por revisar en el PRD.
- Parámetros de equidad, actividad, desempate y relevancia sin consulta no se inventan en el esquema. Se versionan una vez especificados.
- Deben validarse capacidades de planes, consumidor anónimo, varios negocios por propietario y reglas de cancelación/devolución.
- Se incorporan UserSession y AuditEvent para sesiones revocables y trazabilidad. Antes de migraciones se requiere revisión física: autenticación, retención, índices medidos y pruebas transaccionales sobre PostgreSQL/PostGIS real.
- Para reproducir experimentos se conservarán datos sintéticos, semilla, secuencia de consultas, juicios independientes de relevancia y configuración como artefactos de evaluación. Las tablas de producción por sí solas no reconstruyen productos o historiales que hayan cambiado.

## Verificación de esta entrega

Las fuentes UML y ER se elaboran desde la misma definición de 26 entidades. Se revisan nombres, extremos de relaciones y correspondencia de atributos. Las figuras se exportan localmente con Graphviz y SVG, y se inspeccionan muestras gráficas. No se ejecutan migraciones ni se afirma que el modelo esté implementado. La validación con los renderizadores específicos PlantUML/Mermaid y las pruebas de restricciones SQL no se han ejecutado.
