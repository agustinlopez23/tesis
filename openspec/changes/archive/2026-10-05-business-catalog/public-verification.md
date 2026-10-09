# Catálogo público y proxy

Unidad: perfil público, búsqueda/paginación local, transporte compartido, rutas
permitidas y estilos. Depende de almacenamiento y API de catálogo.

Vitest: los dos casos de `catalog-public.test.tsx` pasan; la suite completa antes
de separar archivos pasó 90/90. El proxy conserva filtro/página y rechaza PUT
público. Un 404 posterior retira productos visibles. Build y lint correctos.

Runtime: `node App/tools/verify-catalog.mjs`; Next production y PostgreSQL real,
flujo de alta/archivo/revocación correcto. Playwright: dos productos, filtro
Sandwiches muestra uno, foto JPEG visible, 1440/390/320 px sin desborde.
Rollback: retirar `PublicCatalog` del perfil, componente, rutas nuevas, helper y
estilos solo cuando no existan paneles que dependan de ellos. Conserva API/datos.
