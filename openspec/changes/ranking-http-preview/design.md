# Diseño y unidades

1. Cursor API con Data Protection y propósito independiente `Cerca.RankingCursor.v1`.
   Payload autenticado: snapshot, offset, tamaño 20, versión 1 y vencimiento absoluto.
   Protector sin expiración automática: permite distinguir autenticidad inválida
   (400) de vencimiento auténtico (410). El reloj se consulta después de descifrar.
   No incorpora filtros, visitante ni coordenadas. Límite 2048 caracteres.
2. POST/GET en módulo API separado. DTO público con items, measurement, expiresAt,
   rankingVersion, hasMore y nextCursor. Reutiliza servicios transaccionales y
   páginas revalidadas. La medición v1 conserva posición original y hash de política.
   La previsualización local usa EquityOnly/default/seed 17, política de prueba sin
   aprobación. `Ranking:PreviewEnabled` false por defecto y sin efecto en producción.
   Apagar creación permite terminar cursores; `Ranking:WithdrawPages` retira con 410.
3. Pruebas HTTP de errores, límites y regresión; cierre de especificación/continuidad.

Unidades locales con pruebas y documentación, cada una objetivo <=400 líneas;
mantener cadena local desde la rama de persistencia, sin publicación. RED/GREEN
por comportamiento. Tests de cursor puros; integración en bases PostGIS efímeras
mediante `.tools/test-publication-history.ps1`. No cambiar dependencias.

Rollback: unidad 1 elimina RankingCursorTokens y sus pruebas; unidad 2 elimina
módulo/contratos/registro API y sus pruebas, conservando almacenamiento interno;
unidad 3 elimina ampliaciones de pruebas y revierte documentación de estado.
No hay migración. Ningún rollback elimina oportunidades previamente guardadas.
