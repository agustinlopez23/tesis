# Datos de demostración repetibles

Un archivo JSON editable y un comando local cargan tres negocios, doce productos,
stock inicial auditado, planes de un año y cuentas separadas de titular/admin.
Se agregan datos ficticios identificables sin modificar negocios existentes.
Repetir conserva cambios posteriores, saldos, contraseñas, roles y vigencias.

Herramienta de consola separada de la API: EF/PostGIS y PasswordHasher existentes,
sin paquetes nuevos ni cambios de arquitectura de la aplicación. Solo entorno
Development/Testing y conexión local explícita. Toda la carga en una transacción.
Contraseñas aleatorias locales fuera de Git; no hay credenciales compartidas.

TDD: carga/autenticación real, repetición tras editar/vender, preservación de datos
ajenos y rollback ante colisión. Verificar primero en base temporal, luego cargar
la demo respaldada y revisar con Playwright. Fuera de alcance: pagos, campañas
contratadas ficticias, pedidos históricos inventados, IA y ranking experimental.
