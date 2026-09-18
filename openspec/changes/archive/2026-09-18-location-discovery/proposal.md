# Descubrimiento por ubicación

Un visitante puede elegir una zona manualmente o compartir su ubicación,
seleccionar un radio y descubrir comercios publicados dentro de ese radio.
La distancia representa línea recta; no se promete tiempo de viaje.

Incluye combinación con búsqueda/rubro, orden por distancia, paginación,
ubicación opcional, recuperación cuando se deniega el permiso e índice espacial.
Sin ubicación se conserva la guía general alfabética. No se exige cuenta.

Fuera de este incremento: mapas interactivos, geocodificación externa,
límites oficiales de barrios, perfil con fotos, ranking equitativo y pedidos.
Son los bloques siguientes de la secuencia autorizada.

Depende de PostGIS existente, sin nuevos servicios ni paquetes. Éxito:
pruebas rojas/verdes de API con base real, interfaz y recorrido MCP Playwright.
