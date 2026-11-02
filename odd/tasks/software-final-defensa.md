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
- Push autorizado por el usuario (02/11/2026) al terminar, a la rama `feat/software-final-defensa` (main está protegida). El usuario pidió (02/11/2026) que main quede actualizada con esta rama por PR, solo después de que pasen todos los tests locales, compile todo y el CI del PR esté en verde; luego limpiar ramas remotas ya integradas.
- Estrategia de entrega: rama `feat/software-final-defensa` con un commit por tarea; el usuario decide si la parte en PR encadenados.

## Tareas
- [x] T1 Ranking hiperlocal en producción: flag de producción en API y web, `/explorar` con zona y rubro ordenado por el ranking, política versionada. Ruta: delegada (escritor, 2+ archivos no triviales).
- [x] T2 Moderación del administrador (HU-19): rubros, suspender y reactivar cuentas y negocios, auditoría; bloqueo tras 5 intentos fallidos. Ruta: delegada.
- [x] T3 Promociones en el seed y seeder habilitado para la base hosteada con confirmación explícita. Ruta: delegada.
- [x] T4 Clave del pedido oculta por defecto con opción de mostrar y copiar. Ruta: delegada (se agrupa con T3).
- [x] T5 Preparación de despliegue: Dockerfile de la API, migraciones al iniciar, claves de Data Protection persistidas, encabezados reenviados, guía de despliegue y variables. Ruta: delegada.
- [ ] T6 Alinear el texto de la Entrega 4 con el software, recapturar pantallas y regenerar el PDF. Ruta: delegada.
- [x] T7 Pruebas locales completas: suites, scripts de punta a punta y recorrido en navegador de cada historia de usuario contra la demo local. Ruta: delegada (verificación).
- [x] T9 Cerrar brechas de criterios halladas en T7 (A): HU-01 mensaje de cuenta suspendida, HU-06 saldo disponible y disponibilidad baja, HU-08 tolerancia a errores de tipeo, orden por ranking y ampliar radio. Ruta: delegada.
- [x] T10 Cerrar brechas de criterios halladas en T7 (B): HU-09 promociones en el perfil y mensaje con contexto, HU-15 capacidades de planes, HU-17 motivo de rechazo visible, HU-20/21 versión visible y registrada, HU-22 actividad del responsable con tope; script `verify-connectivity`, test inestable de despliegue y dos fallas de accesibilidad. Ruta: delegada.
- [x] T11 Reescribir README de la raíz como entrada para evaluadores (qué es, HU vs software, arranque local, cuentas, arquitectura, pruebas, despliegue) y reordenar `App/README.md` como guía técnica; mover la bitácora a `Doc/bitacora-desarrollo.md`; corregir enlaces rotos. Español neutro. Pedido del usuario 02/11/2026. Ruta: delegada, después de T10.
- [x] T12 HU-05 desde fotografía: el asistente de IA debe recibir la foto del producto (además de los datos mínimos) y proponer nombre, descripción y categoría; hoy solo envía texto. El resumen y el cuerpo de la Entrega 4 lo presentan como propuesta central. Ruta: delegada.
- [x] T13 Segunda tanda completa de pruebas locales sobre HEAD 2d373bf. Ruta: delegada (verificación).
- [x] T14 Ajustes finales: borrar rubro sin uso (409 con cantidad si está en uso), 404 del catálogo en perfiles Presencia, BOM de `setup-local.ps1`, `-m:1` en el README. Ruta: delegada.
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

- T3–T5 revisión: riesgo medio, revisión debida; consentimiento pendiente (linaje review-ce8829f3a3a657d7).
- T7 (02/11/2026): 151 chequeos de navegador, escritorio y móvil. Aprobadas HU-02,03,04,05,07,10,11,12,13,14,16,18,23; con observaciones HU-17 y HU-19 (sin borrado de rubro: se deshabilita). Parciales HU-01,06,08,09,15,20/21,22. Suites: Api 176/177 (DROP DATABASE con timeout bajo carga, 9/9 al relanzar), BLL 99/99, frontend 150/150, lint/build OK, axe 0 violaciones en el arnés; `verify-connectivity.mjs` roto por la validación de arranque de T5. Datos de prueba locales: cuentas t7a/t7b, rubro "Rubro prueba T7" deshabilitado, base sobrante `migrate_startup_*`.

- T9 (02/11/2026): HU-01 403 `account_suspended` solo tras contraseña correcta; HU-06 409 `insufficient_stock` con `availableQuantity` y "Disponibilidad baja" (`Catalog:LowStockThreshold`=3); HU-08 orden por posición del negocio con `equity-v1` (sin persistir exposición), tolerancia a tipeo con pg_trgm + fuzzystrmatch, ampliar radio. Migración `20261102034202_AddProductFuzzySearch` aplicada a la demo. Api 181/181, BLL 99/99, frontend 155/155, lint/build OK; padre relanzó AcceptanceCriteria.

- T9 revisión: riesgo medio, `under_budget`, pendiente en el tramo.
- T10 (02/11/2026): promociones vigentes en el perfil, WhatsApp con texto de contexto, comparación Presencia/Impulso sin precios, motivo de rechazo visible, "Versión del ranking: equity-v1" y columna `policy_label` en `ranking_snapshots`/`ranking_opportunities`, tabla `owner_activity` con deduplicación (60 min) y tope (10) que no altera `equity-v1` (peso de actividad 0); limpieza robusta de bases de prueba; accesibilidad de `/pedido` y tablas. Migración `20261102035544_AddOwnerActivityAndRankingPolicyLabel` aplicada a la demo. Api 185/185, BLL 106/106, frontend 163/163, lint/build OK, axe 0. El padre detuvo la demo de T7, recompiló con `NEXT_PUBLIC_API_URL=http://localhost:5080` (precondición documentada) y `verify-connectivity.mjs` dio exit 0.

- T10 revisión: tramo T9–T10 `under_budget` según la evaluación (4363 líneas; resultado registrado tal como lo devuelve la herramienta).
- T11 (02/11/2026): README raíz 32→151 líneas (qué es, roles, tabla HU-01..23 con rutas, arranque local, cuentas, arquitectura, pruebas, despliegue); `App/README.md` 610→328 como guía técnica; historial movido a `Doc/bitacora-desarrollo.md`. 59 enlaces verificados, 34 variables verificadas, sin voseo ni secretos. Hallazgo: HU-05 usa texto, no fotografía → T12. Las contraseñas del seed las genera `seed-demo.ps1` y las guarda en `.tools/seed-demo/credentials.json`.

## Siguiente paso
T12, luego repetir pruebas locales completas y T6.
