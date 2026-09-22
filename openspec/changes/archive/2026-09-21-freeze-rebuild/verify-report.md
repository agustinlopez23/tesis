# Verificación · 21/09/2026

Reconstrucción exacta del borrador cuyo pin es
`ba27292a32cf36f44829f3bb577ca3d4fcf76ea89ae135c5951d46a2c0dc794a`.
Fuentes, binarios y manifiesto originales permanecen intactos.

- Restauración sin feeds y build Release con SDK 10.0.400, cero advertencias/errores.
- Primeras compilaciones en .tools difirieron en las cuatro DLL; deps/runtimeconfig
  ya coincidían. PathMap corrigió rutas pero no el cambio de EmbeddedFiles al compilar
  dentro de una carpeta ignorada. Targets locales Roslyn/SourceLink/Git explican
  ambos efectos. Copia no ignorada + PathMap produjo seis hashes idénticos.
- TDD del helper: siete fallos por stubs; después **7/7 aprobadas**, incluyendo
  pin, archivos cambiados/extras/faltantes, ../, comparación exacta y raíz CodeView.
- Integración: `status=matches`, cuatro DLL y dos JSON runtime idénticos, cuatro
  salidas de calibración idénticas y fuentes copiadas sin cambios. Revisión Git
  `58ecb08a7bc771a5c94b5b5c076cd07a61e1b565` registrada en el reporte.
- Replay código 2, `pending-review-only`: 2.286 juicios pendientes, sin rankings
  humanos ni candidata. No se afirma equivalencia empírica de rankings con humanos.
- El sandbox bloqueó procesos Node/Git con EPERM. Se reintentaron pruebas y helper
  con la autorización del entorno. El intento fallido quedó conservado en `verified/`;
  evidencia exitosa en `verified-retry/`, sin sobrescrituras ni relajar verificaciones.
- El trabajo temporal y las pruebas de diagnóstico se archivaron dentro de .tools
  después de verificar rutas absolutas; no quedó una copia de fuentes en App/.
- JSON/enlaces/diff revisados. Helper y pruebas juntos <400 líneas; documentación
  separada. Sin cambios en fuentes congeladas, API/DAL/frontend, ni nuevas dependencias.

Evidencia: `.tools/freeze-rebuild-2026-09-21/verified-retry/` y referencia en
`Doc/evaluation/business-ranking-final/rebuild-reference.json`.
Comando: `node --test App/tools/verify-ranking-rebuild.test.mjs`; ejecución integrada
según `Doc/evaluation/business-ranking-final/rebuild.md`.
Las 81 pruebas BLL/evaluación previas no se repitieron al no cambiar esas fuentes.
Sin anotación humana, congelamiento, apertura de reserva, commit, push o despliegue.
