# Requisitos de onboarding del responsable

## Registro de responsable

POST `/auth/register` acepta email, displayName y password; asigna únicamente
`business_owner`. Email normalizado único; nombre 1–120, email válido hasta 254,
contraseña 12–128 caracteres sin truncar. No admite campos para conceder rol
administrador. Una cuenta válida responde 201 sin exponer contraseña ni hash;
datos inválidos o correo repetido responden 400 o 409.

## Sesión privada y revocable

POST `/auth/login` devuelve identidad segura y cookie HttpOnly, SameSite=Lax,
Path=/, Secure fuera de Development/Testing. El token opaco tiene 256 bits, se
guarda como hash y vence a las ocho horas. GET `/auth/session` devuelve identidad
o 401. POST `/auth/logout` revoca la sesión y elimina la cookie de forma idempotente.
Las contraseñas usan PasswordHasher de ASP.NET Core.

Un ingreso rechazado con 401 MUST indicar que se revisen correo y contraseña,
sin afirmar cuál es incorrecto ni revelar si la cuenta existe. Conservar el correo
para poder corregir los datos y reintentar.

## Protección de solicitudes

Los métodos con efectos requieren Origin exactamente igual al origen configurado
y `X-Requested-With=marketplace`. El proxy Next repite el control, usa una lista
cerrada de rutas y no expone el token. Login y registro tienen límite por IP.

## Negocios del propietario

GET `/business-categories` devuelve categorías semilla. GET/POST `/businesses` y
GET/PUT `/businesses/{id}` operan únicamente sobre el responsable autenticado.
El perfil valida nombre, descripción, dirección, categoría, WhatsApp internacional,
latitud, longitud y estado draft/published. El acceso cruzado devuelve 404 y la
ubicación se persiste como `geography(Point,4326)`.

## Persistencia y experiencia

Las migraciones explícitas crean cuentas, roles, sesiones, rubros y negocios. El
panel en español cubre registro, ingreso, lista vacía, alta, edición, errores,
sesión vencida y salida. La compilación de Next y `/health/live` no requieren base;
una operación de datos con la base inaccesible devuelve un error controlado.
