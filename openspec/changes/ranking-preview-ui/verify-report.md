# Evidencia incremental

## Cursor actual y proxy

RED API: Http_pages_retries falla al buscar `cursor` ausente. GREEN:
`.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~RankingHttpTests'`
24/24 HTTP/PostGIS temporal, cero omitidas, sin advertencias. Primera y segunda
página releídas vía sus propios cursores conservan IDs y una sola oportunidad.

RED proxy: 11/11 fallan por ruta inexistente/no-store ausente. Corregido un Request
mal construido en el fixture (Request usado como RequestInit), sin cambiar contrato.
GREEN: `npm test -- tests/ranking-proxy.test.ts tests/marketplace-proxy.test.ts`,
17/17. Harness llama handlers Next con Request/Response reales y upstream simulado:
sesión, cuerpo, query íntegra, estados 200/400/409/410/429/503, origen/cuerpo,
cancelación y fallo de red no-store. Producción cerrado incluso con flag true;
GET local continúa al deshabilitar POST. Integración real Next/API pendiente del
cierre de UI. Rollback: campo Cursor/traducción HTTP y aserciones; rutas/guard
del proxy, propagación de cancelación y no-store; configuración y pruebas asociadas.

Unidad contrato/proxy: `9289572`.

## Estado de navegación

Pruebas añadidas junto a implementación (sin RED atribuido):
`npm test -- tests/ranking-navigation.test.tsx`, **8/8**. No crea al montar;
reintento conserva requestId incluso tras remonte; navegación/recarga usa GET,
sin almacenar tarjetas ni poner tokens en URL. Frontera React/fetch simulada:
429/503/502, cancelación y respuesta tardía, huecos, popstate, cambio de versión,
vencimiento local y actualización con nueva identidad. Integración real pendiente
de presentación. Rollback: hook use-ranking-preview y sus pruebas, sin tocar proxy.
