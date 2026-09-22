# Historial de primera publicación

Registrar la primera publicación persistida sin rejuvenecer negocios al editarlos
o republicarlos. Es el primer prerrequisito de integración del ranking acordado
en `Doc/continuidad.md`; el usuario pidió retomar los pendientes.

Incluye fecha nullable, marca explícita de historia desconocida, migración y
pruebas sobre PostgreSQL/PostGIS. Todos los registros anteriores se marcan como
desconocidos: tampoco un borrador antiguo demuestra que nunca se haya publicado.
No incluye candidatos completos, snapshots, ranking público ni evaluación humana.
Éxito: altas y transiciones conservan historia incluso con escrituras atrasadas;
la guía mantiene sus contratos y su orden.
