# Evaluación humana · rúbrica business-relevance-v1

Material preparado para dos evaluadores: hay [paquetes de entrenamiento y calibración](packages/README.md)
con fichas sintéticas y grados vacíos. Aún no hay respuestas humanas.
Las plantillas CSV contienen solo encabezados y no cuentan como datos recogidos.
No usar IA para asignar grados, adjudicar desacuerdos ni simular evaluadores.

## Qué recibe cada evaluador

Ficha de consulta: necesidad expresada en español, superficie, rubro, zona pública
de referencia, radio y restricciones explícitas. Ficha de cada candidato elegible:
oferta descrita con hechos, distancia y limitaciones relevantes. No mostrar
puntaje, posición, variante, déficit, actividad, plan, grado del oráculo ni juicio
del otro evaluador. Identificador opaco; orden por SHA-256 de
`rubricVersion|assessorId|datasetVersion|seed|queryId|businessId`, sin ordenar por mérito.
El responsable prepara fichas con fuentes trazables; si son sintéticas, se indica.

Cada evaluador juzga **todos** los negocios elegibles de las consultas seleccionadas.
Ver toda la oferta, no solo el top 10 de una variante. Estimar el esfuerzo después
de contar pares: dos personas × número de pares, más resolución de desacuerdos.
Si el esfuerzo no es viable, revisar el tamaño antes de congelar; no recortar
candidatos según posición. Dos juicios sobre un par no son dos consultas independientes.

## Grados

| Grado | Decisión verificable |
| --- | --- |
| 3 | Oferta satisface la necesidad y todas las restricciones explícitas; sería una opción directa |
| 2 | Oferta útil con una limitación no esencial claramente identificada |
| 1 | Oferta relacionada pero marginal; exige adaptar significativamente la necesidad |
| 0 | Oferta no satisface la necesidad o viola una restricción esencial |
| vacío | Falta información para decidir; nunca se transforma automáticamente en 0 |

En revista, la necesidad es descubrir opciones del rubro en la zona/radio
especificados; no inventar intenciones de compra ni preferencias no declaradas.
Distancia importa cuando condiciona esa necesidad; no aplicar automáticamente
las bandas del oráculo sintético. Un empate entre opciones útiles es válido.
Novedad, precio de un plan y exposición anterior no definen utilidad del negocio.

Escribir una razón breve sustentada en la ficha, especialmente para 0/1 o vacío.
Doce consultas de entrenamiento separadas de calibración/evaluación sirven para
discutir ambigüedades y revisar la rúbrica antes de congelarla. No se cuentan en
las métricas. Si las fichas no permiten distinguir 1/2/3, mejorarlas antes de seguir.

## Independencia, desacuerdos e importación

Dos evaluadores humanos distintos trabajan por separado; códigos E1/E2 evitan
publicar nombres. Conservar sus hojas originales. Un tercero resuelve todos los
desacuerdos, o ambos acuerdan un grado dejando registrada esa limitación de
independencia. Toda adjudicación conserva grados originales, grado final y razón;
no promediar 1 y 2 para fabricar un grado 1,5. Acuerdos se copian al juicio final.

Antes de adjudicar, reportar porcentaje de acuerdo exacto y matriz 4×4; puede
añadirse kappa ponderado lineal (desacuerdo |a−b|/3), con fórmula y denominadores.
Sin variación suficiente, kappa puede ser no definido. No hay umbral de acuerdo
aprobado; desacuerdos y limitaciones acompañan los resultados, sin ocultarlos.

La importación implementada rechaza: dataset/rúbrica incorrectos, IDs ajenos,
duplicados por par/evaluador, un mismo evaluador usado dos veces, grados fuera
de 0..3, razones faltantes para grados 0/1 y resoluciones inconsistentes.
Grado vacío con razón explica falta de información; sin razón indica tarea sin
iniciar. Ambos quedan incompletos. Para adjudicar, usar E3 (otro evaluador) o
`E1+E2` si hubo consenso; registrar razón. Las resoluciones usan E1 primero y E2
segundo, con sus grados originales; agregar solo filas ya resueltas.
Validar correspondencia exacta contra el manifiesto de candidatos y consultas.
Un par faltante deja la consulta incompleta; no borrar esa consulta del promedio
para obtener aceptación. Tras completar/adjudicar: guardar SHA-256 de cada hoja.

## Archivos de intercambio

UTF-8, separador coma, comillas para texto que contenga comas o saltos de línea.
`dataset_version` identifica también semilla; query/business IDs son locales a él.
`split` = training/calibration/evaluation; grado vacío se conserva como faltante.

- [Consultas](templates/queries.csv): necesidades y contexto que se muestran.
- [Candidatos](templates/candidates.csv): universo elegible con hechos de oferta.
- [Juicios individuales](templates/judgments.csv): una fila por par y evaluador.
- [Resoluciones](templates/resolutions.csv): una fila por par; status = agreed/adjudicated.

## Importación disponible

En una carpeta reunir los cuatro CSV con sus encabezados exactos. No mezclar
training/calibration/evaluation. Usar E1 y E2 como códigos fijos. La tabla de
candidatos es el manifiesto declarado de pares elegibles; cada candidato debe
pertenecer a una consulta, tener oferta/fuente y distancia dentro de su radio.
La herramienta no comprueba por sí sola que ese manifiesto incluya todos los
elegibles del dataset original. Para paquetes generados, usar `--collect-review`
con digest fijado: esa ruta sí verifica extracción y metadatos originales.

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --validate-review RUTA_CSV --output .tools/human-review-result
```

Salida nueva o vacía. Códigos: 0 = cobertura consistente y completa; 2 = pendiente;
1 = input inválido/error. `review-status.json` contiene hashes de los cuatro
inputs, consultas vacías, faltantes, pares por resolver, matriz 4×4 y acuerdo
previo a adjudicar. Solo al completar todos los pares se escribe
`judgments-final.json`; un grado 0 es válido, una ausencia jamás se convierte en 0.
Una carpeta de plantillas vacías produce estado incompleto. Identidades humanas,
calidad de las fichas y aceptación experimental no se certifican con este comando.
La importación no calcula nDCG ni decide aceptación. La ruta separada
[`--evaluate-review`](human-ranking.md) conecta el paquete verificado al runner
de calibración. Faltan anotaciones humanas y material reservado; los fixtures
de software no son juicios recogidos.
