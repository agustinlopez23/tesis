# Comparar una candidata con juicios verificados

El laboratorio puede comparar una candidata contra Proximity en **calibración**.
Revalida el paquete y las hojas actuales; reproduce las 5.000 consultas por semilla
con historial independiente por política. Solo mide nDCG en las 30 consultas
seleccionadas. No usa el oráculo para completar ni sustituir juicios humanos.

Todavía no hay respuestas humanas: las 2.286 calificaciones de calibración están
pendientes. La ruta completa se probó con fixtures temporales de software, que
no constituyen evidencia experimental. La reserva permanece sin generar.

## Ejecución

Primero completar entrenamiento y luego las hojas de calibración según la
[rúbrica](human-review.md) y las [instrucciones de recolección](packages/README.md).
Conservar respuestas originales y adjudicar desacuerdos en `resolutions.csv`.
Ejecutar desde la raíz, usando una salida nueva:

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --evaluate-review .tools/review-packages-2026-09-21/calibration --package-sha256 2414208ce4557d9626fa68d6c5c9d108bcc16a9dafe7175c8bcc6db6beea466c --config Doc/evaluation/business-ranking-final/calibration-policy-example.json --variant EquityOnly --output .tools/human-calibration-example
```

El [archivo de pesos](calibration-policy-example.json) es un ejemplo de la grilla,
no una ganadora ni política congelada. `plan.json` no es un archivo de pesos.
No combinar esta ruta con opciones de piloto, selección de semillas o preparación.

| Variante | Novedad | Rotación | Peso N permitido |
| --- | --- | --- | --- |
| EquityOnly | No | No | 0 |
| EquityNewcomer | Sí | No | 0,025 o 0,05 |
| EquityRotation | No | Sí | 0 |
| Equity | Sí | Sí | 0,025 o 0,05 |

Eq permite 0,025 / 0,05 / 0,10. Los demás parámetros permanecen iguales al paquete:
elegibilidad, proximidad, mérito, ventana y discretización. Actividad no participa.
Entrenamiento y evaluación reservada se rechazan en esta ruta.

## Salidas y lectura

- Código 2: `review/review-status.json` enumera faltantes/desacuerdos;
  `status.json` indica `pending-human-review`. No hay trazas ni resumen de métricas.
- Código 0: además de juicios verificados, `summary.json`, `report.md` y tres
  `traces-*.jsonl.gz` contienen comparación y cronología completa de ambas políticas.
- Código 1: entrada inválida o error. Una salida parcial por error debe conservarse
  para diagnóstico; no usarla como una comparación terminada ni sobrescribirla.

Cada corrida contiene global, búsqueda, revista y seis superficies/zonas. Gini
de superficie se calcula después de unir exposiciones de sus zonas. Distribución
usa todas las consultas; nDCG solo las seleccionadas con ideal positivo.
Se cuentan por separado consultas no seleccionadas, seleccionadas vacías, con
juicios faltantes y con ideal cero. Ninguna ausencia equivale a grado cero.

Medias entre semillas con igual peso; diferencias pareadas candidata − Proximity
y desviación estándar muestral de esas diferencias. Si alguna semilla no tiene
una métrica, la media correspondiente y el delta quedan `null`, con cantidad
de pares disponibles. El reporte muestra los denominadores por semilla y ámbito;
la desviación no es un intervalo de confianza.

`sha256.json` verifica los artefactos de salida; el resumen vincula el digest del
paquete y los bytes de las hojas E1/E2. El reporte de revisión conserva también
los hashes de CSV consolidados y resoluciones. Guardar el paquete original para
reproducir: los inputs completos se verifican con su manifiesto fijado previamente.

El comando individual siempre declara `acceptanceEvaluated: false`. El
[barrido de calibración](calibration.md) ya comprueba criterios/denominadores y
sugiere una candidata provisional entre las 18 configuraciones. Faltan
congelamiento de política y evaluación reservada. Tener juicios
completos no garantiza suficiente ganancia ideal ni cumplimiento de tolerancias.
