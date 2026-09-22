# Paquetes de anotación · 21/09/2026

Material sintético preparado, **sin calificaciones humanas**. Dos personas reciben
hojas separadas con la misma cobertura y distinto orden determinista. Solo editar
`grade` y `reason`; no compartir respuestas antes de terminar la fase independiente.

| Etapa | Consultas | Fichas por persona | Total de juicios pendientes | E1 | E2 |
| --- | --- | --- | --- | --- | --- |
| Entrenamiento | 12 | 521 | 1.042 | [Paquete E1](training/E1.zip) | [Paquete E2](training/E2.zip) |
| Calibración | 30 | 1.143 | 2.286 | [Paquete E1](calibration/E1.zip) | [Paquete E2](calibration/E2.zip) |

Cada ZIP contiene solamente el CSV de esa persona y LEEME.txt. Las fichas incluyen
necesidad, rubro, zona, radio, oferta ficticia y distancia; no contienen puntajes,
posición, actividad ni grado del oráculo. No enviar al evaluador el paquete completo
del coordinador, que incluye inputs y ambas hojas. Nadie recibió mensajes/archivos
desde estas herramientas: los ZIP están preparados localmente.

Primero usar entrenamiento para revisar comprensión, ambigüedades y esfuerzo real.
La carga incluye todos los elegibles; no recortar al top 10 ni rellenar con IA.
Si se cambia vocabulario/rúbrica/tamaño, versionar y regenerar antes de anotar
calibración. Las hojas de entrenamiento nunca se suman a las métricas finales.
Las 30 consultas de calibración siguen siendo simuladas; estas fichas no acreditan
datos reales de Córdoba ni independencia semántica de una futura evaluación.

## Reproducción

Desde la raíz, con SDK según `App/README.md`; salidas nuevas o vacías:

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --prepare-review training --output .tools/review-training-new
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --prepare-review calibration --output .tools/review-calibration-new
```

Semillas: entrenamiento 61; calibración 101/103/107. Cada input tiene 200 negocios
y 5.000 consultas, pero las hojas solo contienen las consultas seleccionadas según
el protocolo. `business-review-cases-v1` asigna capacidades a cafeterías, panaderías,
ferreterías y librerías; genera texto a partir de hechos sintéticos, sin invocar
el oráculo. Misma oferta/radio para ambas personas. Evaluación reservada se rechaza.

Guardar el SHA-256 que imprime la preparación fuera del paquete. Los manifiestos
de esta entrega están [aquí para entrenamiento](training/manifest.json) y
[aquí para calibración](calibration/manifest.json); sus hashes y los ZIP están en
[training/sha256.json](training/sha256.json) y [calibration/sha256.json](calibration/sha256.json).

Paquetes completos del coordinador en `.tools/review-packages-2026-09-21/training/`
y `.../calibration/`. Se conservaron repeticiones `.../training-repeat/` y
`.../calibration-repeat/` con todos los archivos idénticos. Se versionan en Doc
solo manifiestos, ZIP en blanco y hashes; los inputs voluminosos se regeneran.

## Recoger respuestas

Reemplazar `reviewers/E1.csv` y `reviewers/E2.csv` del paquete completo con sus
respectivas respuestas. CSV UTF-8, coma, comillas para textos con comas/saltos;
`grade` debe ser vacío o 0/1/2/3, sin decimales. No modificar IDs, hechos o rúbrica.
El archivo maestro `judgments.csv` se mantiene vacío: collect combina las hojas.

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --collect-review .tools/review-packages-2026-09-21/calibration --package-sha256 2414208ce4557d9626fa68d6c5c9d108bcc16a9dafe7175c8bcc6db6beea466c --output .tools/review-calibration-status
```

Código 2: pendientes/desacuerdos; 0: cobertura completa; 1: error/alteración. Cada
lectura usa salida nueva. `review-status.json` identifica qué par/evaluador falta.
Resolver desacuerdos humanos en `resolutions.csv` según la [rúbrica](../human-review.md),
preservando grados originales; volver a recoger. El sistema nunca adjudica por IA.
No borrar candidatos/consultas para eliminar pendientes.

Collect verifica el digest fijado, reextrae selección/elegibilidad y compara los
inputs/fichas/instrucciones con la versión implementada. Acepta cambios de orden
de filas, pero solo grado/razón pueden cambiar en cada ficha. Filas ausentes dejan
pendientes; duplicadas/ajenas/fichas alteradas se rechazan. Los resultados incluyen
`packageSha256` y `reviewerSha256` de los bytes originales de ambas respuestas;
conservar esas hojas para auditarlas. `--validate-review` directo solo valida el universo declarado y
no acredita este vínculo con la extracción. No sustituir collect por ese comando.

SHA-256 asegura correspondencia con un digest confiable, no identidad de personas.
Conservar fuentes/runtime y manifiesto: una versión diferente que no reproduzca
el paquete se rechaza; no se promete identidad binaria universal entre plataformas.
La [comparación de calibración](../human-ranking.md) conecta las hojas verificadas
al runner con historial completo. Sin juicios completos devuelve pendientes;
no decide aceptación ni abre la reserva.
