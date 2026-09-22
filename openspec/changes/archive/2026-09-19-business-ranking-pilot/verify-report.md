# Verificación · 19/09/2026

Resultado: laboratorio de ranking de negocios completo; sin activación pública.
Evidencia: `Doc/evaluation/business-ranking-pilot/README.md` y reporte asociado.

## TDD y pruebas

- Motor: 6 pruebas rojas con `NotImplementedException`, luego 6 verdes.
- Historial: 3 rojas, luego verdes; las 9 iniciales del conjunto pasaron.
- Métricas: 3 rojas, luego 12 pruebas del conjunto verdes.
- Generador: 2 rojas, luego 14 verdes.
- Runner: 2 rojas, luego 16 verdes.
- Exportación: 1 roja, luego 17 verdes; también se corrigió una ambigüedad de
  compilación de `JsonSerializer.Serialize` antes de completar el ciclo.
- Revisión posterior: 2 pruebas adicionales de presupuesto con 20 candidatos,
  expiración parcial y copia de snapshot; se agregaron después de implementar
  y no se presentan como pruebas rojas previas.
- Configuración: una prueba roja confirmó que un parámetro mal escrito se
  ignoraba; lectura JSON estricta lo rechaza. Total BLL/evaluación: **20/20**.
- Suite HTTP con PostgreSQL/PostGIS temporal: **46/46**. Total backend **66/66**.
- Build Release de solución: **0 errores, 0 advertencias**.
- Restore/auditoría de paquetes: aprobados tras reintento de red forzado;
  se reutilizan versiones existentes, sin nuevos paquetes productivos.

Comando focalizado: `dotnet test App/backend/tests/Marketplace.BLL.Tests --configuration Release`.
Suite completa: instrucciones `App/README.md` con `TEST_DATABASE_CONNECTION`
a `postgres`, nunca a datos reales; cada fixture crea y elimina su propia base.

La primera compilación integral encontró archivos bloqueados por la API local.
Se ejecutó el helper conocido que detiene la demo, se completaron las pruebas
y luego se restableció la API. No se aplicaron migraciones nuevas.

## Reproducción y aplicación existente

- Dos ejecuciones completas de 200 negocios × 5.000 consultas × tres semillas
  × cuatro variantes: todos los SHA-256 de inputs, trazas y reportes coinciden.
- Test automatizado de exportación verifica archivos reales byte por byte,
  descomprime trazas y comprueba rechazo de sobrescritura.
- MCP Playwright: `/explorar` carga, consulta del proxy responde 200 con tres
  negocios, sin desborde horizontal en viewport 1920 × 889. Captura inspeccionada:
  `.playwright-mcp/ranking-pilot-guide-smoke.png`. Consola solo DevTools/HMR.
- No hubo cambios frontend: no se repitieron sus 56 pruebas, lint y build de
  la sesión anterior ni se atribuyen como validaciones nuevas. No hay nueva UI.

## Límites

No se validaron garantías matemáticas de equidad, umbrales del experimento final,
juicios humanos, productos, tráfico real, latencia HTTP ni resultados comerciales.
El piloto arroja una reducción de concentración y una pérdida de nDCG; no se
declara aceptable la pérdida sin un umbral acordado. Actividad es sintética.
No se hizo push ni despliegue. API y contratos de actividad conservan su orden.
