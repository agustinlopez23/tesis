## ADDED Requirements

### Requirement: Primera publicación persistente
El sistema MUST distinguir nunca publicado, publicación conocida e historia
desconocida sin permitir que una edición reinicie la antigüedad.

#### Scenario: Alta y primera publicación
- GIVEN un nuevo borrador
- WHEN se publica por primera vez
- THEN registra fecha en la base; un alta publicada también registra fecha.

#### Scenario: Edición, baja y republicación
- GIVEN una fecha registrada
- WHEN se edita, despublica o republica, incluso con una escritura atrasada
- THEN conserva exactamente la fecha original.

#### Scenario: Migración sin evidencia histórica
- GIVEN negocios anteriores publicados o en borrador
- WHEN se migra y luego se publican o republican
- THEN conservan null/unknown, sin inventar fechas ni rejuvenecerlos.

#### Scenario: Rechazo de solicitud
- GIVEN un borrador nuevo
- WHEN se solicita publicar con datos inválidos
- THEN permanece borrador sin fecha.

#### Scenario: Compatibilidad pública
- WHEN se consulta la guía existente
- THEN conserva contratos, elegibilidad, orden y paginación.
