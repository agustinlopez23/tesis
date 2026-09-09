# Review ledger: scaffold-backend-frontend

## Ronda fresca — 2026-09-05

| Hallazgo | Resolución de diseño | Estado de evidencia |
| --- | --- | --- |
| Las puertas de versiones solo detallaban .NET. | `design.md` fija SDK/TFM, paquetes backend/test, generator, Node/npm y todos los paquetes frontend/test directos; cada uno tiene versión exacta, procedencia y comando de preflight. | La metadata de Microsoft, NuGet y npm fue consultada. Restore, lockfile, `npm ci`, build, lint y tests no se ejecutaron. |
| `rollForward: latestPatch` contradice la reproducción exacta. | Se eligió pin estricto: `global.json` usará `10.0.400`, `rollForward: "disable"` y `allowPrerelease: false`. | Decisión cerrada; el SDK no está instalado y el archivo aún no existe porque `App/` no se implementa. |
| Se podía inferir mutabilidad de `NEXT_PUBLIC_API_URL` en runtime desde `force-dynamic`. | Se separan renderizado dinámico y binding de entorno. La regla es configurar antes de build y el apply debe ejecutar el experimento A/B build/start; no se afirmará mutabilidad runtime sin su resultado. | La guía oficial de Next sobre `NEXT_PUBLIC_*` fue consultada; el experimento no se ejecutó. |
| Faltaba una prueba que detecte una URL hardcodeada en la página. | F2 exige mockear `checkApiHealth`, fijar una URL de entorno centinela y comprobar que `page.tsx` se la pasa exactamente al helper, además de ambos textos visibles. | Diseño/test plan cerrado; la prueba no existe ni se ejecutó. |
| Podía confundirse conectividad de servidor con CORS de navegador. | Se declara explícitamente `Next server -> API`; el navegador recibe HTML y no llama a `/health/live`. No se agrega CORS. Un futuro Client Component requerirá allowlist y pruebas CORS separadas. | Decisión de alcance; no hay aplicación que ejecutar. |
| PostgreSQL/PostGIS podía convertirse en requisito implícito de salud. | Solo existe liveness HTTP; readiness, paquetes y servicios de datos se difieren. | Decisión de alcance; la verificación sin datos queda pendiente del apply. |

## Precondición restante para apply

Instalar **.NET SDK `10.0.400`** para la arquitectura Windows del entorno. El SDK observado es `9.0.311`; no se realizó instalación, restore, build ni test en esta revisión.

## Cierre de apply — 2026-09-07

La precondición de SDK quedó resuelta con instalación local verificada. Se completaron restore, build, pruebas y experimento A/B. El contrato ahora distingue proyectos productivos de tests, requiere rebuild para cambiar URL pública en producción y exige HTTP 200 exacto. Evidencia y limitaciones en tdd-evidence.md. Volumen actual: backend 279 líneas, frontend 330 líneas sin lockfile; verificador integrado y README se revisan con sus respectivos cortes. Lockfile generado: 8071 líneas, conservado íntegro y validado con npm ci. No se crearon PRs.
