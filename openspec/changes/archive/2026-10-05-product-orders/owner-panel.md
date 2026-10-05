# Atención desde el negocio

«Ver solicitudes» carga la lista del negocio editado. El titular puede buscar
por referencia, filtrar por estado, paginar y abrir el detalle. Guardar atención,
contacto recibido o cancelación incluye la versión leída; un conflicto permite
actualizar antes de volver a decidir. Contacto confirmado y cancelación son
definitivos. El estado queda separado de cualquier venta futura.

Verificación: dos pruebas en `tests/owner-orders.test.tsx`, incluidas en 100/100
frontend; API 120/120. Playwright verificó atención y contacto con reflejo en el
enlace del cliente a 1440/390/320 px. Reversión: quitar montaje en
`owner-onboarding.tsx`, `business-orders.tsx` y sus pruebas; API y solicitudes
guardadas pueden permanecer intactas.
