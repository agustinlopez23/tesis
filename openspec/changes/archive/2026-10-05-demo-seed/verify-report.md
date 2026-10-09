# Verificación del seed — 05/10/2026

TDD: carga y rechazo productivo fallaron con NotImplementedException; se ajustó
la prueba de colisión para exigir InvalidOperationException y también quedó roja.
Después de implementar se corrigió la comparación temporal del test a precisión
persistida de PostgreSQL. Resultado final: **3/3 DemoSeedTests**, cero omitidas.

Comando: `dotnet test App/backend/tests/Marketplace.Api.Tests --configuration
Release --filter FullyQualifiedName~DemoSeedTests`, con conexión local de pruebas.
Ejecutado mediante `.tools/test-publication-history.ps1 -Filter
'FullyQualifiedName~DemoSeedTests'`. Incluye API real para ambos ingresos, permisos,
ocho productos visibles, creación de pedido y venta, repetición concurrente tras
editar precio/saldo/plan/rol y rollback ante correo ocupado. Cada base se eliminó.

Con el wrapper `powershell -NoProfile -ExecutionPolicy Bypass -File
App/tools/seed-demo.ps1` se cargó la demo y se repitió sin duplicados: 6 negocios
totales (3 nuevos), 3 cuentas (2 nuevas), 12 productos, 3 planes, 12 saldos y 12
movimientos. Los hashes de negocios/cuenta/fotos anteriores coinciden con el
respaldo; no hay cambios en esos datos. Dos contraseñas aleatorias solo en archivo
local ignorado. No se reiniciaron web/API ni se hicieron llamadas a IA.

Playwright: ocho productos públicos; ingreso del titular y administrador; plan y
catálogo accesibles; stock esperado, selección de yerba y aceite agotado bloqueado.
Móvil 390 px sin desborde, cero excepciones JavaScript. Captura local
`.tools/seed-demo/products-mobile.png`. El recorrido en demo no confirmó pedidos
ni ventas: ese caso se verificó en la base temporal de integración.
