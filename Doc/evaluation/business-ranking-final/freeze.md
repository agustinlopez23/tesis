# Borrador verificable para congelamiento

`--prepare-freeze` prepara una copia de evidencia con fuentes, protocolo, hojas
originales, revisión/calibración y binarios identificados por SHA-256. El manifiesto
siempre declara **draft**, `policyFrozen: false` y `evaluationAuthorized: false`.
No congela la política ni abre la reserva. La fecha registrada es la de preparación;
las fechas humanas de entrenamiento, emisión de juicios y cierre quedan vacías.

## Preparar y verificar

Desde la raíz, con una carpeta de salida nueva:

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --prepare-freeze .tools/review-packages-2026-09-21/calibration --package-sha256 2414208ce4557d9626fa68d6c5c9d108bcc16a9dafe7175c8bcc6db6beea466c --source-root . --output .tools/freeze-draft
```

Guardar el SHA-256 impreso **fuera** del borrador. Para comprobarlo después, usar
ese valor previamente guardado, no uno calculado del manifiesto que se desea revisar:

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --verify-freeze .tools/freeze-draft --manifest-sha256 HASH_IMPRESO --output .tools/freeze-check
```

Ambos comandos devuelven 0 si la tarea técnica termina, incluso cuando faltan
juicios. Código 1 significa entrada inválida/error. `verification.json` declara
`integrity-only`: verifica conservación de bytes, no autoría humana ni aprobación.
La salida de verificación debe estar fuera del borrador. No sobrescribir ni editar
las copias; preparar otra carpeta cuando cambien fuentes, documentos o respuestas.

## Qué conserva

- `sources/`: .cs/.csproj de BLL, Models, DTOs, laboratorio y pruebas BLL;
  global.json, metadatos Directory.Build/Packages presentes dentro del workspace,
  protocolo, plan, rúbrica y cuatro plantillas. Excluye bin/obj, .git, .env y frontend/API.
- `review-package/`: inputs, fichas, configuración, manifiesto y hojas E1/E2/resoluciones
  con sus bytes originales verificados. judgments.csv conserva su encabezado original;
  los juicios consolidados se guardan como evidencia de revisión separada.
- `runtime/`: DLLs del laboratorio/BLL/Models/DTOs y archivos deps/runtimeconfig.
  Identifican el programa ejecutado; aún hay que demostrar que procede de las fuentes
  capturadas. No se incluyen el SDK ni paquetes de pruebas o feeds externos.
- `calibration/`: la herramienta vuelve a ejecutar el [barrido](calibration.md)
  sobre el paquete copiado. Con hojas incompletas solo guarda pendientes. Con hojas
  completas conserva todas las corridas, trazas y selección provisional, si existe.
- `manifest.json`: inventario exacto de archivos/hashes, huella del mapa ordenado
  de fuentes, política provisional o null, registros humanos vacíos y pendientes.

Se rechazan rutas que salgan del borrador, enlaces/junctions, solapamientos entre
entrada y salida y cambios de archivos inventariados. La verificación también
rechaza archivos extras o faltantes. El digest externo debe conservarse confiablemente:
estos hashes no prueban por sí mismos quién generó los archivos ni cuándo los juzgó.

## Estado de esta entrega

El [registro del borrador preparado](freeze-draft-reference.json) conserva su digest
externo y ubicación local. Las 2.286 calificaciones siguen pendientes: política null,
sin métricas de calibración humana, sin congelamiento y sin abrir reserva.

Faltan entrenamiento y registro humano de autores/fechas, cierre de salvaguardas
metodológicas, correspondencia fuentes/build, política elegible, materiales/custodia
reservados y evaluador final. El borrador hace esos pendientes revisables; no los
resuelve mediante casillas marcadas automáticamente. Las pruebas con grados
simulados se eliminan y nunca se agregan al paquete entregado.

La [reconstrucción posterior](rebuild.md) comprobó correspondencia exacta de los
binarios y del replay de pendientes bajo el entorno registrado. Esa evidencia
se adjunta por separado; no se modificó el manifiesto original ni sus hashes.
