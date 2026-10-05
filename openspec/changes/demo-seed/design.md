# Herramienta local de carga

Consola Marketplace.Seed separada del servidor y referenciada solo por pruebas.
Usa DAL/EF/PostGIS y PasswordHasher del framework instalado; ningún paquete nuevo.
JSON versionado con IDs estables. Transacción y advisory lock entre corridas.
Solo inserta registros faltantes y comprueba pertenencia ante colisiones. No
restablece roles ni planes revocados. Stock inicial conserva movimiento de ajuste.

Unidad 1: consola, datos, contrato de uso y tres pruebas de integración, debajo
de 400 líneas. Unidad 2: comando PowerShell que prepara credenciales aleatorias
en `.tools/seed-demo/credentials.json`, documentación y evidencia de carga local.
El wrapper conserva/restaura variables del proceso y no imprime contraseñas.

Rollback: retirar herramienta/wrapper sin modificar el esquema ni la aplicación.
No borrar datos de prueba automáticamente cuando puedan tener ventas asociadas.
Respaldo restaurado previo a carga: `.tools/demo-backups/20261005-131041/`.
