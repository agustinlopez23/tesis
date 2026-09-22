# Reconstrucción verificada del borrador

Se reconstruyó el laboratorio desde las fuentes del borrador identificado por
`ba27292a32cf36f44829f3bb577ca3d4fcf76ea89ae135c5951d46a2c0dc794a`.
Las cuatro DLL y los dos archivos deps/runtimeconfig coinciden **byte por byte**
con sus copias originales. También coinciden los cuatro archivos de salida de
calibración. El [registro de evidencia](rebuild-reference.json) vincula los hashes.

El replay devolvió código 2: reproduce las **2.286 calificaciones pendientes**,
sin métricas ni selección. Esta comprobación acredita la correspondencia de
fuentes y binarios en el entorno registrado; no agrega juicios humanos ni valida
el ranking. El manifiesto original se conserva intacto con sus pendientes históricos.

## Repetir la comprobación

Desde la raíz del repositorio, en Windows con Node y el SDK local:

```powershell
node App/tools/verify-ranking-rebuild.mjs --draft .tools/freeze-preparation-2026-09-21/draft --manifest-sha256 ba27292a32cf36f44829f3bb577ca3d4fcf76ea89ae135c5951d46a2c0dc794a --dotnet .tools/dotnet/dotnet.exe --output .tools/freeze-rebuild-new
```

La salida debe ser nueva y estar dentro del repositorio, separada del borrador.
Código 0 significa identidad de runtime y salida; código 1 indica diferencias,
entrada inválida o fallo técnico. Inspeccionar `report.json` y `logs/`.
El helper verifica el digest externo y todos los archivos antes y después; no
compila dentro de la copia protegida. Guarda comandos, SDK, revisión Git,
hash del ejecutable dotnet, comparaciones y hashes de sus artefactos.

## Condiciones de reproducción

SDK fijado: 10.0.400. El restore usa un NuGet.Config con fuentes vacías y desactiva
la consulta de auditoría: estos proyectos runtime no requieren paquetes externos
y usan los packs locales del SDK. No reconstruye la suite de pruebas ni instala
dependencias; conserva sus fuentes como parte del borrador.

Los binarios guardados incorporan información de depuración y revisión Git.
Una compilación ordinaria en `.tools/` no produjo las mismas DLL: cambiaron las
rutas y el SDK incrustó fuentes por tratarse de una carpeta ignorada por Git.
Se verificó ese comportamiento en los targets locales del SDK.

El helper toma la ruta original registrada en CodeView de las cuatro DLL y exige
que coincida entre ellas. Aplica `PathMap` a esa raíz, sin editar las fuentes, y
usa un directorio temporal no ignorado dentro del mismo repositorio. Al terminar
lo archiva en `output/work/`, verificando las rutas de origen/destino. Los logs
conservan su ubicación durante la compilación; para repetir se crea otra copia.
La revisión registrada fue `58ecb08a7bc771a5c94b5b5c076cd07a61e1b565`.

Cambiar SDK, revisión/configuración Git o reglas de exclusión puede producir
diferencias. La herramienta las informa; no omite metadatos del hash ni promete
identidad en cualquier máquina. La evidencia de esta entrega es una reconstrucción
exacta bajo las condiciones registradas, no una firma de autoría o aprobación.

Validación nueva: **7/7 pruebas del helper** y ejecución integrada real.
Las 81 pruebas BLL/evaluación anteriores siguen siendo evidencia previa; no
cambiaron sus fuentes ni binarios originales durante este incremento.
Pendientes: anotación/entrenamiento humanos, cierre metodológico, política elegible,
materiales/custodia reservados y evaluador final. La política sigue sin congelarse.
