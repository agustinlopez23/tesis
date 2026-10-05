# Búsqueda pública de productos

GET `/public/products` MUST aceptar page 1–10000, q hasta 150 caracteres, category
hasta 100 y ubicación opcional lat/lon/radiusKm conjunta y válida (0,1–50 km).
Entradas inválidas producen 400; toda respuesta es no-store. El filtro category
corresponde al rubro del negocio. Texto busca nombre, descripción, categoría del
producto y nombre del negocio, sin distinguir mayúsculas o acentos de vocales.
Los caracteres de LIKE se buscan literalmente; las consultas son parametrizadas.

Cada lectura MUST excluir negocio no publicado, titular suspendido, plan vencido/
revocado, producto no publicado/no disponible y saldo conocido cero. Inventario
desconocido conserva disponibilidad declarada. No se expone el saldo exacto.
Foto se sirve mediante la ruta pública existente y su propia elegibilidad.

Páginas de 20 con hasNextPage. Sin ubicación: nombre e ID; con ubicación:
distancia geográfica en metros convertida a km, luego nombre e ID. Radio calculado
con ST_DWithin sobre geography. No es ranking de relevancia ni equidad; no genera
exposición experimental. Paginación revalida datos vivos y no promete snapshot fijo.

`/productos` MUST conservar texto/rubro/página/zona al navegar y recargar, ofrecer
zona manual o ubicación del dispositivo y reintentar errores sin mostrar tarjetas
obsoletas. Coordenadas del dispositivo van solo a la API y al estado de historial,
no a URL compartible. Cada tarjeta identifica negocio y conduce a su catálogo;
la selección y solicitud continúan siendo de un solo negocio.
