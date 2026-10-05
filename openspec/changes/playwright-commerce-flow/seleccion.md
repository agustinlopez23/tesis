# Recuperar una selección con precio actualizado

Playwright reprodujo un ciclo de 409: «Actualizar catálogo» renovaba las tarjetas,
pero el pedido seguía enviando la versión vieja. La regresión falló antes de
reconciliar la selección editable con los productos de la respuesta. Otra prueba
roja cubrió el botón todavía habilitado al quedar la selección vacía.

El cliente ve un aviso y confirma otra vez; no hay envío automático. Se preservan
productos ausentes de la página consultada y todos los intentos pendientes o
confirmados. Un producto retirado de publicación puede quitarse manualmente de la
selección; no se infiere su retiro a partir de una página filtrada.

Las nueve pruebas de `order-checkout.test.tsx` pasan en la ejecución conjunta de
22 indicada en `paneles.md`. Playwright con el harness aislado confirmó precio
13000 → 13500: 409, actualizar, nuevo total, 201. Una respuesta perdida después
del commit se recuperó con 200, idéntico cuerpo e ID, incluso tras actualizar
catálogo. La clave privada no aparece en el enlace de WhatsApp.

Rollback: revertir reconciliación/aviso en `use-order-cart`, `public-catalog` y
`order-checkout`, junto a las regresiones añadidas. No afecta datos persistidos.
