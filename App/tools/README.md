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

## Accesibilidad y ancho móvil

Con la misma preparación: `npm --prefix App/tools run test:accessibility`.
Usa Chromium y axe 4.13.0, con todas sus reglas predeterminadas, sin exclusiones.
Recorre nueve estados en 1440/320 px: guía, productos, perfil, solicitud, registro,
ingreso, editor de producto, métricas y administración de plan. Espera datos reales
antes de analizar; también prueba con teclado «Saltar al contenido» y su foco.
Comprueba navegación visible a 320/390/768/1440 px, enlaces de al menos 44 px de
alto en móvil y acceso a Productos con Enter, incluida su sección activa.

Guarda `.tools/campaigns/accessibility-result.json` y capturas
`accessibility-*.png`. El comando falla ante violaciones o desbordamiento y limpia
los servicios/base temporales. El informe conserva los resultados inconclusos de
axe para revisión humana, separados de las violaciones; no oculta esos hallazgos.
Las capturas privadas contienen datos temporales y permanecen fuera de Git.

No acredita conformidad WCAG completa: faltan lectores de pantalla, interacción
completa por teclado, dispositivos físicos y revisión del contraste sobre imágenes.
Referencia: [pruebas de accesibilidad con Playwright](https://playwright.dev/docs/accessibility-testing).

## Excepción temporal de auditoría

`npm --prefix App/frontend run audit:ci` analiza todos los avisos y solo acepta
GHSA-vfj7-8cjw-p6xm en la cadena de desarrollo y versiones revisadas, hasta el
15/10/2026 inclusive, hora argentina. CI también exige auditoría de producción
sin excepciones. Pruebas: `node --test App/tools/audit-policy.test.mjs`.
[Autorización, límites y retiro](../../Doc/integracion-main-2026-10-08.md).
