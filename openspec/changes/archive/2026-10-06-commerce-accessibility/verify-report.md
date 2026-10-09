# Contraste comercial — 06/10/2026

`npm --prefix App/tools run test:accessibility` falló antes del cambio: 12/18
análisis mostraron `color-contrast`, con siete selectores distintos en filtros,
tarjetas, invitación a comercios, perfil sin fotos y encabezado de actividad.
Evidencia local conservada en `.tools/campaigns/accessibility-red.json`.

Después de ajustar tres reglas CSS, el mismo comando terminó con código 0:
**18/18 análisis sin violaciones**, sin desbordamiento a 1440/320 y foco correcto
tras activar «Saltar al contenido» con Tab/Enter. Chromium 153.0.8010.12 y axe
4.13.0. Se revisaron capturas de guía/escritorio y productos/móvil.

`npm --prefix App/frontend test`: 123/123; lint y build: código 0.
Regresión comercial: `npm --prefix App/tools run test:browser`, 10 comprobaciones
por motor Chromium/Firefox/WebKit. El backend no cambió; el harness repite
integración real de catálogo, planes, solicitudes, ventas y campañas.

Una primera corrida registró en WebKit una excepción intermitente de acceso al
recurso RSC de planes durante una navegación completa forzada por el runner.
La repetición sin cambios pasó. El recorrido ahora entra mediante el enlace real
«Administrar planes», manteniendo la aserción de cero excepciones; no se silencia
el error ni se atribuye una causa definitiva al producto.

Persisten resultados inconclusos de contraste en la guía (texto sobre imagen,
decoración y chips desplazables). Se conservan en el informe; no equivalen a una
validación manual ni se declara certificación WCAG. No se usaron exclusiones.
Demo y datos persistentes ajenos a las pruebas. Sin cambios de esquema ni ranking.
