# Navegación comercial — 06/10/2026

`npm --prefix App/frontend test -- tests/site-navigation.test.tsx`: 3/3 fallaban
por no encontrar Productos antes de implementar; después pasan, verificando
destinos y única sección actual en negocios, productos y gestión.

`npm --prefix App/frontend test`: 126/126. `lint` y `build`: código 0.
`node App/tools/verify-catalog.mjs --campaigns --accessibility --browsers`:
18 análisis axe sin violaciones/desbordamiento y 30 controles comerciales
aprobados en los tres motores. Navegación cabe en 320/390/768/1440; objetivos
móviles de 44 px y enlace Productos operable por teclado. Captura de Productos
a 320 px revisada: tres destinos visibles, estado activo y separación del título.

Evidencia local: `.tools/campaigns/accessibility-result.json`, capturas y
`browser-result.json`. La base temporal se elimina al terminar. Se mantienen los
límites de revisión humana indicados en el informe de accesibilidad anterior.

Demo persistente actualizada mediante copia compilada independiente; Playwright
confirmó navegación, catálogo del seed, estilo nuevo, acceso, móvil y API sin
errores JS/5xx. Se conserva la copia anterior. API/base sin modificaciones;
comprobación posterior: 6 negocios, 12 productos, 1 venta y arroz en 28 unidades.
Sin bases `catalog_smoke_*` ni servicios de prueba escuchando en 3200/5280.
