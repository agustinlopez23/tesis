# Diseño técnico: baseline ejecutable de backend y frontend

**Estado:** implementado y verificado el 2026-09-07. SDK local en `.tools/dotnet`. Este documento conserva la planificación de versiones; los resultados de ejecución que sustituyen sus pendientes históricos están en `tdd-evidence.md`.

## Decisiones cerradas

| Tema | Decisión |
| --- | --- |
| Backend | .NET `10.0` LTS, TFM `net10.0` y SDK **exacto** `10.0.400`. |
| Reproducibilidad .NET | `App/backend/global.json`: `version: "10.0.400"`, `rollForward: "disable"`, `allowPrerelease: false`. No se acepta otro feature band ni parche. |
| Frontend | `create-next-app@16.3.4`, Next `16.3.4`, React/React DOM `19.2.3`, TypeScript estricto, App Router, `src/`, ESLint, sin Tailwind y sin Git. |
| Runtime frontend | Node `24.20.0` y npm `11.19.0` exactos; `.nvmrc`, `engines` y `packageManager: "npm@11.19.0"` los deben expresar. |
| Consulta | El Server Component entrega la URL configurada al helper `checkApiHealth`; el helper consulta `{base}/health/live` sin caché y con timeout de 3 s. |
| Configuración pública | `NEXT_PUBLIC_API_URL` se establece **antes de `next build`**. `force-dynamic` no implica que una variable pública sea mutable al iniciar `next start`. |
| Datos/CORS | PostgreSQL/PostGIS readiness se difiere. La señal mide `Next server -> API`; no hay `fetch` de navegador a la API ni CORS en este slice. |

El SDK observado `9.0.311` no satisface esta decisión. **Requisito de instalación para el apply: instalar .NET SDK `10.0.400` para la arquitectura Windows del entorno; instalación local al proyecto autorizada al iniciar apply.**

## Proveniencia y puerta de versiones

La consulta read-only del índice oficial de releases de Microsoft, el 2026-09-05, informó que el canal `10.0` es `lts`, está en fase `active`, tiene EOL `2028-11-14` y publica `latest-sdk: 10.0.400` con release `10.0.11` (2026-08-11). El canal `9.0` sigue `sts`; por la decisión explícita de usar LTS, se selecciona `net10.0`/`10.0.400`, no el SDK local 9.x.

### Backend y tests .NET

| Elemento | Versión exacta decidida | Proveniencia consultada | Puerta de compatibilidad pendiente de ejecución |
| --- | --- | --- | --- |
| SDK / TFM | `10.0.400` / `net10.0` | Índice y `10.0/releases.json` oficiales de Microsoft | `dotnet --list-sdks` debe mostrar `10.0.400`; luego restore/build/test. |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.11` | Registro NuGet de la versión publicada y release .NET `10.0.11` | Restore de `Marketplace.Api.Tests` contra `net10.0`, sin warnings/errores. |
| `Microsoft.NET.Test.Sdk` | `18.9.0` | Registro NuGet de la versión publicada | Restore y ejecución de `dotnet test`. |
| `xunit` | `2.9.3` | Registro NuGet de la versión publicada | Restore y ejecución de `dotnet test`. |
| `xunit.runner.visualstudio` | `3.1.5` | Registro NuGet de la versión publicada | Descubrimiento y ejecución de `dotnet test`. |

No se agregará `coverlet.collector`: la cobertura no es un requisito de este slice. Todos los `PackageReference` llevan estas versiones literales, sin rangos ni versiones flotantes. La disponibilidad en los registros está verificada; su restore, compilación y ejecución conjunta **no** se ha ejecutado ni se afirma verificada.

### Frontend y tests

| Área | Paquetes directos exactos | Evidencia de metadata npm | Puerta pendiente de ejecución |
| --- | --- | --- | --- |
| Generador y aplicación | `create-next-app@16.3.4`, `next@16.3.4`, `react@19.2.3`, `react-dom@19.2.3` | `next@16.3.4` declara Node `>=20.9.0` y peer React/React DOM `^19.0.0`; React DOM `19.2.3` pide React `^19.2.3`. | Generar tras aprobación, fijar manifest y crear lockfile; `npm ci`, lint y build. |
| Tipado y lint | `typescript@5.9.3`, `@types/node@24.10.13`, `@types/react@19.2.7`, `@types/react-dom@19.2.3`, `eslint@9.39.1`, `eslint-config-next@16.3.4` | TypeScript declara Node `>=14.17`; ESLint acepta Node `>=21.1.0`; `eslint-config-next` pide ESLint `>=9` y TypeScript `>=3.3.1`. | `npm ci`, `npm ls`, `npm run lint`, `npm run build`. |
| Runner y DOM | `vitest@5.0.0`, `vite@8.2.2`, `@vitejs/plugin-react@6.1.1`, `jsdom@30.0.1` | Vitest acepta Node 22.12+, 24.x o 26+; Vite y el plugin aceptan Node 20.19+ o 22.12+; el plugin pide Vite 8.x; jsdom acepta Node 22.22+, 24.15+ o 26+. | `npm ci`, `npm ls`, ejecución de tests React/DOM. |
| Testing Library | `@testing-library/dom@10.4.1`, `@testing-library/react@16.3.3`, `@testing-library/jest-dom@7.0.1` | React Testing Library pide React/React DOM/tipos `^19` y DOM `^10`; Jest DOM pide DOM `>=10 <11` y Vitest `>=0.32`; sus engines aceptan Node 24.20.0. | `npm ci`, `npm ls` sin peers inválidos y `npm test`. |

Node `24.20.0` y npm `11.19.0` son observaciones locales proporcionadas para esta sesión; no se volvió a ejecutar ningún runner. La metadata consultada demuestra que Node `24.20.0` satisface los engines declarados de los paquetes anteriores. No demuestra que el árbol resuelto, el lockfile, `npm ci`, test, lint o build funcionen: esas son puertas explícitas del apply.

El `package.json` final fija todos los paquetes de la tabla sin `^` ni `~`; `package-lock.json` fija las transitivas. Los scripts son:

```json
{
  "dev": "next dev",
  "build": "next build",
  "start": "next start",
  "lint": "eslint .",
  "test": "vitest run",
  "test:watch": "vitest"
}
```

## Estructura prevista

```text
App/
├── README.md
├── backend/
│   ├── global.json
│   ├── Marketplace.sln
│   ├── src/
│   │   ├── Marketplace.Api/
│   │   ├── Marketplace.BLL/
│   │   ├── Marketplace.DAL/
│   │   ├── Marketplace.IoC/
│   │   ├── Marketplace.DTOs/
│   │   └── Marketplace.Models/
│   └── tests/Marketplace.Api.Tests/
└── frontend/
    ├── .env.example
    ├── .nvmrc
    ├── package.json
    ├── package-lock.json
    ├── tsconfig.json
    ├── vitest.config.ts
    ├── tests/{setup.ts,api-health.test.ts,page.test.tsx}
    └── src/{app,components,lib}/
```

Son exactamente seis proyectos productivos y un proyecto de tests. Se eliminarán los archivos de demostración de los templates; no se crean carpetas vacías, `Marketplace.Utility`, integraciones, proyectos BLL/DAL de tests, `compose.yaml`, paquetes de datos, migraciones ni capas adicionales sin responsabilidad concreta.

## Backend y límites de referencias

Desde `App/backend`, después del preflight, se usarán `dotnet new web` para API, `dotnet new classlib` para los otros cinco proyectos y `dotnet new xunit` para `Marketplace.Api.Tests`, todos con `-f net10.0`. `Marketplace.sln` incluirá los siete proyectos. Nullable e implicit usings permanecen habilitados.

ADR-001 es una allowlist, no una obligación de materializar flechas. La liveness pertenece al límite HTTP y no consume capas de negocio, por lo que no habrá referencias entre los seis proyectos productivos.

| Origen | Permitidas por ADR-001 | Materializadas ahora |
| --- | --- | --- |
| `Marketplace.Api` | BLL, DTOs, IoC | ninguna |
| `Marketplace.BLL` | Models, DTOs | ninguna |
| `Marketplace.DAL` | BLL, Models | ninguna |
| `Marketplace.IoC` | BLL, DAL | ninguna |
| `Marketplace.DTOs`, `Marketplace.Models` | ninguna | ninguna |
| `Marketplace.Api.Tests` | proyecto bajo prueba | `Marketplace.Api` |

`Program.cs` usará Minimal API: `MapGet("/health/live", () => Results.Json(new { status = "ok" }))` y conservará `public partial class Program`. No habrá `AddHealthChecks`, redirect HTTPS obligatorio en el perfil HTTP local, EF Core, Npgsql, PostGIS, Swagger ni DI externa.

La integración con `WebApplicationFactory<Program>` debe comprobar `200`, `application/json` y el objeto exacto `{"status":"ok"}`, sin connection string ni PostgreSQL/PostGIS. Así prueba liveness HTTP real sin confundirla con readiness.

## Frontend, URL y límite CORS

El flujo es deliberadamente servidor a servidor:

```text
Browser -> GET página Next.js
        -> Server Component toma NEXT_PUBLIC_API_URL
        -> checkApiHealth(baseUrl) -> GET {base}/health/live
        -> HTML con role="status" -> Browser
```

`src/lib/check-api-health.ts` recibe URL base, `fetch` inyectable y timeout. Elimina barras finales, exige URL absoluta HTTP(S), anexa exactamente `/health/live`, aborta a los 3000 ms y siempre limpia el temporizador. Devuelve `true` solo con `response.status === 200`, media type JSON y objeto exacto `{"status":"ok"}`; URL ausente o inválida, timeout, red, estado distinto de 200, media type incorrecto, JSON inválido o forma distinta devuelven `false`.

`src/app/page.tsx` es un Server Component dinámico y debe contener el cableado explícito equivalente a:

```ts
const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL;
const apiAvailable = await checkApiHealth(apiBaseUrl);
```

Luego renderiza exclusivamente un título y `<p role="status">API disponible</p>` o `<p role="status">API no disponible</p>`. `force-dynamic` y `cache: "no-store"` controlan la renderización/fetch; **no son evidencia de mutabilidad de variables de entorno**.

La documentación oficial de Next consultada indica que las referencias `process.env.NEXT_PUBLIC_*` pueden reemplazarse durante `next build` e incorporarse al JavaScript entregado al navegador. Por ello la regla operativa de este slice es establecer `NEXT_PUBLIC_API_URL` antes de `npm run build`; no se promete que cambiarla al hacer `npm start` altere un artefacto ya construido.

La verificación de apply debe demostrar esta semántica, no inferirla:

1. Preparar dos APIs centinela, A y B, que registren solicitudes y respondan el contrato de liveness.
2. Construir con `NEXT_PUBLIC_API_URL=<A>` y arrancar ese artefacto con `NEXT_PUBLIC_API_URL=<B>`.
3. Solicitar la página y registrar qué centinela recibió `/health/live` y el texto resultante.
4. El resultado observado, junto con las versiones y modo de despliegue, será la única base para documentar comportamiento de inicio. Si no apunta a A como espera la regla de build, detenerse y actualizar este diseño; no atribuir el resultado a `force-dynamic`.

La prueba de página debe aislar el Server Component, establecer una URL de entorno centinela y mockear `checkApiHealth`. Debe esperar que el mock reciba exactamente esa URL y que los dos valores booleanos produzcan los textos normativos. Una implementación que llame al helper con una URL hardcodeada falla esta prueba. Las pruebas del helper cubren URL, éxito, timeout/abort, red, estado distinto de 200, media type, JSON y formas inválidas.

Aunque el nombre de variable sea público, no puede contener secretos. `.env.example` solo contiene `NEXT_PUBLIC_API_URL=http://localhost:5080`; `.env.local` no se versiona. No hay llamada directa del navegador a la API: por tanto no se configura middleware CORS. Si un cambio futuro mueve el fetch a un Client Component, deberá introducir una allowlist CORS configurable, pruebas permitida/rechazada/preflight y credenciales deshabilitadas salvo decisión explícita.

## Preflight y verificación reproducible

Estos comandos son el contrato de apply y están **sin ejecutar** en este diseño:

```bash
# App/backend
# Requiere que 10.0.400 ya esté instalado; no se permite roll-forward.
dotnet --version
dotnet --list-sdks
dotnet restore Marketplace.sln
dotnet build Marketplace.sln --no-restore --configuration Release
dotnet test Marketplace.sln --no-build --configuration Release
dotnet list src/Marketplace.Api reference
dotnet list src/Marketplace.BLL reference
dotnet list src/Marketplace.DAL reference
dotnet list src/Marketplace.IoC reference
dotnet list src/Marketplace.DTOs reference
dotnet list src/Marketplace.Models reference
dotnet list tests/Marketplace.Api.Tests reference
```

```bash
# App/frontend, con Node 24.20.0, npm 11.19.0 y .env.local seguro
node --version
npm --version
npm ci
npm ls
npm test
npm run lint
npm run build
npm run dev
```

Además del experimento build/start anterior, se iniciará la API en `http://localhost:5080`, se verificará `/health/live`, se cargará la página con API disponible y, al detener la API y recargar, se verificará `API no disponible`. Todos esos caminos deben completar sin PostgreSQL/PostGIS.

## Strict TDD, alcance y cortes

Durante apply, `tdd-evidence.md` registrará fecha UTC, requisito, comando, rojo observado, cambio mínimo y verde observado. Los ciclos son: B1 contrato HTTP, F1 helper health y F2 página/cableado de URL. El test de F2 incluye la aserción de URL al helper indicada arriba. Los preflight de versiones no sustituyen una prueba roja de comportamiento.

Con `auto-forecast` y presupuesto de 400 líneas, los cortes encadenados serán: (1) estructura backend, (2) liveness backend, (3) toolchain frontend y (4) conectividad frontend. El lockfile se revisa como artefacto generado por manifest, metadata npm y `npm ci`; no se fragmenta ni se oculta para eludir el presupuesto.

El slice no incorpora capacidades de marketplace, hosting, Git, publicación ni readiness de PostgreSQL/PostGIS. Un cambio futuro con persistencia real definirá `/health/ready` y pruebas aisladas contra PostgreSQL/PostGIS reales; EF Core InMemory no será sustituto.

## Riesgos abiertos

| Riesgo | Mitigación / estado |
| --- | --- |
| SDK LTS no instalado | `10.0.400` es requisito explícito; no se instaló. |
| Compatibilidad real de paquetes | Metadata y peers/engines consultados; restore, lockfile, build y tests siguen sin ejecutar. |
| Semántica build/start de `NEXT_PUBLIC_API_URL` | Regla conservadora de build-time y experimento A/B obligatorio; ningún reclamo de mutabilidad runtime. |
| Confusión CORS | Se documenta y prueba la dirección Next server -> API; no se habilita CORS sin fetch de navegador. |
| Falso readiness | Liveness no toca dependencias; PostgreSQL/PostGIS se difiere. |

## Resultado del apply — 2026-09-07

Restore, build Release, contrato HTTP, 20 pruebas frontend, lint, build Next y conectividad real completados. El experimento A/B confirmó la URL fijada durante build. Véase [tdd-evidence.md](tdd-evidence.md) para comandos, fallos TDD y límites. La configuración HTTP 5080 está en launchSettings.json; la referencia a API se permite únicamente desde tests. El helper exige estado 200 exacto.
