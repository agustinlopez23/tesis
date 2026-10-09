# Reusar el aislamiento del harness comercial

`verify-catalog.mjs --campaigns --browsers` importa el recorrido dentro de su
try/finally existente. Por eso tanto un fallo de Playwright como un éxito detienen
los procesos temporales y eliminan su base. No se inicia un segundo coordinador
ni se termina el proceso padre por fuerza. Los motores corren secuencialmente
sobre la misma base; cada venta se anula con devolución completa antes de seguir.

Dependencia Playwright 1.63.0 fijada en App/tools, con lockfile y navegadores
aislados. La UI usa contextos nuevos por motor, login desde formulario y espera
logout confirmado antes de navegar. La pérdida de respuesta ocurre después de
que la API guardó: se verifica reintento con igual cuerpo e identidad.

Resumen por motor y capturas locales; nunca se serializan contraseñas, cookies o
claves de pedidos en el reporte. No hay cambios de API, frontend, esquema o seed.
Rollback: retirar browser-flow, sus argumentos/import del harness y el paquete
de herramientas; los comandos HTTP anteriores del harness siguen independientes.
