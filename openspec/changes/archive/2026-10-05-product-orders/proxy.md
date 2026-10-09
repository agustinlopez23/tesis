# Transporte de solicitudes

El proxy permite únicamente las rutas de creación, seguimiento y gestión
definidas en la spec. Reenvía `X-Order-Key` solo al GET privado y descarta query
en esa ruta; la lista del titular conserva únicamente page/reference/status.
Limita cuerpos de solicitudes a 16 KiB y conserva no-store en las respuestas.

Verificación: dos pruebas en `tests/order-proxy.test.ts`, incluidas en la suite
frontend final 100/100; integración real con `verify-catalog.mjs --orders --serve`
correcta. Reversión: quitar rutas/cabecera/filtros añadidos en
`src/app/api/marketplace/[...path]/route.ts` y sus dos pruebas; las pantallas de
solicitudes dependientes deben retirarse primero.
