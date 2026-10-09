# Verificación — 05/10/2026

Rama `feat/sales-inventory`, base `d6ae72b`. Implementados inventario auditado,
ventas vinculadas/presenciales y anulación con devolución explícita.

| Comprobación | Resultado |
| --- | --- |
| `.tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Marketplace.Api.Tests'` | 127/127 API/HTTP/PostGIS; sin omitidas. |
| `npm test` en frontend | 105/105 en 20 archivos. |
| `npm run lint` y `npm run build` | Correctos. |
| `node App/tools/verify-catalog.mjs --sales --serve` | Catálogo, solicitudes, ventas e inventario correctos. |
| Migración | Sin cambios de modelo pendientes; Down poblado bloqueado, vacío reversible. |

TDD: cinco casos API fallaron con 404; luego pasaron. Agotar inventario dejó
inicialmente disponibilidad pública true; prueba roja y corrección. Downgrade
permitía perder saldo; prueba roja y guard SQL. Proxy falló con 404 y componentes
con import inexistente; después pasaron. Dos pruebas de UI necesitaron esperar
habilitación después de recuperar sessionStorage; la suite final quedó verde.

API: operaciones idempotentes, versiones, venta concurrente de últimas unidades,
atomicidad con múltiples productos, conservación del pedido, importes por origen,
aislamiento, plan vencido, nulidad/origen/cantidades/precios inválidos, CSRF,
JSON excesivo y devolución superior a venta sin mutación.

Integración real: saldo inicial 5, venta vinculada de 2 y replay sin doble descuento,
rechazo de venta de 4 sobre saldo 3, anulación con retorno de 1, replay y saldo 4.
El enlace privado reflejó converted y cancelled; tres movimientos, una venta.

Playwright: ingreso de 3 unidades, venta presencial de 2 interceptando PUT después
de commit y abortando su respuesta. Recarga/reapertura/reintento recuperaron la
misma venta. Nueva solicitud convertida desde panel, cantidad final 2 y precio
12500; anulación sin devolución conservó saldo. Consulta SQL final: 3 ventas,
saldo 3 y 6 movimientos. Rechazo de venta de 4 conservó campos editables.
1440/390/320 px sin desbordamiento horizontal. Capturas locales ignoradas en
`.tools/sales/`, inspección visual del formulario móvil. Únicos errores esperados:
respuesta abortada y 409 provocado. No se enviaron mensajes externos.

Base temporal eliminada y servidores cerrados. Demo sin migrar/reiniciar; sin
push, PR ni despliegue. Artefactos de sesión temporales están ignorados por Git.

Revisión: separar esquema/prueba, contrato API/pruebas, proxy, panel/pruebas y
cierre. Archivos de migración generados íntegros; recomendar `size:exception`
para unidades cohesivas superiores a 400 líneas tras una pasada de división.
Comandos y resultados anteriores cubren cada frontera; no se afirman resultados
de carga distribuida ni validación experimental humana.

Unidades locales, en orden de dependencia: `b37d591` esquema (1944 líneas),
`b5ad9c1` transacciones y API (499), `09e7438` proxy (20), `e400931` panel (315)
y el commit de cierre de este informe. Presupuesto: 400 líneas por revisión;
recomendación de `size:exception` en esquema generado y contrato transaccional
con sus pruebas. No hay PR publicada ni aprobación pendiente para commits locales.
