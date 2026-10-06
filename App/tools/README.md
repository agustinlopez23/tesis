# Verificar el flujo comercial en navegadores

`npm --prefix App/tools run test:browser` ejecuta el mismo recorrido en Chromium,
Firefox y WebKit contra Next de producción, API y PostGIS temporales. Sale con
código distinto de cero ante un fallo y elimina la base/servicios al terminar.
La demo en 3000/5080 no se usa para las pruebas.

## Preparación en Windows

Requiere Node y SDK local preparados según [App/README](../README.md), Docker con
PostGIS activo, `App/.env.local`, API Release y frontend compilados. Los puertos
3200/5280 deben estar libres. Desde la raíz del repositorio:

```powershell
.tools/dotnet/dotnet.exe build App/backend/Marketplace.sln --configuration Release
npm --prefix App/frontend run build
npm --prefix App/tools ci
npm --prefix App/tools run install:browsers
npm --prefix App/tools run test:browser
```

Compilar antes de iniciar la prueba. La instalación descarga las versiones de
navegadores de Playwright 1.63.0; no reemplaza Chrome/Edge instalados. Para repetir
un solo motor: `npm --prefix App/tools run test:browser -- --browser=firefox`.
No se requieren cuentas reales ni credenciales de la demo.

## Cobertura y resultados

El harness crea datos y cuentas temporales, luego el navegador recorre ingreso
incorrecto, solicitud con respuesta perdida y reintento idéntico, atención y
contacto, venta, anulación con devolución, inventario y métricas sin recargar,
seguimiento de cliente, permisos de titular/administrador y anchos 390/320 px.
Se comprueba que WhatsApp no incluya la clave privada y no haya excepciones JS.

Resumen: `.tools/campaigns/browser-result.json`, con versión de cada motor y
comprobaciones alcanzadas. Capturas de la solicitud: `*-order-320.png`; ante fallo,
`*-failure.png`. Sin trazas de red que exporten cookies o enlaces privados.
`session.json` y los logs del harness son locales y pueden contener datos privados
de las cuentas temporales: no versionarlos. Una corrida reemplaza los resultados
anteriores; la ausencia de `passed: true` no acredita que se completó ese motor.

No es una matriz de dispositivos físicos ni una auditoría completa de accesibilidad,
carga o todas las funciones. WebKit no equivale a ejecutar Safari en un iPhone.
La primera ejecución falló al navegar antes de finalizar logout; ahora espera el
formulario de acceso. No se necesitó modificar el comportamiento del producto.

Referencias: [navegadores de Playwright](https://playwright.dev/docs/browsers) y
[verificación Next/Playwright](https://nextjs.org/docs/app/guides/testing/playwright).
