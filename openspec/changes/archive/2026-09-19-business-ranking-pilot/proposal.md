# Laboratorio reproducible de ranking de negocios

Entregar un motor explicable en BLL y un piloto ejecutable que compare relevancia,
proximidad y compensación de exposición con métricas independientes del puntaje.

Incluye candidatos completos, elegibilidad, ventana y contexto, rotación estable,
bonificación inicial acotada, ablación sintética de actividad, métricas, semillas,
inputs exportables y evidencia. Sigue el PRD §8–9 y ADR-001.

Fuera de alcance: activar el ranking público, productos, planes, publicidad,
instrumentar una señal real de actividad y concluir el experimento académico.
La actividad disponible mide interacción, no mérito ni elegibilidad histórica.

Éxito técnico: tests test-first de reglas y métricas, ejecución determinista y
auditable de 200 negocios/5.000 consultas por semilla, reporte de límites y
resultados sin umbrales de éxito inventados. No se modifica el contrato HTTP.
