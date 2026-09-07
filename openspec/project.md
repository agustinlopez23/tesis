# Contexto SDD de la tesis

El proyecto continuará mediante OpenSpec con **strict TDD**. El primer cambio a especificar será un scaffold compilable de backend y frontend con conectividad mínima, sin implementar todavía funcionalidades del marketplace.

## Camino inmediato

1. Crear la propuesta del primer slice técnico.
2. Precisar requisitos observables de arranque, health/connectivity y compilación.
3. Diseñar versiones, referencias, configuración local y estrategia de pruebas.
4. Descomponer la implementación en tareas test-first de hasta 400 líneas cambiadas por PR.

## Contexto confirmado

| Tema | Decisión o evidencia |
| --- | --- |
| Dominio | Marketplace hiperlocal para comercios de Córdoba. |
| Backend | Monolito ASP.NET Core con `Marketplace.Api`, `Marketplace.BLL`, `Marketplace.DAL`, `Marketplace.IoC`, `Marketplace.DTOs` y `Marketplace.Models`. |
| Proyectos opcionales | `Marketplace.Utility` e integraciones se crean solo cuando tengan una responsabilidad concreta. |
| Frontend | Next.js con TypeScript; revalidar la última versión estable y compatibilidad con Node antes de implementar. |
| Datos | PostgreSQL/PostGIS. |
| Arquitectura | ADR aceptado en `Doc/architecture/ADR-001-layered-architecture.md`; BLL no depende de DAL y las referencias deben ser acíclicas. |
| IA | Solo asistencia opcional para borradores, con confirmación y fallback manual. No participa en ranking. |
| Ranking futuro | Debe contemplar exposición equitativa sin intervención de IA. |
| Idioma | Documentación en español; identificadores técnicos en inglés. |
| Repositorio | `App/` está vacío y no se modifica durante init. No hay repositorio Git inicializado en este workspace. |

## Primer slice

**Incluye:** estructura mínima real de backend y frontend, compilación, arranque, configuración local sin secretos, una señal mínima verificable de conectividad y pruebas del comportamiento acordado.

**No incluye:** catálogo, stock, autenticación, ranking, geobúsqueda funcional, asistencia de IA, pagos, logística, despliegue contratado ni otras reglas de negocio.

La forma exacta de la conectividad mínima —por ejemplo, health endpoint y verificación desde frontend— debe quedar definida en spec y design, no asumirse durante init.

## Calidad y runners

- Aplicar ciclo estricto: prueba fallida, implementación mínima, refactorización.
- Backend previsto: xUnit ejecutado con `dotnet test`.
- Frontend previsto: Vitest + Testing Library ejecutados con `npm test`; validar build con `npm run build`.
- Fijar versiones y scripts definitivos en el diseño, luego de revalidar compatibilidad.
- Para DAL futuro, probar contra PostgreSQL/PostGIS real y aislado; no usar EF Core InMemory como sustituto.
- Mantener los cambios dentro de 400 líneas por PR o usar PRs encadenados.

Toolchain observado el 2026-09-05: .NET SDK 9.0.311, Node.js 24.20.0 y npm 11.19.0. Estas observaciones no fijan por sí solas las versiones objetivo.

## Fuentes

- `Doc/architecture/ADR-001-layered-architecture.md`
- `Doc/Seminario1 entrega/LopezRamalloMarceloAgustin - Prototipado Tecnológico.docx`
- `Doc/Seminario1 entrega/LopezRamalloMarceloAgustin - Prototipado Tecnológico Entrega1.pdf`

## Próximo paso

Crear una propuesta OpenSpec para el primer slice, manteniendo `App/` intacto hasta que propuesta, specs, diseño y tareas estén aprobados.

## Estado actualizado — 2026-09-07

El primer scaffold está implementado en App/ y verificado. El estado inicial y el camino inmediato anteriores son históricos. Backend: siete proyectos compilados y una prueba HTTP aprobada. Frontend: 20 pruebas, lint y build aprobados. Verificación HTTP integrada y experimento de URL de build completados sin base de datos. SDK local 10.0.400 en .tools/dotnet. Instrucciones: App/README.md. Evidencia: openspec/changes/scaffold-backend-frontend/tdd-evidence.md.

Próximo paso: definir y especificar el primer caso de uso de negocio antes de implementarlo. No se inicializó Git ni se eligió hosting.
