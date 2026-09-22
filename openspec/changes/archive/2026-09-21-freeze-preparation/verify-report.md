# Verificación · 21/09/2026

Preparador/verificador implementados. Borrador real generado con 85 artefactos,
política null, 2.286 juicios pendientes y flags policyFrozen/evaluationAuthorized false.
No hay nuevos juicios humanos ni evaluación reservada.

- TDD: siete casos nuevos y ampliación de la prueba de grilla fallaron por stubs.
  Tras implementación, siete pruebas rápidas aprobadas; CLI inicialmente rechazó
  --prepare-freeze y luego reconoció el comando. Suite intermedia 80/80.
- Revisión detectó omisión de Directory.Build.props en carpetas intermedias.
  Prueba nueva falló por archivo ausente; corregida la enumeración de padres.
  Suite final **81/81**, compilación Release sin advertencias.
- Fixture integral ampliado: grilla completa sobre paquete copiado, verificación
  del manifiesto, recolección de originales con grados preservados, política
  consistente con selección y lectura de 285.000 trazas/hashes. Es prueba de
  software con archivos temporales eliminados, nunca evidencia humana.
- Pruebas rápidas: copia autocontenida, exclusiones, ausencia de política/aprobación,
  cambios/extras/faltantes/pin incorrecto, intento de ../ fuera del directorio,
  entrenamiento, fuentes incorrectas, salida solapada y metadatos de build.
- CLI real: preparación y verificación externas con código 0; sobrescritura de
  borrador/reporte, hash incorrecto y --source-root aislado con código 1.
- Los 85 hashes y la huella del mapa ordenado de fuentes se comprobaron además
  desde Python. E1/E2 copiados byte por byte; grados/razones siguen vacíos.
  Calibración solo contiene pendientes; sin summary, trazas ni candidata.
- Sin fuentes/binarios modificados después de generar el borrador; continuidad
  y evidencia de OpenSpec no forman parte de la selección acotada de fuentes.

Digest externo del manifiesto:
`ba27292a32cf36f44829f3bb577ca3d4fcf76ea89ae135c5951d46a2c0dc794a`.
Ubicación: `.tools/freeze-preparation-2026-09-21/draft/`; reporte: `.../verified/`.
Referencia: `Doc/evaluation/business-ranking-final/freeze-draft-reference.json`.
Helper local: `.tools/prepare-freeze-draft.py` (no repetir sobre las mismas salidas).

Comando de suite: `dotnet test App/backend/tests/Marketplace.BLL.Tests
--configuration Release --no-restore -m:1`. Documentación/JSON/enlaces/diff revisados.
Unidades encadenables como design.md: archivo/verificador/pruebas, preparación/
integración/CLI, documentación; cada unidad debajo de 400 líneas.
No se repitieron frontend/API/DAL/HTTP/PostGIS/Playwright: no cambiaron esas capas.
Sin nuevas dependencias, commit, push, despliegue ni distribución a evaluadores.
