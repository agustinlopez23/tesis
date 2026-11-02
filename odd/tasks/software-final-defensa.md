# Software final para la defensa: alinear con los entregables y publicar online

## Objetivo
Terminar el prototipo para la defensa del TFG: que haga lo que los entregables dicen,
y que los profesores del CAE puedan probarlo online sin instalar nada.

## Problema y por qué
La auditoría del 01/11/2026 encontró que la Entrega 4 promete más de lo que hace el software:
- la búsqueda pública no usa el ranking hiperlocal con equidad (solo la vista previa `/revista`, bloqueada en producción);
- la moderación del administrador (rubros, suspensiones) no existe (HU-19);
- no hay bloqueo tras intentos fallidos de inicio de sesión;
- el seed no publica promociones, la clave privada del pedido se ve en texto plano;
- no existe despliegue, aunque el documento nombra Vercel, Railway y Supabase.

## Alcance
- Código en `App/` (API .NET, web Next.js, seed).
- Configuración de despliegue: API en Railway (Dockerfile), web en Vercel, base PostGIS en Supabase.
- Ajuste del texto de la Entrega 4 donde el documento describe decisiones distintas
  de las del software (carpeta `Doc/Seminario_Entrega_4`, gitignoreada: nunca commitear).

## Restricciones
- Artefactos técnicos en inglés; textos de interfaz en español (como el proyecto existente).
- Crear cuentas, cargar credenciales y publicar es decisión del usuario: se deja todo listo y documentado.
- Push, PR y merge quedan a cargo del usuario.
- Estrategia de entrega: rama `feat/software-final-defensa` con un commit por tarea; el usuario decide si la parte en PR encadenados.

## Tareas
- [x] T1 Ranking hiperlocal en producción: flag de producción en API y web, `/explorar` con zona y rubro ordenado por el ranking, política versionada. Ruta: delegada (escritor, 2+ archivos no triviales).
- [x] T2 Moderación del administrador (HU-19): rubros, suspender y reactivar cuentas y negocios, auditoría; bloqueo tras 5 intentos fallidos. Ruta: delegada.
- [x] T3 Promociones en el seed y seeder habilitado para la base hosteada con confirmación explícita. Ruta: delegada.
- [x] T4 Clave del pedido oculta por defecto con opción de mostrar y copiar. Ruta: delegada (se agrupa con T3).
- [x] T5 Preparación de despliegue: Dockerfile de la API, migraciones al iniciar, claves de Data Protection persistidas, encabezados reenviados, guía de despliegue y variables. Ruta: delegada.
- [ ] T6 Alinear el texto de la Entrega 4 con el software, recapturar pantallas y regenerar el PDF. Ruta: delegada.
- [ ] T7 Pruebas locales completas: suites, scripts de punta a punta y recorrido en navegador de cada historia de usuario contra la demo local. Ruta: delegada (verificación).
- [ ] T8 (diferida por decisión del usuario, 02/11/2026) Publicar online; por ahora solo queda la guía `Doc/despliegue.md`.

## Verificación
- Backend: `dotnet test App/backend/Marketplace.sln` con `TEST_DATABASE_CONNECTION` sobre PostGIS.
- Frontend: `npm test`, `npm run lint`, `npm run build` en `App/frontend`.

## Evidencia
- T1 (02/11/2026): API `Ranking:Enabled` + web `RANKING_ENABLED`, política `equity-v1` (EquityOnly, seed 17) en campo `policy`; `/explorar` ordena por ranking con zona soportada + rubro y sin texto, con caída al listado. Api.Tests 153/153, BLL.Tests 99/99, frontend 136/136 (relanzado por el padre), lint y build OK. RED observado antes de GREEN en API, flag, proxy y explorar.

- T1 revisión: riesgo medio, revisión debida (presupuesto de tramo); consentimiento pendiente del usuario (linaje review-4146b47aa3786af4).
- T2 (02/11/2026): `/admin/categories`, `/admin/moderation/*`, `/admin/audit`, tabla `admin_actions`, bloqueo 15 min tras 5 fallos (401 genérico), páginas Rubros/Moderación/Auditoría. Migración `20261102021436_AddAdminModeration` aplicada a la base demo. Api.Tests 158/158, BLL 99/99, frontend 144/144, lint y build OK; padre relanzó AdminModeration 5/5.

- T2 revisión: riesgo medio, revisión debida; consentimiento pendiente del usuario (linaje review-a69c0e996281f993).
- T3/T4 (02/11/2026): dos promociones sembradas con ventana relativa (ahora−1 d a +29 d) renovable de forma idempotente; seed hosteado solo con `--allow-hosted-demo` + `DEMO_SEED_CONFIRM_HOST`; cuentas evaluadoras con `SEED_OWNER_PASSWORD`/`SEED_ADMIN_PASSWORD`. Clave del pedido enmascarada con "Mostrar enlace". Api.Tests 168/168, BLL 99/99, frontend 147/147, lint OK; padre relanzó build (exit 0). Seed local aplicado: 2 promociones visibles.

- T3/T4 revisión: riesgo medio, `under_budget` (269 líneas), queda pendiente en el tramo.
- T5 (02/11/2026): Dockerfile + railway.json (salud `/health/live`), `Database:MigrateOnStartup`, validación de arranque en Production, claves de Data Protection en tabla `data_protection_keys` (repositorio propio: el paquete oficial falla la auditoría NuGet), IP del visitante vía proxy con `API_PROXY_SECRET`/`Proxy:SharedSecret`, guía `Doc/despliegue.md`. Api.Tests 177/177, BLL 99/99, frontend 150/150, lint/build OK, `docker build` OK, humo local en Production (migraciones, seed, ranking `equity-v1`, login, reinicio con claves persistidas). Padre relanzó DeploymentReadiness 9/9. Plantillas `.env.production.example` no creadas (permisos); las variables están en las tablas de la guía.

## Siguiente paso
T7 (pruebas locales completas), luego T6.
