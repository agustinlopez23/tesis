# Comprobación y selección provisional de calibración

El laboratorio recorre las **18 configuraciones** del protocolo sobre las semillas
101/103/107. Usa las hojas verificadas y una sola referencia Proximity por semilla;
cada política conserva su propia historia de las 5.000 consultas. Actividad excluida.
Los criterios principales siguen siendo los acordados el 21/09; no se ajustaron
según resultados. El comprobador fijo se identifica como `business-calibration-criteria-v1`.

## Ejecutar

Primero completar entrenamiento, anotación y adjudicación según la [rúbrica](human-review.md).
Conservar el paquete completo y sus respuestas originales. Desde la raíz:

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --calibrate-review .tools/review-packages-2026-09-21/calibration --package-sha256 2414208ce4557d9626fa68d6c5c9d108bcc16a9dafe7175c8bcc6db6beea466c --output .tools/calibration-grid
```

Salida nueva o vacía. La grilla es fija: Eq={0,025;0,05;0,10}, N={0;0,025;0,05},
rotación={no;sí}. No acepta `--config`, semillas alternativas, entrenamiento,
reserva ni opciones de piloto. La [comparación individual](human-ranking.md)
sigue disponible para inspeccionar una candidata sin seleccionar ni decidir.

| Código | Significado |
| --- | --- |
| 2 | Hojas incompletas: revisión de pendientes y grilla preparada; sin rankings ni selección |
| 0 | Barrido terminado; leer estados, no significa que alguna política cumpla |
| 1 | Entrada inválida/error; no sobrescribir evidencia ni interpretar una salida parcial como terminada |

## Criterios y suficiencia

`mainStatus` aplica los límites **acordados** de nDCG, mejora relativa global/por
superficie y cobertura. `guardrailStatus` aplica las salvaguardas **propuestas**
por semilla y de empeoramiento de distribución local. `status` combina ambos.
Cada comprobación exporta regla, procedencia, ámbito, semilla, valores, límite
efectivo y resultado. Igualdad permitida, sin redondear ni agregar tolerancias.
Referencia cero en Gini/desviación exige candidata cero, sin dividir por cero.

Datos incompletos producen `inconclusive`, incluso si otra métrica incumple un
límite. Se exigen tres semillas y nueve ámbitos, muestras/universos iguales,
historial completo, juicios sin faltantes y denominadores coherentes y finitos.
Cada estrato debe conservar sus cinco consultas seleccionadas: al menos cuatro
con ideal positivo en total y una por semilla. Ausencia de exposición o negocios
nuevos elegibles impide concluir. Consultas sin ideal positivo no se inventan ni
se reemplazan. Las medias dan igual peso a las semillas.

La selección requiere las 18 celdas evaluables. Entre las que cumplen el diseño
combinado elige menor desviación global por mérito; luego mayor nDCG, menor Eq,
menor N y rotación desactivada. Posibles estados: `provisional-candidate`,
`no-eligible-policy` e `inconclusive`. Ninguno congela automáticamente la política.
Las salvaguardas siguen pendientes de congelamiento metodológico; el reporte
no las presenta como criterios ya acordados por el usuario.

## Artefactos y estado real

`grid.json` conserva pesos; `summary.json`, todas las corridas, comparaciones,
comprobaciones y selección; `report.md`, estados y fallos; tres archivos
`traces-*.jsonl.gz`, las 285.000 consultas con `policyId`. La referencia aporta
15.000 y cada candidata otras 15.000. Revisión y hashes vinculan paquete,
respuestas y resoluciones; se registra también SHA-256 del ensamblado evaluador.
Conservar fuentes y entorno para el futuro manifiesto de congelamiento.

**Todavía no hay calibración humana ejecutada.** Las hojas reales siguen con
2.286 calificaciones pendientes: el comando devuelve 2 sin métricas. La prueba
del barrido completo usa grados uniformes en archivos temporales de software,
eliminados después; no son juicios recogidos ni evidencia experimental.

Quedan entrenamiento/anotación humana, cierre metodológico, manifiesto de política
congelada y material/evaluación reservada. `finalAcceptanceEvaluated` permanece
`false`; cumplir durante calibración no valida la hipótesis en la reserva.
El [borrador verificable](freeze.md) prepara esas referencias sin declararlas congeladas.
