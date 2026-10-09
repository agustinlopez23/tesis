# Verificación — 05/10/2026

Base `23bd605`, rama `feat/commerce-metrics`. Dos pruebas HTTP iniciales fallaron
con 404 y frontend por import inexistente. Implementación posterior verde.

API completa: **129/129**, comando `.tools/test-publication-history.ps1 -Filter
'FullyQualifiedName~Marketplace.Api.Tests'`. Frontend: **107/107**, `npm test`;
lint y build correctos. Casos: solicitudes de distintos períodos, conversión
por cohorte, venta presencial separada, exclusión de anuladas, acceso, período
inválido, cohorte vacía y retirada de cifras ante error.

`node App/tools/verify-catalog.mjs --sales --serve` verificó integración real:
dos solicitudes, una convertida, 50%, ARS 24000 solicitado; después de anular,
cero ventas vigentes, una anulada, conversión cero e importes vacíos. Playwright
abrió panel, cambió a 30 días y verificó 1440/390/320 px sin desborde. Captura
local ignorada `.tools/sales/commerce-mobile.png`. Base y procesos eliminados.

Sin migración nueva, push, PR ni despliegue. Rollback: quitar `Commerce*` en BLL/
DAL/DTOs, registro IoC, ruta `/commerce`, su proxy, componente/montaje/estilos y
pruebas/aserciones del harness. Solicitudes, ventas y datos anteriores permanecen.
Una unidad de revisión local con pruebas, contrato, guía y evidencia.
