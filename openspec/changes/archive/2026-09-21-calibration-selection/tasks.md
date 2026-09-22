# Tareas

- [x] Especificar criterios/suficiencia y separar acuerdos de salvaguardas propuestas.
- [x] TDD requisito 14: cinco rojas, comprobador mínimo, corregir igualdad en media.
- [x] TDD requisito 15: tres pruebas de grilla/selección y dos de exportación rojas;
  implementar 18 políticas, comparación compartida y exportación verificable.
- [x] CLI: registrar rechazo inicial de --calibrate-review; implementar recorrido
  real de pendientes y rechazo de entradas incompatibles/sobrescritura.
- [x] Ejecutar suite completa y verificar 285.000 trazas/hashes de fixture temporal.
- [x] Mantener hojas reales vacías, actualizar uso/estado y archivar incremento.

Verificación: `dotnet test App/backend/tests/Marketplace.BLL.Tests --configuration
Release --no-restore -m:1`; CLI documentada en
`Doc/evaluation/business-ranking-final/calibration.md`.

Fuera del incremento: anotación humana, cierre del diseño, manifiesto congelado,
material y evaluación reservada e integración pública.
