# Punto de continuidad — 22/09/2026

Sesión continuada: historial de publicación y lector de candidatos completos implementados. El usuario
prioriza avanzar por incrementos completos sin confirmaciones rutinarias.

## Estado terminado

Último incremento: `IRankingCandidateRepository`, con consulta DAL/PostGIS única
de todos los publicados de dueño habilitado, rubro y radio; sin Skip/Take ni corte
top 20. Conserva distancia, ID de dueño interno y fecha/marca histórica, sin
calcular puntajes ni exponer endpoints. Valida entrada/integridad y cancelación.
**62/62 HTTP/PostGIS**: 12 casos nuevos y extensión de migración histórica; 45
elegibles completos, exclusiones y cambios posteriores verificados. Evidencia:
`../openspec/changes/archive/2026-09-22-complete-ranking-candidates/verify-report.md`.
Sigue adaptar desconocidos a N=0 y definir contexto de revista; luego snapshots.
Chequeo previo a versionar el avance completo: **81/81 BLL/evaluación** sobre la
compilación actual, además de las 62 HTTP/PostGIS. El helper de reconstrucción
conserva su evidencia previa 7/7; no se repitió ni modificó el borrador original.

Incremento anterior: `FirstPublishedAt` y `PublicationHistoryUnknown`, mantenidos
atómicamente por PostgreSQL y recuperados por EF al guardar. Altas publicadas y
primeras publicaciones nuevas registran fecha; editar/despublicar/republicar y
escrituras atrasadas la conservan. La migración marca todos los registros anteriores
como desconocidos, incluidos borradores: no se puede probar que nunca publicaron.
Verificación: **50/50 HTTP/PostGIS**, incluidas cuatro pruebas nuevas; modelo EF
sin cambios pendientes. Evidencia: `../openspec/changes/archive/2026-09-22-business-publication-history/verify-report.md`.
Migración comprobada solo en bases temporales; aplicar al iniciar la demo actualizada.
El lector completo aún no se conecta al ranking público. El adaptador futuro debe
tratar la historia desconocida como N=0. El borrador congelable anterior queda intacto.

Cerca, marketplace hiperlocal de Córdoba. Implementados y verificados: identidad y panel del responsable, guía pública con búsqueda/rubro, ubicación/radio/distancia, perfil y galería con hasta cinco fotos, y actividad privada por negocio.

El incremento anterior investigó patrones de Google Business Profile, Airbnb, Etsy, GA4 e IAB y los aplicó al panel **Mi negocio → Ver actividad**:

- Apariciones visibles (50 % de tarjeta, 1 segundo continuo, pestaña activa), visitas al perfil y clics de WhatsApp.
- Sesión aleatoria de pestaña de 30 minutos; deduplicación también atómica en PostgreSQL; visitas del propio dueño autenticado excluidas.
- Comprobantes protegidos de contexto: negocio, posición, origen y versión del orden. No guardar coordenadas del visitante ni búsquedas textuales en eventos.
- Períodos 7/30 días, hora de Córdoba, gráfica interactiva, tabla y estados vacíos/error/reintento.
- Retención de eventos de 30 días; purga al arrancar y cada hora. Sin proveedores analíticos externos.

Piloto inicial: motor BLL de ranking explicable y consola
`App/backend/tools/Marketplace.Evaluation`, sin IA ni conexión al orden público.
Cuatro variantes, 200 negocios y 5.000 consultas por cada semilla 17/29/43,
ventana de 30 días, candidatos/contexto, relevancia sintética independiente,
Gini/cobertura/nDCG/desviación por mérito. Exporta inputs, trazas y SHA-256.

Dos ejecuciones completas produjeron hashes idénticos. Contra relevancia más
proximidad, Gini medio baja de 0,4806 a 0,4106 y nDCG baja de 0,9333 a 0,8946.
La actividad sintética empeora ambas métricas globales. No se declara validada
la hipótesis ni aceptable retrospectivamente esa pérdida; no hay resultados de ventas.

Ablaciones completadas: `--suite ablation` separa compensación sola, con novedad,
con rotación y con ambas, frente a proximidad. Gini/nDCG globales: compensación
sola 0,4203/0,9013; con novedad 0,4131/0,8972; con rotación 0,4178/0,8990;
ambas 0,4106/0,8946. La compensación explica la mayor parte del cambio;
novedad y rotación agregan reducción de concentración y pérdida de relevancia.
Hay regresiones locales: revisar evidencia por superficie/zona, no solo medias.
Dos ejecuciones de 75.000 rankings dieron hashes idénticos. El piloto original
de cuatro variantes también se reprodujo con sus diez hashes intactos.

Validación nueva: **7/7 pruebas del helper de reconstrucción**, build Release sin
advertencias, seis archivos runtime y cuatro salidas idénticos por hash. Las
**81/81 pruebas BLL/evaluación** son evidencia previa de las mismas fuentes/binarios.
Las 46 HTTP/PostGIS y 56 frontend, lint/build y revisión visual
son evidencia previa; no se repitieron porque no cambiaron API, DAL ni frontend.
No hay nueva UI ni migraciones. No se verificó disponibilidad actual de la demo.

Protocolo de evaluación en
`Doc/evaluation/business-ranking-final/protocol.md`, plan JSON, rúbrica y cuatro
plantillas CSV vacías. Límites principales acordados: pérdida nDCG humana ≤0,02 global y
por superficie, ≤0,03 por superficie/zona; reducir Gini ≥10 % y desviación por
mérito ≥5 % global/por superficie, con límites locales/cobertura adicionales.
El 21/09 el usuario respondió «Usar estos criterios para preparar el experimento».
No volver a pedir ese acuerdo. Son tolerancias propias, no estándares ni aprobación
de resultados/despliegue. Los controles adicionales por semilla/distribución local
siguen como salvaguardas metodológicas a congelar con el diseño.
Semillas reservadas propuestas, sin generar resultados: 211/223/227/229/233;
calibración 101/103/107. Muestras humanas propuestas: 30/60 consultas.
Hay fichas sintéticas para entrenamiento/calibración; aún no hay juicios humanos,
material semántico de reserva ni runner de evaluación final.
Contrato propuesto de snapshot/ledger en `Doc/ranking-public-integration.md`:
primer corte revista por zona pública/rubro/radio 4 km; las demás consultas
conservan orden actual. FirstPublishedAt y la consulta completa ya están implementados;
faltan el adaptador productivo, snapshots y ledger persistente.
Verificación documental: JSON, semillas/muestras, límites, CSV y enlaces correctos.

Importador: `--validate-review RUTA_CSV --output SALIDA_VACIA` importa
queries/candidates/judgments/resolutions.csv. Valida referencias, split único,
grados/rúbrica, dos evaluadores E1/E2, duplicados y resoluciones. Reporta faltantes
por par/evaluador, desacuerdos y acuerdo/matriz antes de adjudicar, con hashes de
inputs. Solo exporta judgments-final.json con cobertura completa; salida CLI
0=completo, 2=incompleto, 1=inválido/error. Completo no equivale a ranking aceptado.
En esa ruta la cobertura se mide solo contra los candidatos declarados en CSV.
Para paquetes generados, usar la nueva recolección verificable. Las pruebas usan
fixtures, no personas.

Preparación previa: `--prepare-review training|calibration --output SALIDA_VACIA`
genera inputs completos, fichas de cuatro rubros y hojas E1/E2 en blanco, con
selección por hash y todos los elegibles. Entrenamiento: semilla 61, 12 consultas,
521 fichas por persona (1.042 juicios). Calibración: 101/103/107, 30 consultas,
1.143 fichas por persona (2.286 juicios). Evaluación se rechaza; no se abrió reserva.
`--collect-review PAQUETE --package-sha256 HASH_FIJADO --output SALIDA_VACIA`
reextrae y verifica fuentes/selección/fichas/rúbrica antes de recoger solo grados
y razones; resultados incluyen digest del paquete y SHA-256 de respuestas E1/E2.
Faltar filas deja pendientes; alterar fichas/IDs se rechaza. Hash externo confiable
necesario; no prueba identidad humana. No usar el importador directo para evadir
el vínculo con la extracción. Los ZIP de esta entrega tienen todos los grados vacíos.
Descargas y comandos: `Doc/evaluation/business-ranking-final/packages/README.md`.
Paquetes completos en `.tools/review-packages-2026-09-21/{training,calibration}/`,
con repeticiones idénticas `...-repeat`. Versionados: ZIP por evaluador, manifiestos
y hashes; inputs grandes se regeneran. Fichas son simuladas, no negocios reales.

Comparación individual: `--evaluate-review PAQUETE --package-sha256 HASH --config PESOS
--variant EquityOnly --output SALIDA_VACIA` conecta hojas verificadas de calibración
al replay de 5.000 consultas por semilla. Compara una candidata con Proximity,
historias separadas, nDCG solo en muestra humana y distribución global, superficie
y superficie/zona. Distingue ausencias/ideal cero/vacías; Gini por superficie une
exposiciones. Medias por semilla con peso igual y diferencias pareadas, sin ocultar
métricas ausentes. Exporta trazas, denominadores, reporte y hashes.
Solo acepta Eq/N/rotación de la grilla; congela mérito/elegibilidad. Entrenamiento
y reserva rechazados. Esta ruta no decide aceptación ni recorre la grilla.
La CLI sobre hojas reales dio código 2: 2.286 juicios pendientes, sin métricas ni
trazas; dos salidas idénticas en `.tools/human-ranking-replay-2026-09-21/`.
La ruta completa se probó con fixtures temporales eliminados, no evidencia humana.
Uso: `Doc/evaluation/business-ranking-final/human-ranking.md`.

Grilla de calibración: `--calibrate-review PAQUETE --package-sha256 HASH --output SALIDA`
recorre las 18 configuraciones con baseline una vez por semilla (285.000 rankings).
Comprueba criterios, nueve ámbitos, tres semillas, denominadores y >=4/5 consultas
útiles por estrato, con >=1 por semilla. Faltantes/ideal insuficiente => inconclusive.
Reporta mainStatus (acordado), guardrailStatus (propuesto) y status combinado.
Selección provisional exige las 18 celdas evaluables; entre aptas ordena por mérito
global, nDCG, Eq/N y sin rotación. Ninguna apta => no-eligible-policy; no se fuerza
ganadora. No congela diseño/política ni habilita reserva o despliegue.
Prueba completa pasó con fixtures temporales eliminados y hashes/trazas verificados.
CLI real y réplica: código 2, 2.286 pendientes, hashes idénticos, sin rankings ni
selección en `.tools/calibration-selection-2026-09-21/{pending,pending-repeat}/`.
Uso y estados: `Doc/evaluation/business-ranking-final/calibration.md`.

Borrador preparado: `--prepare-freeze PAQUETE --package-sha256 HASH --source-root RAÍZ
--output SALIDA` conserva fuentes/protocolo, originales, binarios y calibración en
un borrador. Reejecuta calibración sobre la copia: sin hojas completas solo pendientes.
`--verify-freeze CARPETA --manifest-sha256 HASH_EXTERNO --output SALIDA` valida el
digest externo y lista exacta de archivos/hashes; cambios/extras/faltantes rechazados.
Código 0 significa tarea técnica terminada, nunca aprobación metodológica.
Se generó `.tools/freeze-preparation-2026-09-21/draft/` y se verificaron 85 archivos.
Digest fijado: `ba27292a32cf36f44829f3bb577ca3d4fcf76ea89ae135c5951d46a2c0dc794a`.
Referencia externa en `Doc/evaluation/business-ranking-final/freeze-draft-reference.json`.
Estado draft, política null, 2.286 pendientes, policyFrozen/evaluationAuthorized false.
No alterar esa copia: futuros cambios requieren nuevo borrador. Los registros humanos
y fechas siguen vacíos. En ese manifiesto original la correspondencia del build
figura pendiente; la evidencia posterior se adjunta sin modificarlo. Uso en `freeze.md`.

Último incremento: `App/tools/verify-ranking-rebuild.mjs` verifica el pin, copia
fuentes y reconstruye con SDK 10.0.400 sin feeds. PathMap restituye rutas CodeView;
se usa trabajo temporal no ignorado por Git para conservar EmbeddedFiles/PDB.
Con revisión `58ecb08a7bc771a5c94b5b5c076cd07a61e1b565`, las cuatro DLL y dos JSON
runtime coinciden exactamente. Replay con el binario nuevo: código 2 y cuatro
archivos idénticos, 2.286 pendientes. No se ejecutaron rankings con juicios humanos.
Se verificaron 179 artefactos de reconstrucción y 85 del borrador intacto. Fuentes
copiadas sin cambios; no se editó el manifiesto ni se declaró política congelada.
Evidencia en `.tools/freeze-rebuild-2026-09-21/verified-retry/`; el intento `verified/`
conserva el bloqueo EPERM del sandbox. Reintento autorizado, sin relajar controles.
Referencia externa: `Doc/evaluation/business-ranking-final/rebuild-reference.json`.
Uso/condiciones en `rebuild.md`. Esto prueba correspondencia en este entorno,
no garantiza binarios idénticos al cambiar SDK/Git/ignores ni aprueba metodología.

## Próximo paso acordado

**Continuar desde los paquetes y los criterios acordados**, sin repetir ablaciones
ni pedir nuevamente el acuerdo. Replay, comprobador y selección provisional ya
están implementados. El manifiesto borrador y su verificador también están listos.
La reconstrucción y correspondencia de binarios/replay ya están comprobadas.
El tramo técnico de calibración está listo: no repetir verificaciones equivalentes
ni fabricar registros humanos o aprobación. No abrir reserva.
En paralelo al desarrollo, falta que dos
personas revisen entrenamiento y luego anoten calibración; no simularlas con IA.
Si entrenamiento cambia rúbrica/semántica, versionar/regenerar antes de anotar
calibración. No ajustar con reserva ni generar evaluación hasta cerrar su diseño.
La evaluación final requiere personas y resultados reservados. En integración,
la publicación histórica y el lector completo ya están implementados. Sigue adaptar
esos datos al motor (historia desconocida recibe N=0), fijar contexto de revista y
preparar snapshots/ledger persistente sin activar la función. El contrato de
snapshot ya está redactado pero no implementado. Los eventos actuales no permiten reconstruir candidatos no
mostrados. La guía conserva distancia/nombre/id (nombre/id sin ubicación).
Los 3.000 productos y límites por catálogo necesitan implementar catálogo.
Mantener IA fuera del ranking y no confundir este piloto con el experimento final.
Si el usuario pide continuar desarrollo mientras llegan anotaciones, avanzar con
el adaptador de candidatos e historia desconocida, sin activar el
ranking público. No convertir otra corrida sintética en sustituto de personas.

Después quedan catálogo, capacidades por plan, solicitudes/pedidos, ventas/stock y publicidad. Trabajar un incremento completo por vez, con investigación primaria cuando corresponda, pruebas y revisión visual mediante MCP Playwright. El usuario prioriza una UI/UX profesional y avanzar sin pedir confirmaciones rutinarias.

## Dónde retomar

- [Estado general](../openspec/project.md).
- [PRD](PRD.md), secciones de exposición, ranking y evaluación.
- [Resultados, reproducción y límites del piloto](evaluation/business-ranking-pilot/README.md).
- [Ablaciones, contrastes pareados y límites](evaluation/business-ranking-ablations/README.md).
- [Protocolo de ablaciones](../openspec/changes/archive/2026-09-21-business-ranking-ablations/design.md).
- [Verificación de ablaciones](../openspec/changes/archive/2026-09-21-business-ranking-ablations/verify-report.md).
- [Protocolo con criterios principales acordados](evaluation/business-ranking-final/protocol.md).
- [Rúbrica y plantillas humanas](evaluation/business-ranking-final/human-review.md).
- [Paquetes para evaluadores](evaluation/business-ranking-final/packages/README.md).
- [Comparación con juicios verificados](evaluation/business-ranking-final/human-ranking.md).
- [Grilla, criterios y selección provisional](evaluation/business-ranking-final/calibration.md).
- [Borrador verificable y pendientes](evaluation/business-ranking-final/freeze.md).
- [Referencia externa del borrador](evaluation/business-ranking-final/freeze-draft-reference.json).
- [Reconstrucción exacta y condiciones](evaluation/business-ranking-final/rebuild.md).
- [Referencia de reconstrucción](evaluation/business-ranking-final/rebuild-reference.json).
- [Verificación del rebuild](../openspec/changes/archive/2026-09-21-freeze-rebuild/verify-report.md).
- [Verificación del preparador](../openspec/changes/archive/2026-09-21-freeze-preparation/verify-report.md).
- [Verificación de calibración](../openspec/changes/archive/2026-09-21-calibration-selection/verify-report.md).
- [Verificación del replay humano](../openspec/changes/archive/2026-09-21-human-ranking-replay/verify-report.md).
- [Verificación del importador](../openspec/changes/archive/2026-09-21-human-ranking-review/verify-report.md).
- [Verificación de paquetes](../openspec/changes/archive/2026-09-21-review-packages/verify-report.md).
- [Contrato propuesto de integración pública](ranking-public-integration.md).
- [Protocolo fijado antes de la corrida](ranking-research-2026-09-19.md).
- [Verificación del piloto](../openspec/changes/archive/2026-09-19-business-ranking-pilot/verify-report.md).
- [Investigación de actividad](analytics-research-2026-09-19.md).
- [Contrato de actividad](../openspec/specs/business-activity/spec.md).
- [Verificación y límites](../openspec/changes/archive/2026-09-19-business-activity/verify-report.md).
- [Arquitectura](architecture/ADR-001-layered-architecture.md).
- [Ejecución local](../App/README.md).

Rama `main`; el usuario pidió versionar y publicar el estado completo el 22/09.
La referencia anterior era `58ecb08`; consultar `git log -1` para el commit actual.
Se incluyen fuentes, pruebas, documentación y paquetes compactos de evaluación.
Los artefactos grandes y borradores de `.tools/` se conservan localmente según
`.gitignore`; sus referencias/hashes quedan en Doc. No hay despliegue público.
Artefactos grandes ignorados: `.tools/ranking-pilot-2026-09-19/` y su réplica
`...-repeat/`. Resultados compactos en `Doc/evaluation/business-ranking-pilot/`.
Reproducción: `dotnet run --project App/backend/tools/Marketplace.Evaluation
--configuration Release -- --output .tools/ranking-reproduction` (directorio vacío).
Para ablaciones: agregar `--suite ablation` y usar otra salida vacía.
Artefactos nuevos: `.tools/ranking-ablation-2026-09-21/`, `...-repeat/` y
`.tools/ranking-pilot-compatibility-2026-09-21/`. Resumen, reporte, configuración
y hashes en `Doc/evaluation/business-ranking-ablations/`.

## Entorno y precauciones prácticas

Windows PowerShell; workspace `C:\Users\AgustinPc\Documents\Proyectos\Tesis`. Next.js 16.3.4, Node 24.20.0, .NET SDK 10.0.400, PostgreSQL/PostGIS local. Antes de tocar Next, leer `App/frontend/AGENTS.md` y la guía pertinente incluida en `node_modules/next/dist/docs`.

Demo: frontend `http://localhost:3000`, API `http://localhost:5080`, PostgreSQL puerto 55432. Docker/PostGIS se inició el 22/09 para las pruebas. La nueva migración de historial se aplicó solo en bases temporales; actualizar/migrar la demo antes de usar el nuevo backend. Comprobar disponibilidad al volver: los procesos pueden no sobrevivir a la sesión. No guardar credenciales en documentación.

Helpers locales ignorados: `.tools/profile-check.ps1` prueba backend y detiene temporalmente la API; `.tools/refresh-ui-api.ps1` compila, migra y reinicia API; `.tools/ui-demo-processes.json` guarda PID. Revisar contenido antes de ejecutar. `.tools/version-current.ps1` contiene los commits del incremento ya ejecutados: NO volver a correrlo sin actualizar sus rutas.

Captura nueva: `.playwright-mcp/ranking-pilot-guide-smoke.png`. Conservadas las
de actividad de la sesión previa. Al capturar, desplazar a inicio y quitar foco.
Usar bases temporales PostgreSQL, nunca EF InMemory. No iniciar subagentes salvo
pedido explícito. No repetir suites completas sin cambios o dudas nuevas.
