# Verificación · 21/09/2026

Comprobador y grilla listos; ninguna calibración humana ejecutada. Los criterios
principales conservan los límites acordados; salvaguardas adicionales se identifican
como propuestas. Toda selección es provisional, no congelamiento ni evaluación final.

- TDD criterios: cinco rojas por stubs. Primera implementación pasó cuatro;
  frontera inclusiva detectó redondeo al promediar valores idénticos. Se centró
  la media antes de sumar, sin epsilon/relajación de tolerancias. Cinco verdes
  más regresión de diferencias pareadas aprobadas.
- TDD grilla/exportación: cinco rojas por stubs; implementadas. CLI nueva también
  falló inicialmente con opción desconocida y luego reconoció el recorrido.
- Suite final **73/73 BLL/evaluación**, compilación Release sin advertencias.
  Comando: `dotnet test App/backend/tests/Marketplace.BLL.Tests --configuration
  Release --no-restore -m:1`.
- Pruebas: límites inclusivos y exceso pequeño, fallos locales/por semilla,
  referencia cero, ausencia de semilla/ámbito/juicio/nuevos, NaN y muestra insuficiente.
  Los datos ausentes predominan sobre incumplimientos y nunca producen aceptación.
- Grilla exacta de 18 celdas sin actividad, parámetros de mérito fijos; selección
  invariante al orden, desempates, celda ausente/inconclusa y ninguna apta.
- Fixture integral temporal: tres semillas × (referencia + 18 políticas) × 5.000
  consultas = **285.000 trazas**. Se leyó cada línea, se verificaron 19 IDs con
  15.000 registros cada uno, 18 comparaciones de seis corridas/nueve ámbitos y
  todos los hashes de artefactos. Los grados uniformes eran exclusivamente una
  prueba de software; se eliminaron los archivos y no se conservaron como evidencia humana.
- CLI sobre hojas entregadas: dos salidas idénticas por hash, código 2, 2.286
  pendientes sin métricas/trazas/selección. Grilla coincide con plan.json.
  Entrenamiento, pin incorrecto, semillas de reserva mezcladas y sobrescritura:
  código 1; sin directorios inválidos. Hojas originales mantienen grado/razón vacíos.
- JSON, semillas disjuntas, muestras, grilla, límites, CSV y enlaces revisados.
  El helper documental antes contaba acumulativamente varios incrementos contra
  el presupuesto original de 400 líneas; se corrigió a comprobación por documento.
  Las unidades de revisión de este cambio siguen separadas como en design.md.

Evidencia local: `.tools/calibration-selection-2026-09-21/{pending,pending-repeat}/`.
Helper: `.tools/verify-calibration-selection.py` requiere salidas nuevas al repetir.
Estado/uso: `Doc/evaluation/business-ranking-final/calibration.md`.
Sin frontend/API/DAL/dependencias nuevas; no se repitieron HTTP/PostGIS/Playwright.
Sin anotación humana, material reservado, commit, push ni despliegue.
