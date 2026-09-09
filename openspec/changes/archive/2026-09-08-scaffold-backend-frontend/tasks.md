# Tareas: scaffold backend y frontend

Estado al cerrar el 2026-09-07: scaffold implementado y verificado; evidencia en `tdd-evidence.md`.
Estado inicial: Engram registraba el diseño cerrado y `App/` vacío. Verificación local de esta
sesión: SDK .NET `9.0.311`, Node `24.20.0` y npm `11.19.0`.
El SDK `10.0.400` se instaló durante apply en `.tools/dotnet`, con SHA-512 verificado.

## 0. Puertas previas

- [x] Revalidar disponibilidad de las versiones exactas de `design.md` en Microsoft,
  NuGet y npm antes de instalar o generar. Registrar fuentes y fecha. Si una versión
  no existe o resulta incompatible, corregir el diseño antes de continuar.
- [x] Resolver dos precisiones de contrato antes de apply: la prohibición de
  referencias a API corresponde a proyectos productivos (el proyecto de tests
  necesita referenciarla); el cambio de URL de producción requiere reconstruir
  según la regla de configuración fijada en diseño.
- [x] Confirmar el criterio HTTP del helper: la spec exige el contrato `200 OK`,
  mientras el diseño describe `response.ok` (cualquier 2xx). Alinear ambos y
  agregar el caso `201` con cuerpo válido a la prueba correspondiente.
- [x] Preparar el SDK fijado tras validar su disponibilidad. Verificar con
  `dotnet --list-sdks`; comprobar `dotnet --version` desde backend una vez creado
  `global.json`. No sustituir silenciosamente la LTS por el SDK 9 instalado.
- [x] Revisar propuesta, specs, diseño y estas tareas antes de implementar, según
  el flujo acordado. Las casillas se completan únicamente con evidencia real.

## 1. Corte: estructura backend

Requisito: solución backend en capas y compilable; configuración local sin secretos.

- [x] Crear `global.json` estricto, solución `.sln`, seis proyectos productivos y
  `Marketplace.Api.Tests`, con las versiones del diseño. Usar formato de solución
  explícito si el SDK genera `.slnx` por defecto. Eliminar código de demostración.
- [x] Agregar solamente la referencia de tests a API. Inspeccionar referencias con
  `dotnet list <proyecto> reference` para los siete proyectos; contrastar con ADR-001.
- [x] Documentar prerrequisitos y perfil HTTP local `http://localhost:5080` en
  `App/README.md`, junto con configuración de ejemplo e ignores pertinentes.
- [x] Ejecutar desde backend `dotnet restore Marketplace.sln` y
  `dotnet build Marketplace.sln --no-restore --configuration Release`.
  Registrar resultados; estas verificaciones estructurales no son un ciclo TDD.

## 2. Corte: liveness backend — ciclo B1

Requisitos: liveness HTTP y verificación automatizada sin servicios de datos.

- [x] Rojo: escribir prueba con `WebApplicationFactory<Program>` para
  `GET /health/live`: estado 200, media type JSON y objeto exacto `{"status":"ok"}`.
  Ejecutar `dotnet test Marketplace.sln --configuration Release` y registrar fallo
  de contrato con host operativo; un error de restore o compilación no lo sustituye.
- [x] Verde: implementar el endpoint mínimo y ejecutar el mismo comando sin
  connection string, PostgreSQL ni PostGIS.
- [x] Refactorizar si aporta claridad y repetir pruebas. Documentar ejecución local
  y alcance exclusivo de liveness en la misma unidad que el comportamiento.
- [x] Registrar comando, fecha UTC, rojo, cambio mínimo y verde en
  `tdd-evidence.md`; no anticipar resultados.

## 3. Corte: toolchain frontend

Requisito: frontend TypeScript verificable y configuración sin secretos.

- [x] Generar Next según el diseño, sin inicializar Git. Fijar paquetes directos,
  Node/npm, scripts, TypeScript estricto y configuración Vitest/Testing Library.
- [x] Crear `.env.example` con `NEXT_PUBLIC_API_URL=http://localhost:5080` e ignorar
  `.env.local`. Documentar configuración antes del build y comandos locales.
- [x] Generar lockfile y ejecutar `npm ci`, `npm ls`, `npm run lint` y
  `npm run build` desde frontend. Registrar incompatibilidades antes de seguir.
  No declarar pruebas aprobadas mientras no existan pruebas de comportamiento.

## 4. Corte: conectividad frontend — ciclos F1 y F2

Requisitos: estados visibles, URL configurable y fallos controlados.

- [x] F1 rojo: probar el helper con fetch inyectable: URL configurada, normalización
  de barras finales, ruta exacta, `cache: "no-store"`, éxito y rechazo de URL ausente
  o inválida, red, HTTP fuera del contrato, media type incorrecto, JSON inválido y
  forma distinta. Usar `npm test -- tests/api-health.test.ts`.
- [x] Probar timeout a 3000 ms con temporizadores controlados, señal abortada y
  limpieza del temporizador tanto en éxito como en error. Registrar rojo antes de
  implementar cada comportamiento; no usar esperas reales de tres segundos.
- [x] F1 verde/refactor: implementar el helper mínimo y repetir la prueba enfocada.
- [x] F2 rojo: mockear el helper, fijar URL centinela en el entorno y comprobar que
  la página la pasa exactamente; verificar `role="status"` y ambos textos
  normativos. Restaurar entorno y mocks entre casos. Ejecutar
  `npm test -- tests/page.test.tsx` y registrar fallo observable.
- [x] F2 verde/refactor: implementar Server Component dinámico con título y estado,
  ejecutar prueba enfocada y luego `npm test`, lint y build. Registrar evidencia
  TDD y actualizar instrucciones en este mismo corte.

## 5. Verificación integrada y cierre

- [x] Backend: ejecutar restore, build Release y
  `dotnet test Marketplace.sln --no-build --configuration Release` tras ese build.
- [x] Frontend: ejecutar `npm ci`, `npm ls`, `npm test`, `npm run lint` y
  `npm run build` con configuración de ejemplo.
- [x] Iniciar API y frontend; comprobar endpoint y página con API disponible.
  Detener API y recargar: debe aparecer `API no disponible`. Registrar comandos
  exactos y resultados sin servicios de datos.
- [x] Ejecutar experimento de diseño: build con URL de API centinela A, start con
  B y solicitud de página. Registrar solicitudes recibidas por ambas, texto y
  versiones. Si contradice el diseño, actualizarlo antes de cerrar. Detener los
  procesos auxiliares al terminar.
- [x] Revisar alcance, ausencia de secretos, referencias y documentación. Registrar
  líneas por corte con presupuesto de 400; evaluar lockfile completo como generado,
  sin fragmentarlo ni ocultarlo. No crear Git, PRs ni publicación en este slice.
- [x] Actualizar estado y memoria con verificaciones reales y pendientes restantes.

Orden previsto: 0 → 1 → 2 → 3 → 4 → 5. El toolchain frontend se preparó mientras se descargaba el SDK; los ciclos TDD se preservaron. Ver evidencia y limitaciones en `tdd-evidence.md`.
