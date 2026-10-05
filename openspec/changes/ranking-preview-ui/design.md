# Diseño

`RANKING_PREVIEW_ENABLED=true` habilita la página y POST del proxy en development/test;
production siempre cerrado. GET local permanece disponible al apagar creación para
terminar cursores vigentes. El backend conserva su habilitación independiente.
Proxy limitado a rutas exactas, cuerpo 4096 bytes, origen confiable, query íntegra,
cookie de sesión, cancelación y no-store incluso en errores generados por Next.

Cada respuesta agrega `cursor` de la página actual con el mismo TTL absoluto.
UI usa GET para volver a cualquier página. Solo nueva consulta/actualizar genera
requestId; reintentar POST usa el original. La vista no crea listas al montarse.
Historial del navegador conserva contexto/cursor, sin poner tokens en la URL ni
guardar tarjetas. Recarga/atrás vuelve a consultar elegibilidad vigente.
Un cambio de filtros invalida la lista anterior; respuestas tardías se descartan.
410/vencimiento exige actualizar; 429/503/red permite reintentar; 404 ofrece guía.
Las tarjetas reutilizan perfil/contacto y tokens emitidos por el backend.

Verificar frontend y backend afectados, build/lint, integración Next/API/PostGIS
y Playwright en escritorio/móvil. Tres unidades previstas: contrato/proxy,
estado/navegación con pruebas, presentación y verificación integrada.
Rollback por unidad documentado con evidencia; sin migraciones ni datos de demo.
