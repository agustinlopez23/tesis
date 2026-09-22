# Diseño previo

Verificar SHA-256 externo del manifiesto e inventario antes de reconstruir.
Copiar fuentes a una salida nueva, usar SDK local 10.0.400 con global.json fijado,
registrar restore/build y evitar consultar feeds para los proyectos sin paquetes.
No construir sobre sources/ original ni usar sus DLLs como producto reconstruido.

Comparar seis artefactos runtime por hash; registrar diferencias antes de interpretar
su causa. Ejecutar calibración con el binario reconstruido sobre hojas verificadas y
comparar todo el árbol de salida, conservando diferencias de identidad del evaluador.
Si hay diferencias por metadatos de build, investigar y documentar sin ocultarlas.
Las hojas actuales están vacías: coincidir en pendientes no prueba equivalencia de
rankings ni resuelve anotación humana. No cambiar la raíz de confianza para aprobar.

Automatizar el procedimiento después de comprobar el entorno real. TDD para
validación/comparación del helper y pruebas integrales sobre el borrador existente.
Unidad de tooling y pruebas <400 líneas; documentación/evidencia separadas.

Hallazgo de implementación: las DLL incluyen CodeView/PDB con rutas y fuentes
ignoradas incrustadas por el SDK. PathMap solo no basta en .tools; una copia no
ignorada con el mismo contexto Git y PathMap dio seis hashes idénticos. El helper
preserva estas condiciones y registra diferencias sin normalizar los binarios.
