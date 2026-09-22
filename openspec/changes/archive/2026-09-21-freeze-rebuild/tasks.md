# Tareas

- [x] Verificar pin externo/integridad y reconstruir copia con SDK local, sin feeds.
- [x] Comparar archivos y aislar efectos de rutas, Git y fuentes incrustadas en PDB.
- [x] TDD: siete pruebas rojas de verificación/comparación/origen; implementar y pasar.
- [x] Automatizar restore/build/replay, logs, hashes y archivo de trabajo independiente.
- [x] Ejecución real: seis archivos runtime y cuatro salidas iguales; snapshot intacto.
- [x] Guardar referencia externa, documentar alcance y actualizar continuidad.

Pruebas: `node --test App/tools/verify-ranking-rebuild.test.mjs`.
Integración: comando en `Doc/evaluation/business-ranking-final/rebuild.md`.

No se ejecutó calibración humana completa: las hojas actuales están pendientes.
