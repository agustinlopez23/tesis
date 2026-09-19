# Tareas

- [x] Investigación primaria y contrato de métricas, visibilidad, contexto y retención antes de implementación.
- [x] Dos pruebas API rojas: comprobantes ausentes; implementar modelo, ingesta atómica, aislamiento y reportes. PostgreSQL aislado.
- [x] Cinco pruebas UI rojas con contratos mínimos y una prueba proxy roja; implementar observadores, deduplicación, panel y allowlist.
- [x] Prueba roja de registro de tarea de retención; agregar ejecución al inicio/cada hora.
- [x] Ampliar cobertura de comprobantes caducados, posición/versiones, suspensión, cambio de día en Córdoba, atribución, desmontaje e intervalos separados. Estas ampliaciones se añadieron después de la implementación base.
- [x] Verificar recorridos en Playwright con visitante separado y responsable: exposición/recarga/perfil/contacto, 7/30 días, tabla y error/reintento.
- [x] Verificar anchos 320/390/768/1440; corregir etiquetas nativas de select y escala de gráfica.
- [x] Ejecutar suite backend, frontend, lint y build; documentar y archivar.

Comandos: `dotnet test App/backend/Marketplace.sln --configuration Release -m:1` con `TEST_DATABASE_CONNECTION` a PostgreSQL/PostGIS local; en `App/frontend`, `npm test`, `npm run lint`, `npm run build`. Helpers locales ignorados resuelven SDK, credenciales y reinicio sin volcarlas en documentos.

Versionar en unidades de contratos, persistencia, API, observación, panel y documentación. La migración generada y su snapshot exceden individualmente el presupuesto orientativo; el código escrito a mano se separa para revisión.
