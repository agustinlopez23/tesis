# Navegación visible y consistente

Se agrega Productos al SiteHeader compartido utilizando Link y aria-current,
igual que las otras secciones. En pantallas de hasta 600 px, la marca y los tres
enlaces se distribuyen en dos filas: no hay menú oculto ni JavaScript adicional.
Los enlaces móviles miden al menos 44 px de alto. El contenido de Productos usa
separación superior adaptable al ancho, manteniendo sus formularios y filtros.

Rollback: revertir el enlace en ui.tsx, las reglas de navegación/productos en
globals.css y sus comprobaciones en site-navigation.test.tsx/accessibility-flow.mjs.
No hay migración, cambios de API ni nuevas dependencias.
