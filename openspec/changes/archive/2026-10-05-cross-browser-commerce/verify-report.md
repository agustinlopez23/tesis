# Matriz de navegadores — 05/10/2026

`npm --prefix App/tools run test:browser`: **3/3 motores, 10 comprobaciones por
motor, salida 0**. Chromium 153.0.8010.12, Firefox 155.0, WebKit 26.6, con Playwright
1.63.0 y Node 24.20.0. Resultado a las 2026-10-06 00:44:56 UTC (05/10 en Córdoba).

Ingreso, recuperación después de respuesta perdida sin duplicados, clave fuera de
WhatsApp, atención/contacto, venta, anulación con devolución completa, stock y
métricas sincronizados, seguimiento, permisos y móvil 390/320. Sin pageerror en
los tres motores. El harness también pasó sus aserciones HTTP/PostGIS previas.

Primera corrida: Chromium correcto y Firefox falló porque el test navegaba antes
de esperar logout. La captura mostró el panel todavía autenticado. Se corrigió
la secuencia del test y se repitieron los tres motores; no hubo cambio de producto.
El finally limpió servicios/base también en esa primera salida con error.

Artefactos locales ignorados: `.tools/campaigns/browser-result.json` y capturas
`chromium-order-320.png`, `firefox-order-320.png`, `webkit-order-320.png`.
Lockfile instalado con cero vulnerabilidades reportadas. No se repitieron Vitest
ni las suites backend completas: no se cambió código de aplicación.

No acredita pruebas en hardware móvil, Safari comercial, accesibilidad exhaustiva
ni carga. La demo conserva seis negocios, doce productos y la venta de ejemplo.
