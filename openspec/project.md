# Contexto SDD de la tesis

## Estado vigente — 2026-10-05

Catálogo/planes, solicitudes, ventas/inventario, métricas comerciales, búsqueda
global y campañas manuales implementados. Asistencia opcional preparada y
desactivada hasta configurar modelo/clave; borrador revisable sin auto publicación.
**148 API, 99 BLL/evaluación y 123 frontend**, lint/build/integración/navegador
correctos. Revisión Playwright posterior corrigió paneles obsoletos, selección con
precio viejo y mensajes de acceso/sesión; API/BLL conservan la validación anterior.
Evidencia en `../Doc/qa-playwright-2026-10-05.md`.
Contratos en specs; estado en `../Doc/continuidad.md`. Sigue prueba real
de IA, evaluación humana/final y ranking experimental de productos. 2286 juicios
pendientes comprobados; no hay activación experimental productiva.

Demo local actualizada en 3000/API 5080, con respaldo restaurado y doce migraciones
aplicadas. Se conservaron tres negocios, una cuenta y fotos. No se cargaron planes
ni productos ni se habilitó IA/ranking. Operación: `../Doc/demo-local-2026-10-05.md`.

## Estado anterior — 2026-10-04

Completado `ranking-preview-ui`: `/revista` local con proxy cerrado en producción,
selección explícita, navegación por cursor actual/siguiente, historial revalidado,
reintento estable, vencimiento y recuperación. Tarjetas y atribución compartidas
con guía. Compatibilidad de actividad requiere
`20261005020557_AllowRankingActivityVersions`, aplicada solo en bases temporales.
**83/83 frontend y 101/101 API/HTTP/PostGIS**, build/lint y Playwright a 1440/390/320px.
Harness Next/API/PostGIS: 45 negocios, 20/20/5, replay sin nuevos aportes, medición
deduplicada y 404 productivo aun con flags activos. Guía y demo conservan su estado.
Evidencia: `changes/archive/2026-10-04-ranking-preview-ui/verify-report.md`.
Siguiente incremento funcional: catálogo por negocio. Activación productiva del
ranking requiere anotaciones/política evaluada, dimensionamiento y claves compartidas.

Capacidad completada previamente:

Capacidad del ranking local: máximos de 1.000 candidatos
y 10.000 saldos por contexto, exceso 503 con rollback, sin truncar; reintento seguro.
Cupos independientes de 4 POST/16 GET simultáneos, sin cola, recuperación tras
éxito/error/cancelación y validación al arrancar. Carga integrada con 1.000 negocios,
17 listas y 50 páginas; integridad conservada. **100/100 API/HTTP/PostGIS y 99/99
BLL/evaluación**, cero omitidas. Evidencia: `changes/archive/2026-10-04-ranking-capacity/verify-report.md`.
Se conservan cursores, TTL, cuotas por minuto y retirada de emergencia del HTTP local.
Solo Development/Testing con habilitación explícita; producción cerrada.
Proxy/UI completado posteriormente; activación productiva sujeta a anotaciones, política evaluada y
dimensionamiento del despliegue real. Demo sin cambios.

Estado previo dentro del mismo día:

Entrega 2 realizada por el usuario. Desarrollo retomado con adaptador interno
`MagazineRankingCandidates`: tres zonas públicas/rubro/4 km, contexto versionado,
R=1, sin truncar candidatos e historia desconocida explícita con N=0 en el motor.
Snapshots/ledger internos completados: transacción, deduplicación concurrente,
secuencia, traza reproducible, previsualización propia en cero y purga automática.
Páginas estables revalidan elegibilidad sin rellenar huecos ni sumar exposición.
Verificación: **99/99 BLL/evaluación y 72/72 HTTP/PostGIS**. Evidencia:
`changes/archive/2026-10-04-ranking-snapshots/verify-report.md`.
HTTP local completado en el incremento posterior; siguen controles de carga y UI; ranking público
deshabilitado hasta evaluación/política. Catálogo/pedidos siguen pendientes.

## Estado anterior — 2026-09-22

Implementados: scaffold, onboarding de responsables y guía pública de negocios,
búsqueda textual/rubro, descubrimiento por ubicación y rediseño visual Cerca (nombre de trabajo).
El panel permite registrar una cuenta, iniciar/cerrar sesión y crear/editar
negocios con ubicación PostGIS. `/explorar` permite visitar sin cuenta las
tarjetas de negocios publicados y contactar por WhatsApp.

La guía pagina de a 20, ordena por distancia/nombre/id cuando hay ubicación
y por nombre/id sin ella. Excluye borradores y responsables
suspendidos. El DTO público no contiene datos de cuenta. Se corrigió además la
lectura del campo `whatsApp` al editar/publicar desde el panel y se incorporó
PostGIS a la configuración CI, que antes no preparaba base para la suite.

Verificación nueva: **62/62 HTTP/PostGIS**, incluidas 12 pruebas nuevas de candidatos
y extensión de migración histórica. Lector interno BLL/DAL obtiene todos los
elegibles con distancia/fecha/marca histórica en una consulta, sin corte top 20.
Evidencia: `changes/archive/2026-09-22-complete-ranking-candidates/verify-report.md`.
La verificación anterior de historial fue 50/50; modelo EF sin cambios pendientes.
Primera publicación registrada atómicamente en PostgreSQL, conservada al editar
o republicar. Todos los registros anteriores migran con historia desconocida,
también borradores; no se inventan fechas. Aplicado solo en bases temporales.
Evidencia: `changes/archive/2026-09-22-business-publication-history/verify-report.md`.
Verificación anterior: helper de reconstrucción 7/7, build Release y correspondencia
exacta de seis archivos runtime/cuatro salidas. BLL/evaluación 81/81 es evidencia
previa de las mismas fuentes/binarios, sin repetir la suite.
Las 46 HTTP/PostGIS y 56 frontend, lint y build son evidencia de sesiones previas;
sin cambios API, DAL ni frontend durante las ablaciones.
Recorrido HTTP integrado aprobado sobre base temporal: publicación, consulta
anónima y despublicación, además del onboarding y la revocación de sesión.
Contrato, tareas y evidencia: `changes/archive/2026-09-18-public-business-directory/`.
Versiones vigentes: .NET SDK 10.0.400, Next.js 16.3.4, Node 24.20.0, npm 11.19.0.

La portada y `/explorar` presentan la guía; `/mi-negocio` contiene el panel.
El diagnóstico técnico se conserva en `/estado`. Búsqueda y filtros sobreviven
a recarga e historial; la búsqueda ignora tildes en vocales.
La búsqueda por ubicación ofrece zona manual, geolocalización opcional, radio,
distancias y recuperación de permisos denegados. La consulta espacial usa PostGIS
con índice GiST y respeta elegibilidad y filtros antes de paginar.
Evidencia: `changes/archive/2026-09-18-location-discovery/verify-report.md`.
Los perfiles en `/negocios/{id}` ya incluyen galería y enlace desde la guía.
El responsable administra cinco fotos con carga/reemplazo/eliminación; el backend
valida, orienta, reduce y elimina metadatos. Borradores y suspendidos mantienen
sus imágenes privadas. Utility aporta el procesamiento, sin referencias a capas.
Evidencia: `changes/archive/2026-09-18-public-business-profile/verify-report.md`.
Actividad implementada: apariciones visibles, visitas y clics, contexto protegido,
deduplicación, exclusión del dueño autenticado, retención de 30 días y panel privado
con períodos 7/30 días y evolución. Investigación: `Doc/analytics-research-2026-09-19.md`.
Evidencia: `changes/archive/2026-09-19-business-activity/verify-report.md`.
Laboratorio offline de ranking de negocios implementado: motor BLL explicable,
ventana/contexto, cuatro variantes, juicios sintéticos independientes y exportación
reproducible. Dos corridas completas con 200 negocios y 5.000 consultas por cada
semilla 17/29/43 produjeron hashes idénticos. Reduce concentración con pérdida
de nDCG; no se considera validada la hipótesis. La guía conserva su orden actual.
Evidencia: `Doc/evaluation/business-ranking-pilot/README.md` y
`changes/archive/2026-09-19-business-ranking-pilot/verify-report.md`.
Ablaciones completadas: cinco variantes separan compensación, novedad y rotación;
dos ejecuciones de 75.000 rankings con hashes idénticos y piloto original compatible.
La compensación sola explica la mayor parte del cambio global; revisar pérdidas
de nDCG y regresiones locales. Evidencia: `Doc/evaluation/business-ranking-ablations/`.
Protocolo en `Doc/evaluation/business-ranking-final/protocol.md`: usuario acordó
criterios principales el 21/09. Importador CSV `--validate-review` implementado:
valida cobertura declarada y resoluciones, exporta hashes y juicios solo al completar.
Paquetes de entrenamiento/calibración generados con fichas sintéticas, grados
vacíos, manifiesto y recolección que verifica fuentes/selección/candidatos. ZIP por
evaluador en `Doc/evaluation/business-ranking-final/packages/`. Reserva sin generar.
Contrato de candidatos/snapshot
propuesto en `Doc/ranking-public-integration.md`. No hay juicios humanos, runner
final ni integración implementada. Comparación de calibración implementada:
hojas verificadas, historial completo, agregados por superficie y diferencias pareadas.
Comprobador de criterios/suficiencia y barrido de 18 celdas implementados; selección
provisional distingue límites acordados de salvaguardas propuestas. CLI con hojas
reales: 2.286 pendientes, sin métricas ni selección. Borrador de congelamiento
preparado y verificado: 85 archivos, copias de fuentes/protocolo/originales/binarios,
política null y requisitos humanos/metodológicos pendientes. No congela ni aprueba.
Reconstrucción verificada desde la copia con SDK/Git/PathMap registrados; seis
archivos runtime y cuatro salidas de pendientes idénticos. Borrador intacto,
evidencia separada en `Doc/evaluation/business-ranking-final/rebuild.md`.
Entrenamiento/anotación humana pendiente; siguiente desarrollo independiente:
adaptador de candidatos al motor con historia desconocida N=0 y contexto de revista;
la publicación histórica y recuperación completa están implementadas. Luego snapshots
y ledger persistente, sin activar ranking público ni omitir la evaluación pendiente.
Siguen pendientes catálogo, capacidades
por plan, pedidos, stock, publicidad y evaluación experimental del ranking.
No hay despliegue público; documentación de ejecución en `App/README.md`.

## Contexto inicial (histórico)

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

## Estado actualizado — 2026-09-08

El primer caso de negocio `business-owner-onboarding` está implementado y
verificado. Incluye registro e ingreso del responsable, sesión opaca revocable,
categorías semilla y alta/listado/consulta/edición de negocios propios con
`geography(Point,4326)`. El frontend incorpora proxy de mismo origen y panel en
español. Verificación: backend 10/10, frontend 25/25, lint y build aprobados, más
recorrido integrado sobre base temporal. No hay despliegue público configurado.

## Incremento 05/10/2026 — catálogo, planes y solicitudes

Catálogo por negocio, períodos manuales auditados y solicitudes completados.
Perfil público, edición versionada, foto procesada y preservación al vencer.
Solicitudes con precios históricos, reintentos sin duplicados, resumen para
WhatsApp, acceso privado por 30 días y gestión auditada del titular.
API/PostGIS 120/120 y frontend 100/100; build/lint e integración real correctos.
Rama local `feat/product-orders`; demo sin migrar. Ver
`specs/business-catalog/spec.md`, `specs/product-orders/spec.md` y
`../Doc/continuidad.md`. Ventas/stock completados en el incremento siguiente:
ajustes auditados, líneas finales, transacciones sin stock negativo, replay y
anulación con devolución explícita. API 127/127 y frontend 105/105. Rama
`feat/sales-inventory`; ver `specs/sales-inventory/spec.md`. Sigue el alcance
comercial/experimental restante, con ranking productivo cerrado.
