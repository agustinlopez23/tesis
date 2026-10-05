# Punto de continuidad — 05/10/2026

**Estado actual: [cierre local y pendientes reales](cierre-desarrollo-2026-10-05.md).**
Completados catálogo/planes, solicitudes, ventas/stock, métricas, descubrimiento
global y campañas. Asistencia opcional preparada en `feat/catalog-draft-assistance`
desde `7f412cd`, con corrección posterior `7a3567f`: borrador editable, consentimiento,
confirmación, fallback, adaptador OpenAI desactivado hasta configurar modelo/clave.
**148 API + 99 BLL/evaluación + 117 frontend**, lint/build/integración/navegador
correctos. No migraciones nuevas para IA. Sin credenciales reales ni llamadas pagas.
Evidencia: `../openspec/changes/archive/2026-10-05-catalog-draft-assistance/verify-report.md`.

Recolección humana verificada de nuevo: 2286 juicios pendientes, 1143 pares y cero
comparados. No inventar anotaciones ni declarar validada la hipótesis. Falta
configuración/prueba real de IA, selección/congelamiento/reserva/evaluación final y
ranking experimental de productos. Producción experimental cerrada. Demo intacta;
servidores temporales detenidos y bases eliminadas. Las secciones siguientes son
históricas y sus indicaciones de siguiente paso quedan reemplazadas por este estado.

## Campañas completadas previamente

Campañas completadas en `feat/sponsored-campaigns`, base `1fe510b`. Preparación,
revisión/pago manual, cupos/rotación por zona, anuncios separados, promociones
gratuitas con imagen revisada, métricas y atribución de solicitudes. Pausa y retiro
por elegibilidad; historial conservado. **138/138 API y 114/114 frontend**, lint,
build, integración y navegador correctos. Migración solo en bases temporales;
demo intacta. Evidencia: `../openspec/changes/archive/2026-10-05-sponsored-campaigns/verify-report.md`.
Sigue asistencia opcional de catálogo; evaluación humana/final continúa pendiente.

## Búsqueda completada previamente

Búsqueda global de productos completada en `feat/product-discovery`, base `ef20283`.
`/productos`: texto literal/sin acentos, rubro, zona o ubicación/radio, páginas de
20, distancia PostGIS y enlace al catálogo. Elegibilidad viva por plan/publicación/
titular/stock. Orden explícito alfabético o por distancia; no ranking experimental.
**131/131 API y 109/109 frontend**, lint/build/integración/navegador correctos.
Tras interrupción de PostgreSQL, runner acotado a dos clases; suite repetida verde
y base temporal residual eliminada. Demo intacta. Evidencia:
`../openspec/changes/archive/2026-10-05-product-discovery/verify-report.md`.
Sigue publicidad con gestión manual y asistencia opcional; falta evidencia humana.

## Métricas completadas previamente

Métricas comerciales completadas en `feat/commerce-metrics`, desde `23bd605`:
cohorte de solicitudes, contactos y conversión hasta ahora; ventas por fecha de
registro, anuladas separadas e importes por origen/moneda. 7/30 días de Córdoba,
lectura consistente y privada, sin migración. **129/129 API y 107/107 frontend**,
lint/build/integración y navegador correctos. Evidencia:
`../openspec/changes/archive/2026-10-05-commerce-metrics/verify-report.md`.
Continuar con descubrimiento global de productos, publicidad y asistencia opcional.

## Ventas e inventario completados previamente

Completadas ventas e inventario en `feat/sales-inventory`, desde `d6ae72b`.
Saldo inicial explícito, ajustes auditados, ventas desde solicitudes con líneas
finales y origen, venta presencial separada, descuento atómico e idempotente,
anulación con devolución explícita. Catálogo público deshabilita productos con
saldo conocido cero; solicitudes no reservan. Una venta por solicitud.

Validación: **127/127 API/HTTP/PostGIS y 105/105 frontend**, lint/build correctos.
Integración real y Playwright 1440/390/320, pérdida de respuesta después de guardar
sin duplicación, anulación sin reposición y rechazo recuperable por stock.
Migración `20261005114423_AddSalesInventory`, Down poblado bloqueado. Demo intacta;
bases temporales eliminadas. Reproducción: `node App/tools/verify-catalog.mjs --sales`.
Evidencia: `../openspec/changes/archive/2026-10-05-sales-inventory/verify-report.md`.

El usuario pidió continuar con todos los pendientes sin confirmaciones rutinarias.
Seguir por métricas comerciales y descubrimiento de productos; después publicidad
y asistencia opcional. Evaluación humana y configuración del proveedor de IA
requieren evidencia externa; se consultó de forma asíncrona mientras sigue el trabajo.
No inventar anotaciones ni abrir ranking experimental en producción.

## Solicitudes completadas previamente

Completadas solicitudes de productos en `feat/product-orders`, desde `58a0efd`.
Selección de un negocio, cantidades, nombre/alias, confirmación con referencia y
precios históricos, resumen para WhatsApp y enlace privado válido por 30 días.
Reintentos conservan identidad incluso después de perder la respuesta. Panel del
titular con filtros, atención, contacto recibido y cancelación auditados; funciona
para solicitudes existentes aunque venza el plan. No registra venta ni stock.

Validación: **120/120 API/HTTP/PostGIS y 100/100 frontend**, build/lint correctos.
Integración real y Playwright 1440/390/320 px, pérdida de respuesta tras guardar
sin duplicados, cambios de estado y privacidad del enlace. Bases temporales
eliminadas. Migración `20261005111735_AddProductOrders`, Down poblado bloqueado.
Demo sin migrar ni reiniciar. Commits locales por unidad; sin push/PR/despliegue.
Reproducir con `node App/tools/verify-catalog.mjs --orders` (`--serve` para navegador).
Evidencia: `../openspec/changes/archive/2026-10-05-product-orders/verify-report.md`.

Próximo incremento: registrar ventas vinculadas a solicitudes y stock atómico,
incluyendo concurrencia, idempotencia y correcciones auditadas. Siguen pendientes
IA opcional, búsqueda/ranking global de productos, publicidad y evaluación humana/
final. Ranking productivo permanece cerrado.

## Catálogo y planes completados previamente — 05/10/2026

Catálogo por negocio y planes manuales en `feat/business-catalog`, derivada de
`feat/ranking-preview-ui`. Gestión privada, precios ARS/USD, disponibilidad
declarada, borrador/publicado/archivado y foto procesada. Perfil público con
búsqueda local y páginas alfabéticas de 20. Administración en
`/administracion/planes`, períodos sin superposición y revocación auditada.
Expirar/revocar oculta catálogo y bloquea escrituras conservando datos privados.
Asignación inicial del rol documentada en `App/README.md`; registro no lo otorga.
Migración `20261005025720_AddBusinessCatalog`, solo en bases temporales.

Validación de ese incremento: 110/110 API y 90/90 frontend, build/lint e integración
real correctos. Unidades `0c5cb25`, `fc0ffee`, `13d1baa`, `e3ba78c` y cierre
`58a0efd`. Evidencia: `../openspec/changes/archive/2026-10-05-business-catalog/verify-report.md`.

## Registro anterior — 04/10/2026

# Punto de continuidad — 04/10/2026

Completado proxy/UI local de ranking en `feat/ranking-preview-ui`, desde `37cad23`.
`/revista` permite elegir zona/rubro, recorrer listas por cursor, volver/recargar,
reintentar sin duplicar y actualizar explícitamente tras vencimiento. Nueva
respuesta `cursor` habilita GET de primera página. El acceso aparece solo con
`RANKING_PREVIEW_ENABLED=true` en Next dev y `Ranking__PreviewEnabled=true` en API.
Producción cerrado aunque se configuren ambos flags; guía pública sin cambio de orden.
Pruebas: **83/83 frontend, 101/101 API/HTTP/PostGIS**, lint/build y Playwright
1440/390/320 px correctos. Harness real con 45 negocios: 20/20/5, identidad estable,
retiro de negocio, 410 y reintento 429 con mismo requestId; sin errores inesperados.
Medición arreglada tras detectar 503 en navegador: migración
`20261005020557_AllowRankingActivityVersions` admite hash de política sin perder
compatibilidad; downgrade bloqueado mientras existan eventos de esa versión.
Aplicada solo a bases temporales; demo intacta. No hay paquetes humanos modificados.
Unidades locales: `9289572` contrato/proxy, `1bdf7e1` navegación, `bcf67d0` actividad,
`63957a2` pantalla y cierre siguiente; consultar git. Sin push/PR.
Evidencia: `../openspec/changes/archive/2026-10-04-ranking-preview-ui/verify-report.md`.
Reproducción: `node App/tools/verify-ranking-preview.mjs` (Release/build previos).
Para revisión visual agregar `--serve`; puerto 3100, base temporal eliminada al cerrar.
Próximo incremento funcional: catálogo por negocio; ranking productivo permanece
pendiente de anotaciones/política evaluada, dimensionamiento y claves compartidas.

## Capacidad completada previamente

Completado el incremento de capacidad en `feat/ranking-capacity`, desde `56a238e`.
Candidatos y saldos históricos acotados: exceso rechaza toda creación con 503 y
rollback; reintento con mismo requestId seguro. Cupos separados de 4 POST/16 GET
simultáneos, sin cola, 429 no-store y recuperación tras éxito/error/cancelación.
Configuración validada al arrancar. Carga local de 1.000 negocios: 17 listas,
50 páginas completas, sin pérdida ni duplicación de exposición al leer/reintentar.
**100/100 API/HTTP/PostGIS y 99/99 BLL/evaluación**, sin omitidas ni advertencias.
Política de previsualización EquityOnly/default/17, sin aprobación experimental.
Solo Development/Testing con `Ranking:PreviewEnabled=true`; Production cerrado.
Tres unidades locales: `ddc4c3a` lecturas acotadas, `072164a` concurrencia y cierre
con carga/documentación; consultar git para el último hash. Sin push/PR.
Evidencia: `../openspec/changes/archive/2026-10-04-ranking-capacity/verify-report.md`.
El proxy/UI se completó en el incremento posterior. La activación productiva requiere
política evaluada/anotaciones y dimensionamiento real; carga HTTP en proceso
no certifica memoria pico, varias instancias ni agregado SQL a escala de 30 días.
Paquetes humanos y borrador intactos. Demo sin migrar/reiniciar/habilitar.
Contrato y configuración en `ranking-public-integration.md` y `../App/README.md`.

Incremento HTTP anterior: cursores protegidos con TTL absoluto, POST/GET locales,
validación, no-store, reintentos, 400/409/410, cuotas 60/600 por minuto y retirada.
Evidencia: `../openspec/changes/archive/2026-10-04-ranking-http-preview/verify-report.md`.

## Persistencia interna completada previamente

El usuario ya realizó la Entrega 2 y quiere avanzar para terminar la app.
Completada la persistencia interna del ranking de revista: creación atómica de
snapshot/oportunidad/contribuciones/secuencia; reintentos concurrentes deduplicados;
traza completa reproducible sin datos del visitante; previsualización propia en cero.
Listas de 30 minutos, historial de 30 días, purga al arranque/cada hora. Paginación
20/20/5 revalida datos actuales, conserva huecos/posiciones y no suma exposición.
El adaptador previo conserva historia desconocida como null, con N=0.
**99/99 BLL/evaluación y 72/72 HTTP/PostGIS**, sin omitidas. Evidencia:
`../openspec/changes/archive/2026-10-04-ranking-snapshots/verify-report.md`.

Próximo incremento del ranking: endpoints y cursores protegidos, validaciones y
límites operativos, luego interfaz. Servicios actuales son internos; guía vigente
sin cambios. La activación sigue requiriendo anotaciones humanas/política evaluada.
No alterar paquetes ni borrador previo. Para avanzar funcionalidad comercial
quedan catálogo, capacidades por plan, pedidos, ventas/stock y publicidad.
Rama `feat/ranking-snapshot-persistence`, derivada de `feat/magazine-ranking-candidates`;
seis unidades locales con pruebas/documentación, sin push ni PR. Consultar git.
Nueva migración `20261004100000_AddRankingSnapshots`: tablas SQL internas fuera
del modelo EF, reversible y probada solo en bases temporales. Aplicar migraciones
habituales antes de usar backend actualizado. Docker/PostGIS activo; demo no
migrada ni reiniciada. Contrato actualizado en `ranking-public-integration.md`.

## Registro anterior — 22/09/2026 (histórico)

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
