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
