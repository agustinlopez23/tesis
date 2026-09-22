# Verificación · 21/09/2026

Implementado comparador de una candidata contra Proximity en calibración, con
replay completo y relevancia de hojas verificadas. Sin anotaciones humanas reales
todavía; la reserva no se generó y no hay decisión de aceptación.

- TDD núcleo: cuatro fallos por NotImplementedException, luego cuatro aprobadas.
- TDD adaptador: corregidos dos errores de compilación en asserts nullable;
  después cuatro fallos por stubs de exportación/comparación y cuatro del núcleo
  aprobadas. Implementación mínima: ocho aprobadas.
- Regresión final: **63/63 BLL/evaluación**, build Release sin advertencias.
  El fixture completo recorre 30.000 rankings (tres semillas × dos políticas ×
  5.000 consultas); 30 consultas humanas simuladas solo para probar software.
  Comprueba nueve ámbitos, seis corridas y nDCG uniforme 1 con todos los grados 2.
  Sus archivos se eliminan al terminar; no son evidencia humana ni científica.
- Historia: los 120 órdenes y objetivos de exposición coinciden con el piloto,
  incluyendo consultas no seleccionadas. Falta/parcial/ideal cero/vacía conservan
  estados distintos. Gini de superficie coincide con exposiciones unidas y difiere
  de la media de zonas. Se rechazan consultas y candidatos ajenos y grados inválidos.
- Comparación: semillas de tamaños distintos pesan igual; una métrica ausente
  deja delta null y declara pares disponibles. Desviación muestral de deltas.
- CLI real: dos salidas de calibración vacía tienen hashes idénticos; código 2,
  1.143 pares y 2.286 grados pendientes. Sin summary, trazas ni qrels finales.
  Las 1.143 filas de cada hoja original siguen con grado/razón vacíos.
- Rechazos CLI: entrenamiento, pin incorrecto, opciones mezcladas, variante
  aislada y sobrescritura. Código 1, sin salidas de comparación inválidas.
- Paquete de calibración conserva digest fijado `2414208ce4557d9626fa68d6c5c9d108bcc16a9dafe7175c8bcc6db6beea466c`.
  Recolección y sus pruebas previas siguen pasando; Build no cambió.
- JSON, semillas disjuntas, límites, cuatro CSV vacíos, enlaces y diff --check correctos.

Comando de suite: `dotnet test App/backend/tests/Marketplace.BLL.Tests
--configuration Release --no-restore -m:1`. Helper local del recorrido CLI:
`.tools/verify-human-ranking-replay.py` (requiere salidas nuevas para repetir).
Evidencia local: `.tools/human-ranking-replay-2026-09-21/{pending,pending-repeat}/`.

Unidades encadenables para revisión: núcleo y pruebas; exportador/comparación/
CLI/refactor de lectura; pruebas del exportador y documentación. Cada unidad
queda debajo de 400 líneas; no se crearon commits o PRs.
Sin cambios API/DAL/frontend ni dependencias nuevas: no se repitieron HTTP,
PostGIS, Playwright o ablaciones completas. Sin despliegue.
