# Evidencia TDD — incorporación de comerciantes

## Preparación y primer rojo — 2026-09-08

Se retomaron los archivos existentes sin implementar endpoints. Se descargó
`postgis/postgis:17-3.5` y se inició el servicio `database` de `App/compose.yaml`.
Docker confirmó estado `healthy`, con puerto limitado a `127.0.0.1:55432`.
Se conservaron las credenciales existentes de `App/.env.local`.

La solución compiló en Release con las dependencias restauradas previamente.
La primera ejecución falló al abrir la conexión (EndOfStreamException); ese
resultado es un fallo de entorno y no cuenta como evidencia roja funcional.

La repetición fuera del entorno restringido pudo crear y eliminar la base
temporal de la fixture y ejecutar las cuatro pruebas de OwnerAuthenticationTests:
0 aprobadas, 4 fallidas. Todas fallaron por HTTP 404 en `/auth/register`:
tres esperaban 201 y una esperaba 400. No hubo error de inicialización ni limpieza.

Comando de la repetición, desde la raíz y con el SDK local en PATH:

```powershell
dotnet test App/backend/Marketplace.sln --configuration Release --no-build --no-restore -m:1 --filter FullyQualifiedName~OwnerAuthenticationTests
```

La variable `TEST_DATABASE_CONNECTION` apuntó al servidor local, base `postgres`,
usuario `marketplace`, con contraseña leída del archivo local sin imprimirla.
La fixture usa una base `onboarding_test_<guid>` distinta en cada ejecución.

Esta evidencia demuestra el rojo inicial del registro. Las aserciones de login,
sesión, revocación y hashes todavía no se alcanzaron porque dependen de registrar
una cuenta. Deben observarse sus fallos específicos antes de implementar esos
comportamientos. La tarea roja completa de autenticación permanece abierta.

Siguiente paso: implementar registro con persistencia y migración explícita;
repetir las pruebas para obtener el rojo específico del inicio de sesión.

## Registro verde y sesión roja — 2026-09-08

Se agregó el modelo mínimo de identidad, el repositorio EF/Npgsql y la migración
`InitialOwnerIdentity`. La siguiente ejecución produjo 1 prueba aprobada y 3
fallidas: el registro pasó y los tres recorridos siguientes recibieron HTTP 404
en `/auth/login`. Esto confirmó el rojo específico de sesión.

Al ejecutar la migración desde la fixture apareció primero un error de contraseña:
la conexión se había leído antes de que `WebApplicationFactory` agregara la
configuración de pruebas. Se difirió esa lectura hasta crear el DbContext; la
fixture volvió a usar correctamente su base temporal.

## Autenticación verde y limitación por IP — 2026-09-08

Se implementaron login, consulta y cierre de sesión con token aleatorio de 256
bits, hash SHA-256 persistido, vencimiento absoluto de ocho horas y cookie
HttpOnly/SameSite=Lax. La corrida siguiente aprobó las 4 pruebas originales.

Luego se agregó una prueba aislada con límite de un intento. El rojo esperado fue
HTTP 401 en el segundo login en lugar de 429. Tras aplicar el rate limiter por IP,
la prueba pasó. La verificación final de toda la solución obtuvo 6/6 pruebas
aprobadas en Release, incluidas las pruebas de liveness del scaffold.

Durante la compilación se detectó un conflicto entre EF Core 10.0.11 y la versión
relacional 10.0.4 usada por Npgsql 10.0.3. EF Core y Design se alinearon en 10.0.4;
la compilación posterior terminó con 0 advertencias y 0 errores.

La migración fue aplicada y revertida indirectamente en bases temporales distintas
por corrida. En esta etapa no se modificó la base persistente `marketplace`.

## Negocios propios — 2026-09-08

Se agregaron tres pruebas HTTP antes de los endpoints. Las tres fallaron al pedir
`/business-categories` con HTTP 404. Luego se implementaron categorías semilla,
alta, listado, consulta y edición por propietario, junto con la migración
`AddOwnerBusinesses`.

La corrida verde aprobó 3/3 pruebas. Verificó además con SQL real que la ubicación
persistida tiene SRID 4326 y conserva latitud/longitud, que una sesión ajena recibe
404 al consultar o modificar y que una edición inválida no altera el registro.

## Proxy y panel frontend — 2026-09-08

Las pruebas iniciales fallaron al importar el proxy y el panel todavía inexistentes.
Se implementó un Route Handler con lista cerrada de rutas, control de origen en
mutaciones, reenvío de cookie y configuración de API sólo del lado servidor. El
panel permite registro, ingreso, alta/edición de negocios y cierre de sesión; muestra
errores sin borrar email, nombre ni datos comerciales no sensibles.

La primera corrida verde obtuvo 3/4 pruebas; la restante comparaba un objeto
`Headers` como si fuera un objeto literal. Ajustada la aserción para leer los
encabezados reales, la suite frontend completa aprobó 24/24 pruebas. El build de
producción terminó correctamente y generó la ruta dinámica
`/api/marketplace/[...path]`.

## Recorrido integrado — 2026-09-08

`node App/tools/verify-onboarding.mjs` creó una base PostgreSQL temporal, aplicó
ambas migraciones, inició la API y el build de Next y atravesó el proxy de mismo
origen. Resultados: registro 201, login 200, creación 201, edición 200, logout 204
y reutilización de la cookie revocada 401. El script detuvo ambos procesos y
eliminó únicamente la base temporal al finalizar. El resultado local se guardó en
`.tools/onboarding-smoke-result.json`, directorio ignorado por Git.

Una prueba adicional del panel verificó el alta visible de un negocio. También se
agregó un ciclo rojo/verde para base inaccesible: inicialmente la excepción de
Npgsql escapaba del servidor; el manejador final devuelve Problem Details 503 y
`/health/live` continúa en 200.

Verificación final: backend 10/10 pruebas, frontend 25/25 pruebas, ESLint sin
hallazgos, build Next exitoso y `git diff --check` sin errores de whitespace.

Después de cerrar la verificación se aplicaron las dos migraciones explícitas a la
base local `marketplace`. La comprobación final encontró 0 cuentas, 0 negocios y
las 3 categorías semilla; no se copiaron datos del recorrido temporal.
