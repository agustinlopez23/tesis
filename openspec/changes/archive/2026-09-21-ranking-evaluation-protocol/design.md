# Diseño documental

- Protocolo: `Doc/evaluation/business-ranking-final/protocol.md` y `plan.json`.
- Rúbrica/plantillas: `human-review.md` y `templates/` en el mismo directorio.
- Integración: `Doc/ranking-public-integration.md`; rutas/tablas aún propuestas.

Dos unidades de revisión: protocolo/plantillas (<400 líneas) e integración/
continuidad (<400 líneas). No cambiar especificaciones productivas para presentar
un diseño futuro como comportamiento implementado. La aceptación académica del
protocolo y la ejecución del experimento son hitos diferentes de redactarlo.

Validar JSON, semillas disjuntas, cabeceras CSV, links locales y consistencia de
límites. Sin cambios de runtime: no repetir suites de aplicación ni crear pruebas
unitarias que solo reproduzcan documentos. Los futuros cambios ejecutables tendrán TDD.
