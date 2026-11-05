# Despliegue de la demo online

Arquitectura: **web** Next.js en Vercel → **API** .NET 10 en Railway → **base** PostgreSQL/PostGIS en Supabase.
El navegador solo habla con la web; la web llama a la API desde el servidor (proxy `/api/marketplace`).
La cookie de sesión queda en el dominio de Vercel, por eso no hace falta CORS.

Orden: 1) Supabase → 2) secreto → 3) Railway → 4) Vercel → 5) completar `FrontendOrigin` → 6) seed → 7) prueba.

## 1. Supabase (base de datos)

1. Crear un proyecto en https://supabase.com, región **South America (São Paulo)**. Guardar la contraseña de la base.
2. Habilitar PostGIS: *Database → Extensions → postgis → Enable*. La primera migración también ejecuta
   `CREATE EXTENSION IF NOT EXISTS postgis`, pero habilitarla desde el panel evita problemas de permisos.
3. Copiar la conexión: botón *Connect* → **Session pooler** (funciona por IPv4; la conexión directa es solo IPv6).
   Convertirla al formato de Npgsql:

   ```text
   Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<PROJECT_REF>;Password=<DB_PASSWORD>;SSL Mode=Require
   ```

   El host exacto (`aws-0-…` o `aws-1-…`) es el que muestra Supabase. `SSL Mode=Require` cifra la conexión;
   en Npgsql actual `Trust Server Certificate` ya no tiene efecto, no hace falta agregarlo.

## 2. Secreto compartido web ↔ API

La API confía en la IP del visitante que envía la web solo si llega con este secreto (sirve para limitar
intentos de inicio de sesión por visitante y no por servidor de Vercel). Generarlo una vez:

```bash
openssl rand -hex 32          # Git Bash
```

En PowerShell 7: `[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))`.

Usar el mismo valor en Railway (`Proxy__SharedSecret`) y en Vercel (`API_PROXY_SECRET`). No commitearlo.

## 3. Railway (API)

1. *New Project → Deploy from GitHub repo* → elegir el repositorio.
2. En el servicio: *Settings → Source → Root Directory* = `App/backend`. Railway lee `railway.json`
   (build con `Dockerfile`, healthcheck en `/health/live`).
3. *Variables* (entorno Production):

   | Variable | Valor |
   | --- | --- |
   | `ASPNETCORE_ENVIRONMENT` | `Production` (ya viene en la imagen) |
   | `ConnectionStrings__Marketplace` | cadena del paso 1 |
   | `Database__MigrateOnStartup` | `true` (aplica migraciones al arrancar) |
   | `FrontendOrigin` | `https://<app>.vercel.app` (sin barra final; completar en el paso 5) |
   | `Proxy__SharedSecret` | secreto del paso 2 (mínimo 32 caracteres) |
   | `Ranking__Enabled` | `true` |
   | `CatalogAi__Enabled` / `CatalogAi__Provider` / `CatalogAi__ApiKey` / `CatalogAi__Model` | opcional: `true`, `OpenAI` o `Gemini`, clave de ese proveedor y modelo. Sin clave, dejar `false` |

   `PORT` lo define Railway; la API escucha en ese puerto.
4. *Settings → Networking → Generate Domain*. Anotar la URL (`https://<api>.up.railway.app`).
5. Verificar: `https://<api>.up.railway.app/health/live` responde `{"status":"ok"}`.

Si falta una variable obligatoria, la API **no arranca** y el log de Railway dice cuál falta
(`Invalid Production configuration: …`). Las claves de Data Protection se guardan en la tabla
`data_protection_keys`, así que los tokens de actividad y del ranking siguen siendo válidos tras cada redeploy.

## 4. Vercel (web)

1. *Add New → Project* → importar el repositorio. *Root Directory* = `App/frontend` (framework Next.js, se detecta solo).
2. *Environment Variables* (Production):

   | Variable | Valor |
   | --- | --- |
   | `APP_ORIGIN` | `https://<app>.vercel.app` (sin barra final) |
   | `API_BASE_URL` | `https://<api>.up.railway.app` |
   | `API_PROXY_SECRET` | secreto del paso 2 |
   | `RANKING_ENABLED` | `true` |

3. *Deploy*. Anotar el dominio de producción (`https://<app>.vercel.app`).

## 5. Cerrar el círculo

Si el dominio de Vercel no se conocía antes, actualizar `FrontendOrigin` en Railway y `APP_ORIGIN` en Vercel con
ese valor exacto y redeployar ambos. Si no coinciden, todo lo que escribe (login, pedidos) responde 403.

## 6. Cargar los datos de demostración (desde tu PC)

El seed corre localmente contra Supabase, con doble confirmación del host (ver `App/seed/README.md`).
Primero esperar a que Railway haya aplicado las migraciones (paso 3). En PowerShell, desde la raíz del repo:

```powershell
$env:ConnectionStrings__Marketplace = "<cadena del paso 1>"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DEMO_SEED_CONFIRM_HOST = "aws-0-sa-east-1.pooler.supabase.com"   # host exacto de la cadena
$env:SEED_OWNER_PASSWORD = "<contraseña comerciante, 12-128 caracteres>"
$env:SEED_ADMIN_PASSWORD = "<contraseña admin, distinta>"
dotnet run --project App/backend/tools/Marketplace.Seed --configuration Release -- --apply --allow-hosted-demo
```

Cuentas creadas: `comerciante.seed@example.test` y `admin.seed@example.test`. Pasar las contraseñas a los
evaluadores por un canal privado. Repetir el seed renueva la vigencia de las promociones sin duplicar datos.

## 7. Prueba final (checklist)

- [ ] `/` carga y muestra comercios y promociones.
- [ ] `/explorar` con **zona + rubro** (sin texto) ordena por el ranking con equidad (política `equity-v1`).
- [ ] Una promoción abre su detalle (`/promociones/<id>`).
- [ ] `/estado` dice "API disponible".
- [ ] Login con `comerciante.seed@example.test` → `/mi-negocio` muestra los tres negocios.
- [ ] Hacer un pedido como visitante; el comerciante lo ve en su panel. La clave del pedido aparece oculta con "Mostrar enlace".
- [ ] Login con `admin.seed@example.test` → administración: moderación, rubros y auditoría.
- [ ] Redeploy de la API y volver a navegar `/explorar`: sigue funcionando sin cerrar sesión.

## Costos

- Supabase: plan gratuito (500 MB). Los proyectos gratuitos se pausan tras una semana sin actividad: entrar al panel y reanudar antes de la defensa.
- Vercel: plan Hobby gratuito.
- Railway: crédito de prueba inicial; después, plan Hobby (≈ USD 5/mes). Revisar el consumo antes de la defensa.

## Notas técnicas

- La cookie de sesión es `Secure` en producción: requiere HTTPS (Vercel lo da). En `http://localhost` funciona
  porque los navegadores y curl tratan `localhost` como contexto seguro.
- Correr la API localmente en modo producción: `docker build -t marketplace-api App/backend` y ejecutar con las
  mismas variables (validado con PostGIS local, seed, login y reinicio del contenedor).
- Las migraciones manuales siguen disponibles: `dotnet run --project App/backend/src/Marketplace.Api -- --migrate`.
