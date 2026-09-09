# Apply progress: scaffold-backend-frontend

**Estado:** implementación completada el 2026-09-07. Este archivo reconcilia la evidencia existente de `tdd-evidence.md` con el contrato de strict TDD requerido por `sdd-verify`.

## Implementation Summary

- Backend scaffold creado con solución `Marketplace.sln`, seis proyectos productivos y proyecto `Marketplace.Api.Tests`.
- Endpoint `GET /health/live` implementado con contrato JSON exacto `{ "status": "ok" }`.
- Frontend Next.js implementado con helper `checkApiHealth`, estados visibles de disponibilidad/indisponibilidad y configuración mediante `NEXT_PUBLIC_API_URL`.
- No se agregaron funcionalidades de negocio, persistencia, CORS, `Marketplace.Utility` ni integraciones.

## TDD Cycle Evidence

| Cycle | Requirement / behavior | RED evidence | GREEN evidence | TRIANGULATE evidence | SAFETY NET | Test files |
| --- | --- | --- | --- | --- | --- | --- |
| B1 | Backend liveness returns HTTP 200, JSON media type and exact `{ "status": "ok" }` body without database dependencies. | `dotnet test Marketplace.sln --configuration Release` failed with an operative host returning `NotFound` for `/health/live`. | Added the minimal `MapGet("/health/live", ...)` endpoint; `dotnet test Marketplace.sln --no-restore --configuration Release -m:1 -nodeReuse:false` passed 1/1. | The test asserts status code, JSON media type and exact payload, not only that the server starts. | Final backend restore/build/test passed; no connection string, PostgreSQL or PostGIS required. | `App/backend/tests/Marketplace.Api.Tests/LivenessTests.cs` |
| F1 | `checkApiHealth` calls `{base}/health/live` without cache, validates exact success contract, handles invalid configuration, HTTP failures, network failures and malformed responses. | Initial Vitest run failed for success HTTP, JSON with charset and aborted signal; later 16 helper tests failed because URL/contract validation and error capture were missing. | Implemented URL normalization, absolute HTTP(S) validation, `cache: "no-store"`, status `200`, JSON media type/body validation, error capture and timer cleanup; `npm test` passed helper coverage. | Cases include missing/invalid URL, trailing slash normalization, exact path, HTTP `201` rejection, extra body properties, wrong media type, invalid JSON, network error and timeout. | `npm test`, `npm run lint` and `npm run build` passed after implementation. | `App/frontend/tests/api-health.test.ts` |
| F2 | The page passes the configured API URL to the helper and renders accessible success/unavailable states. | Page tests failed because the helper received 0 calls instead of the sentinel URL. | Server Component passed `process.env.NEXT_PUBLIC_API_URL` to the helper and rendered status text from the boolean result; page tests passed. | Tests cover both available and unavailable states, `role="status"`, normative visible texts and environment/mocks reset between cases. | Full frontend `npm test`, lint, build and HTTP smoke script passed after implementation. | `App/frontend/tests/page.test.tsx` |

## Verification Evidence

| Area | Command / evidence | Result |
| --- | --- | --- |
| .NET SDK | `.tools/dotnet/dotnet.exe --version` from backend context | `10.0.400` |
| Backend restore/build | `dotnet restore Marketplace.sln`; `dotnet build Marketplace.sln --no-restore --configuration Release -m:1 -nodeReuse:false` | Passed, 0 errors and 0 warnings. |
| Backend tests | `dotnet test Marketplace.sln --no-build --configuration Release -m:1 -nodeReuse:false` | Passed, 1/1. |
| Project references | `dotnet list <project> reference` for seven projects | Six productive projects had no references; tests referenced only API. |
| Frontend install/tree | `npm ci --no-audit --no-fund`; `npm ls` | Passed; direct tree valid. |
| Frontend tests | `npm test` | Passed, 20/20. |
| Frontend lint/build | `npm run lint`; `NEXT_PUBLIC_API_URL=http://localhost:5080 npm run build` | Passed. |
| Integrated smoke | `node App/tools/verify-connectivity.mjs` | Passed: API available, unavailable state after stopping API, and build-time URL binding A/B confirmed. |

## Notes and limitations

- The detailed original execution notes remain in `tdd-evidence.md`.
- Infrastructure failures (`spawn EPERM` in the sandbox and a stuck MSBuild attempt without `-m:1`) were excluded from RED evidence because they did not represent product behavior.
- The lockfile is generated and intentionally remains whole.
- No Git, PR or publication step was performed in this slice.
