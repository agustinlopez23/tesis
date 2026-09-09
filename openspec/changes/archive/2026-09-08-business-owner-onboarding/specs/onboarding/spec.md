# ADDED Requirements

## Requirement: Registro de responsable
POST `/auth/register` acepta email, displayName y password; asigna únicamente business_owner. Email normalizado único; nombre 1–120, email válido hasta 254, contraseña 12–128 caracteres sin truncar. No admite campos para conceder rol administrador.

### Scenario: Cuenta válida
- WHEN se presenta una cuenta válida
- THEN responde 201 con id, email, displayName y roles; nunca hash o contraseña.

### Scenario: Datos inválidos o correo repetido
- WHEN falla validación o el email normalizado ya existe
- THEN responde 400 o 409, respectivamente; la restricción única también impide duplicados concurrentes.

## Requirement: Sesión privada y revocable
POST `/auth/login` devuelve identidad segura y cookie HttpOnly, SameSite=Lax, Path=/, Secure fuera de Development/Testing. Token aleatorio de 256 bits, hash en base, duración absoluta 8 horas. GET `/auth/session` devuelve identidad o 401. POST `/auth/logout` revoca sesión y elimina cookie, de forma idempotente. Contraseñas con PasswordHasher de ASP.NET Core.

### Scenario: Ingreso y salida
- WHEN las credenciales son válidas y luego se cierra sesión
- THEN la cookie permite consultar sesión antes y reutilizar el token después devuelve 401.

### Scenario: Credenciales, suspensión o vencimiento
- WHEN la contraseña no coincide, la cuenta está suspendida o la sesión venció/revocó
- THEN no se concede acceso; login usa el mismo mensaje para cuenta inexistente e inválida.

## Requirement: Protección de solicitudes
Los métodos con efectos requieren Origin exactamente igual al origen configurado de frontend y encabezado X-Requested-With=marketplace. El proxy de Next aplica el mismo control y no expone un proxy arbitrario. Login/registro tienen limitación por IP. No almacenar token en localStorage ni devolverlo en JSON.

### Scenario: Petición desde otro origen
- WHEN una mutación no tiene origen válido o encabezado requerido
- THEN devuelve 403 antes de modificar datos.

## Requirement: Negocios del propietario
GET `/business-categories` devuelve categorías semilla. GET/POST `/businesses` lista/crea negocios del responsable. GET/PUT `/businesses/{id}` obtiene/edita solo un negocio propio. Cada perfil tiene nombre 1–160, descripción hasta 2000, dirección hasta 250, categoryId válido, teléfono internacional con + y 8–15 dígitos, latitud [-90,90], longitud [-180,180] finitas y estado draft o published. Una cuenta puede tener varios negocios, cada uno con una ubicación. La API asigna OwnerId, nunca confía en el cliente.

### Scenario: Guardado persistente
- WHEN un responsable crea un negocio válido y lo consulta en otra petición
- THEN recupera los datos exactos y las coordenadas almacenadas como geography(Point,4326).

### Scenario: Acceso cruzado o sin sesión
- WHEN falta sesión o se solicita un negocio ajeno
- THEN devuelve 401 o 404, respectivamente, sin modificarlo ni exponer sus datos.

### Scenario: Edición y validación
- WHEN el dueño modifica su perfil válido
- THEN conserva id/propietario y persiste cambios; coordenadas fuera de rango, categoría inexistente o campos inválidos producen 400 sin escritura parcial.

## Requirement: Persistencia y experiencia
Migración explícita crea solamente cuentas, roles, sesiones, rubros y negocios de este corte. UI en español con estados de envío, error, sesión vencida y lista vacía. No requiere base durante build Next ni para `/health/live`. Una operación de datos con base inaccesible devuelve error controlado, no éxito.

### Scenario: Demostración de extremo a extremo
- WHEN el usuario registra, ingresa, guarda y edita su negocio desde el panel
- THEN se muestran datos persistidos y al salir vuelve al formulario de acceso.
