# Diseño del primer caso de negocio

## Arquitectura

Models contiene entidades simples y GeoLocation; DTOs define entrada/salida; BLL valida y coordina por interfaces propias; DAL implementa EF Core/PostGIS; IoC registra persistencia y servicios. API implementa transporte, protección de origen, limitación y adaptador PasswordHasher. BLL no referencia DAL ni EF. No se agregan Utility o Integrations por anticipación.

GeoLocation no depende de NetTopologySuite: el mapeo DAL convierte longitud/latitud a Point con SRID 4326 y tipo geography. El ranking futuro deberá especificar consultas espaciales antes de introducir traducciones SQL complejas.

## Identidad

No usar JWT para sesiones revocables de este corte. Token opaco generado con RandomNumberGenerator, hash SHA-256 persistido y comprobación del estado de cuenta en cada acceso. PasswordHasher del framework para contraseñas, con rehash cuando su verificación lo indique. Registro no permite selección de privilegios. Los secretos no se guardan en archivos versionados.

El navegador usa `/api/marketplace/...` del mismo origen. El Route Handler Next valida Origin contra APP_ORIGIN y encabezado, reenvía solo rutas permitidas y la cookie de sesión a la API interna. La API repite validación contra FrontendOrigin, sin CORS abierto. La API emite cookie host-only HttpOnly y el proxy transmite Set-Cookie sin exponer token en el cuerpo. Secure obligatorio en producción. API_BASE_URL es configuración de servidor; no contiene credenciales ni es controlada por el cliente.

La autenticación usa rate limit por IP en memoria, adecuado al único proceso del prototipo, no distribuido. En producción deben definirse proxy confiable, TLS y retención antes de un piloto real. Recuperación/verificación de correo no forman parte de este corte.

## Base y ejecución

Compose fija PostGIS 17-3.5, enlazado únicamente a loopback y puerto 55432. Contraseña aleatoria local en App/.env.local ignorado; ejemplo sin contraseña efectiva. Las pruebas usan una base aislada por corrida dentro del servidor local y la eliminan al terminar. No usar EF InMemory.

Inicialización explícita de esquema mediante `--migrate`; nunca migraciones automáticas en cada inicio productivo. La API puede responder liveness sin base y las operaciones persistentes muestran indisponibilidad controlada. Las categorías semilla son rubros de demostración, no datos obtenidos de comercios reales.

## Versiones verificadas

Se conserva .NET SDK 10.0.400 y frontend existente. EF Core 10.0.4 y proveedor Npgsql EF/PostGIS 10.0.3 quedan alineados para evitar conflictos de ensamblados observados durante la compilación. Las referencias exactas y la imagen usada quedarán en archivos de proyecto/Compose y evidencia.

- [Npgsql EF Core 10.0.3](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3).
- [Npgsql NetTopologySuite](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite/10.0.3).
- [EF Core Design](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.4).
- [Imagen oficial del proyecto PostGIS](https://github.com/postgis/docker-postgis).
- [Protección antifalsificación de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).

Se leyeron las guías locales Next de Route Handlers y cookies asíncronas, conforme a AGENTS.md del frontend.
