# Demo local actualizada — 05/10/2026

**Actualización posterior:** [seed](../App/seed/README.md) cargado y repetido sin
duplicados: 6 negocios totales, 3 cuentas, 12 productos, 3 planes y 12 saldos.
Los datos anteriores permanecen intactos. Credenciales de titular/admin en
`.tools/seed-demo/credentials.json`. Respaldo previo: `.tools/demo-backups/20261005-131041/`.
Las notas siguientes describen el estado anterior a esa carga.

La versión de código `055a6ee` está ejecutándose en **http://localhost:3000**,
con API en `http://localhost:5080` y PostgreSQL/PostGIS en 55432. Se aplicaron las
siete migraciones pendientes; el historial tiene doce entradas y termina en
`20261005121814_AddSponsoredCampaigns`. No es un despliegue público.

Los tres negocios, la cuenta y las fotos existentes se conservaron: sus resúmenes
de integridad coinciden antes/después, excluyendo las dos nuevas columnas del
historial de publicación. Los negocios anteriores mantienen historia desconocida.
No se asignaron roles ni se crearon planes, productos o ventas de demostración.
La búsqueda de productos muestra su estado vacío hasta cargar un catálogo elegible.

## Respaldo y ejecución

Respaldo local ignorado: `.tools/demo-backups/20261005-125749/marketplace.dump`.
Se restauró en una base temporal, se comparó su contenido y se eliminó esa base.
La carpeta contiene SHA-256 en `manifest.json`, resúmenes `before.json`/`after.json`
y registro de migración. El respaldo contiene datos privados; no subirlo a Git.

API publicada y frontend compilado en `.tools/demo-runtime/20261005-055a6ee/`.
La copia de `.next` permite seguir compilando el proyecto sin reemplazar los
archivos de la demo en ejecución. `node_modules` es una junction al frontend del
workspace; una actualización de dependencias requiere preparar otra versión.
Ambos servicios escuchan solo en loopback y se iniciaron en segundo plano oculto.
IA y previsualización experimental permanecen desactivadas.

PID y directorio de versión: `.tools/ui-demo-processes.json`. Registros:
`.tools/ui-api.log`, `.tools/ui-api-error.log`, `.tools/ui-web.log` y
`.tools/ui-web-error.log`. La conexión privada se pasa solo al proceso API.

Después de reiniciar Windows, con Docker iniciado, desde la raíz del workspace:

```powershell
docker compose --env-file App/.env.local -f App/compose.yaml up -d database
powershell -NoProfile -ExecutionPolicy Bypass -File .tools/start-demo.ps1
```

El iniciador local rechaza puertos ocupados; no detiene otro servicio. Usa la
versión preparada, sin recompilar ni migrar. No hay arranque automático de Windows.
No usar los helpers históricos `restart-ui-demo.ps1`/`refresh-ui-api.ps1` para
esta versión. Para recuperar datos, restaurar primero el respaldo en otra base y
verificarlo antes de cambiar la conexión; no borrar la base vigente.

## Verificación

`dotnet publish` Release y `npm run build` terminaron con código 0. Migración y
comparación de datos correctas. `/health/live` responde 200; `/estado` muestra
«API disponible». Playwright comprobó guía con los tres negocios, perfil público,
búsqueda de productos vacía, formulario de acceso, mensaje corregido ante ingreso
inválido y protección de administración. Vista 390 px sin desborde horizontal;
cero excepciones JavaScript y cero respuestas 5xx en ese recorrido.
Captura local: `.tools/demo-runtime/20261005-055a6ee/demo-mobile.png`.

La verificación privada completa sigue respaldada por el
[recorrido aislado](qa-playwright-2026-10-05.md); esta puesta en marcha no creó
credenciales ni cambió la contraseña existente para volver a probar ese flujo.
