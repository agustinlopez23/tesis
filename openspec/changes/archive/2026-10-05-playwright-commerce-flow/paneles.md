# Paneles coherentes después de guardar

Playwright reprodujo inventario obsoleto después de vender y una solicitud que
seguía convertida después de anular. Dos regresiones en `commerce-sync.test.tsx`
fallaron antes de implementar el aviso local por negocio; ambas quedaron verdes.

`business-refresh.ts` emite solo después de escrituras confirmadas. Los paneles
suscriptos consultan la API con sus filtros actuales. El detalle de solicitud
se actualiza en segundo plano para no desmontar una venta todavía en edición.
No hay nuevas dependencias, API, migración ni sincronización entre pestañas.

Verificación: las dos regresiones pasaron dentro de la ejecución de 22 pruebas
`npm test -- tests/owner-onboarding.test.tsx tests/order-checkout.test.tsx
tests/commerce-sync.test.tsx tests/sales.test.tsx` desde `App/frontend`.
Con `node App/tools/verify-catalog.mjs --campaigns --serve` y Playwright:
stock 4 → venta de 2 → 2 → anulación con devolución de 1 → 3; solicitud cancelada
y métricas actualizadas sin recarga. Alta manual aparece como «Sin cargar» en
inventario abierto. Stock cero bloquea campaña y reposición la reactiva.

Rollback: revertir el hook, sus emisores/suscripciones y estas dos regresiones;
los botones de actualización manual y los contratos del servidor siguen vigentes.
