# Diseño previo

CLI --prepare-freeze PAQUETE --package-sha256 HASH --source-root RAÍZ --output VACÍO.
Verificar fuentes acotadas y paquete de calibración antes de copiar. Guardar
originales E1/E2 y resolutions leídos durante la validación, no reconstruirlos
desde grados ni copiarlos otra vez tras validarlos. judgments.csv del paquete
permanece original; la consolidación vive en el reporte de revisión.

Capturar .cs/.csproj de BLL/Models/DTOs/laboratorio y sus pruebas, global.json y
metadatos Directory.Build/Packages presentes, más plan/protocolo/rúbrica/plantillas.
Excluir bin/obj, .git, .env, frontend/API y dependencias ajenas. Rechazar enlaces
y salidas solapadas con inputs. Capturar DLLs y runtimeconfig/deps del laboratorio
para identificar implementación ejecutada; esto no prueba origen del build.

Ejecutar calibración sobre la copia verificada para evitar vincular un resumen
editable previo. Hojas vacías => pendientes sin rankings. Hojas completas =>
grilla original y política provisional si existe; ninguna ganadora inventada.
El manifiesto siempre dice draft, policyFrozen=false, evaluationAuthorized=false.
Registrar instante de preparación, no fechas/identidades de anotación inventadas.

--verify-freeze CARPETA --manifest-sha256 HASH --output VACÍO verifica digest
externo y lista exacta de archivos/copias, rutas relativas seguras y sus hashes.
No usar índices mutables internos como raíz de confianza. Código 0 significa
borrador preparado/integridad verificada, nunca aprobación metodológica.
Alteración, archivo extra/faltante, escape de ruta, enlace o sobrescritura =>1.

Unidades de revisión <400 líneas: archivo/verificador y pruebas; preparación y
pruebas integrales/CLI; documentación. TDD antes de cada comportamiento nuevo.
