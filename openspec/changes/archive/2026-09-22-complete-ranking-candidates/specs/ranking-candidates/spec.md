## ADDED Requirements

### Requirement: Universo elegible completo
El lector MUST obtener todos los publicados de dueño habilitado, rubro y radio
válidos en una vista consistente, sin paginación previa ni truncamiento silencioso.

#### Scenario: Más de una página
- GIVEN 45 elegibles y negocios borradores, suspendidos, fuera del radio u otro rubro
- WHEN se recuperan candidatos
- THEN devuelve exactamente los 45 IDs únicos con distancia e historia conservadas.

#### Scenario: Cambio posterior
- WHEN cambian publicación, suspensión, rubro o ubicación
- THEN una nueva lectura refleja elegibilidad vigente sin modificar la lista anterior.

#### Scenario: Historia desconocida
- GIVEN un candidato histórico migrado
- THEN devuelve fecha null y marca desconocida sin sustituir CreatedAt.

#### Scenario: Entrada inválida, corrupción o cancelación
- WHEN faltan rubro/área válidos, un resultado viola integridad o se cancela
- THEN rechaza sin devolver una lista parcial; ausencia de coincidencias sí es vacía.

#### Scenario: Compatibilidad
- WHEN se usa la guía pública actual
- THEN mantiene sus contratos, orden y paginación; no se activa el ranking.
