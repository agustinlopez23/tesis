# Diseño técnico

DAL usa ST_DWithin y ST_Distance sobre geography SRID 4326, long/lat en ese
orden. SQL interpolado parametrizado mediante EF Core SqlQuery; el tipo
intermedio de lectura permite componer texto, categoría, orden y paginación.
No se transforma toda la tabla a memoria. Índice GiST sobre location.
BLL recibe un valor de área validado; no referencia EF ni NetTopologySuite.

DTO público: distanceKm nullable omitido cuando no hay centro.
La API valida parámetros antes de consultar. El frontend usa referencias
manuales versionadas y getCurrentPosition bajo acción explícita.
La zona manual se comparte por id; datos precisos del dispositivo no entran
en la URL. Un contador de solicitud descarta resultados tardíos.

Las coordenadas del dispositivo se guardan en history.state, preservando las
claves internas de Next. Se restauran en recarga/popstate. Una pestaña abierta
desde un enlace sin ese estado muestra la guía general y explica cómo elegir
su propia ubicación. La retención del historial depende del navegador.
Las consultas HTTP contienen coordenadas; los logs de acceso podrían incluirlas.
No se crean historial de ubicaciones en base ni almacenamiento local de aplicación.

Referencias manuales (consultadas 2026-09-18; coordenadas de puntos, no polígonos):

- [Plaza San Martín](https://www.wikidata.org/wiki/Q6079803): -31.416281, -64.184022.
- [Buen Pastor](https://www.wikidata.org/wiki/Q7141916): -31.423889, -64.186389.
- [Plaza Colón](https://es.wikipedia.org/wiki/Plaza_Col%C3%B3n_(C%C3%B3rdoba)): -31.4093, -64.19555.

Limitación: tres zonas de referencia iniciales; no hay buscador de direcciones
ni catálogo completo de barrios. El dispositivo puede aportar otro centro.

Rollback: revertir frontend/API y retirar el índice con migración Down;
no se eliminan negocios. Configuración y servicios existentes.

Fuentes: https://postgis.net/docs/ST_DWithin.html,
https://www.npgsql.org/efcore/mapping/nts.html,
https://learn.microsoft.com/en-us/ef/core/querying/sql-queries
