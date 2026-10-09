# Datos de demostración de Cerca

`demo.json` contiene tres comercios ficticios de Córdoba y doce productos. Cada
negocio recibe un plan Impulso de 365 días desde la primera carga. Se crean dos
cuentas independientes: `comerciante.seed@example.test` (titular de los tres
negocios) y `admin.seed@example.test` (administración de planes/campañas).

| Caso | Datos |
| --- | --- |
| Almacén / Centro | Yerba, arroz, aceite sin stock y conserva no disponible |
| Café / Nueva Córdoba | Café, medialunas, té en borrador y chocolate archivado |
| Tienda / Alberdi | Remera, buzo, bolso en USD y bufanda |

Ocho productos aparecen en la búsqueda global. El perfil también muestra los
productos publicados no disponibles, con selección deshabilitada. Cada saldo
inicial tiene un movimiento auditado. Direcciones, precios y WhatsApp son ficticios.
No se crean ventas, solicitudes, fotos, campañas pagadas ni anotaciones de ranking.

## Cargar la demo de este workspace

Con Docker/PostGIS iniciado y la base `marketplace` migrada, desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File App/tools/seed-demo.ps1
```

El comando usa el SDK local si existe y lee la conexión de `App/.env.local`.
Genera contraseñas aleatorias distintas la primera vez y las conserva en
`.tools/seed-demo/credentials.json`, ignorado por Git. Abrí ese archivo para ingresar
en http://localhost:3000/mi-negocio. No borrar ni compartir las credenciales.
Si falla la carga, el archivo se conserva para reintentar con la misma identidad.
No levanta servidores, migra ni borra datos; no requiere la API en ejecución.

## Carga desde .NET

Requiere PostgreSQL/PostGIS local con las migraciones actuales. Configurar en el
proceso `ConnectionStrings__Marketplace`, `ASPNETCORE_ENVIRONMENT=Development`
o `Testing`, `SEED_OWNER_PASSWORD` y `SEED_ADMIN_PASSWORD` (distintas, 12–128
caracteres). Desde la raíz:

```powershell
dotnet run --project App/backend/tools/Marketplace.Seed --configuration Release -- --apply
```

El archivo JSON se copia al compilar. Editarlo permite agregar registros con UUID
nuevo; conservar identificadores para reejecutar. Los registros existentes no se
actualizan: precios, stock, permisos, contraseñas, estados y planes se gestionan
desde la aplicación. Un plan vencido/revocado no se renueva al repetir el seed.

La carga usa una transacción y serializa ejecuciones simultáneas. Un ID o correo
ocupado por otra identidad aborta todo. No se restablecen contraseñas; conservar
las credenciales originales. No existe borrado/reset automático de datos.
Rollback del código: retirar Marketplace.Seed, su referencia de pruebas y el
dataset; los datos ya cargados permanecen. Recuperar datos requiere un respaldo.

Verificación: `dotnet test App/backend/tests/Marketplace.Api.Tests --filter
FullyQualifiedName~DemoSeedTests`, con `TEST_DATABASE_CONNECTION` hacia `postgres`
local. Las pruebas crean/eliminan bases aisladas: autenticación real, permisos,
catálogo, pedido/venta, repetición concurrente, conservación y rollback.
