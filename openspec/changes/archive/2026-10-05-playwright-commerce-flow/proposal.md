# Correcciones del recorrido comercial comprobado en navegador

Dejar recuperables y coherentes los flujos comerciales probados con Playwright.
Playwright sobre Next/API/PostGIS aislados reprodujo cuatro problemas: inventario y
solicitud abiertos quedan obsoletos al registrar/anular una venta desde otro panel;
el catálogo actualizado no renueva precio/versión de los productos seleccionados,
por lo que una solicitud rechazada vuelve a entrar en conflicto; una escritura con
sesión vencida muestra un mensaje ambiguo; el ingreso incorrecto no ofrece ayuda.

Sincronizar paneles del mismo negocio después de escrituras confirmadas y actualizar
selección desde productos consultados, con aviso y nueva confirmación del cliente.
No modificar intentos pendientes de respuesta ni precios de solicitudes guardadas.
Agregar regresiones primero, repetir recorridos con Playwright y documentar la
cobertura, fallas simuladas y límites de la revisión. No tocar demo ni proveedores.

Dependencias: harness comercial existente, Next compilado, API Release y PostGIS.
Éxito: regresiones rojas/verdes, correcciones verificadas en navegador y pruebas,
lint y build aprobados. Fuera de alcance: proveedores reales, despliegue, pruebas
de carga, matriz multinavegador y evaluación científica del ranking.
