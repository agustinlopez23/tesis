## ADDED Requirements

### Requirement: Ablaciones identificables y aisladas
El laboratorio DEBE comparar las cuatro combinaciones de novedad/rotación
manteniendo compensación, más la referencia por proximidad. Quitar novedad
DEBE devolver su peso al mérito. Actividad DEBE estar excluida de esas celdas.

#### Scenario: Desactivar rotación
- WHEN la variante es EquityOnly o EquityNewcomer
- THEN se ordena por puntaje exacto e id, independientemente de la secuencia.

#### Scenario: Evidencia compatible
- WHEN se ejecuta sin --suite o con --suite pilot
- THEN se conservan las cuatro variantes y los hashes del piloto original.

#### Scenario: Exportar ablaciones
- WHEN se ejecuta --suite ablation
- THEN se exportan cinco variantes bajo ranking-ablation-report-v1, con inputs
  iguales al piloto y resultados iguales para las variantes comunes.

#### Scenario: Suite desconocida
- WHEN se proporciona una suite distinta
- THEN se rechaza antes de escribir artefactos.
